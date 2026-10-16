using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace POVLMTYBeastAIAdapter
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("treextr.pathofvalheiman", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("meldurson.LetMeTameYou", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.rehyun.povlmtybeastaiadapter";
        public const string PluginName = "POV LMTY BeastAI Adapter";
        public const string PluginVersion = "0.3.3";

        internal static ManualLogSource Log;
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            _harmony = new Harmony(PluginGuid);

            MethodBase addTame = AdapterPatch.TargetMethod();
            if (addTame == null)
            {
                Logger.LogError("Could not find LetMeTameYou.TameManager.AddTame. Adapter was not installed.");
                return;
            }

            _harmony.Patch(addTame, prefix: new HarmonyMethod(typeof(AdapterPatch), nameof(AdapterPatch.Prefix)));
            Logger.LogInfo("POV LMTY BeastAI Adapter 0.3.3 loaded. Generic compatibility + adaptive taming fallback enabled; no prefab allowlist.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }

    internal static class AdapterPatch
    {
        private static readonly BindingFlags AnyInstance =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly BindingFlags AnyStatic =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        internal static MethodBase TargetMethod()
        {
            Type tameManager = AccessTools.TypeByName("LetMeTameYou.TameManager");
            if (tameManager == null) return null;

            foreach (MethodInfo method in tameManager.GetMethods(AnyStatic))
            {
                if (method.Name != "AddTame") continue;
                ParameterInfo[] p = method.GetParameters();
                if (p.Length == 2 && p[0].ParameterType == typeof(GameObject))
                    return method;
            }
            return null;
        }

        public static bool Prefix(object[] __args)
        {
            try
            {
                if (__args == null || __args.Length < 2) return true;

                GameObject go = __args[0] as GameObject;
                object tb = __args[1];
                if (go == null || tb == null) return true;

                string prefabName = CleanPrefabName(go.name);

                // Diagnostic trace for every LMTY creature. If original AddTame crashes,
                // the final "BEGIN" line tells us exactly which prefab caused it.
                Plugin.Log.LogInfo(
                    $"[LMTY Trace] BEGIN prefab={prefabName}; AI={GetBaseAIType(go)}; " +
                    $"MonsterAI={(go.GetComponent<MonsterAI>() != null)}; AnimalAI={(go.GetComponent<AnimalAI>() != null)}; " +
                    $"Character={(go.GetComponent<Character>() != null)}; Humanoid={(go.GetComponent<Humanoid>() != null)}; " +
                    $"Tameable={(go.GetComponent<Tameable>() != null)}; Procreation={(go.GetComponent<Procreation>() != null)}");

                MonsterAI monsterAI = go.GetComponent<MonsterAI>();
                Character character = go.GetComponent<Character>();
                Humanoid humanoid = go.GetComponent<Humanoid>();
                AnimalAI animalAI = go.GetComponent<AnimalAI>();
                Tameable tameable = go.GetComponent<Tameable>();
                Procreation proc = go.GetComponent<Procreation>();

                string aiType = GetBaseAIType(go);

                // Generic v0.3.x rule: prefab names are irrelevant. We only care about
                // the structural mismatch that made Deer/Hare crash LMTY 0.1.0:
                //   MonsterAI-derived AI exists + Character exists + Humanoid is absent
                //   + AnimalAI is already gone.
                // Normal LMTY creatures (for example Hatchling with Humanoid=True) pass
                // straight through to the original AddTame method.
                bool incompatibleShape = monsterAI != null && character != null && humanoid == null && animalAI == null;
                if (!incompatibleShape)
                    return true;

                string povEvidence;
                if (!IsHighConfidencePovAI(monsterAI, out povEvidence))
                {
                    Plugin.Log.LogWarning(
                        $"[Generic Compatibility] Suspicious LMTY target detected but auto-repair was skipped: " +
                        $"prefab={prefabName}; AI={aiType}; reason=unknown/non-POV AI. " +
                        "This prevents the adapter from modifying unrelated custom-AI mods.");
                    return true;
                }

                Plugin.Log.LogWarning(
                    $"[Generic Compatibility] AUTO-REPAIR triggered: prefab={prefabName}; AI={aiType}; " +
                    $"shape=MonsterAI+Character/no-Humanoid/no-AnimalAI; POV={povEvidence}");

                // Egg-based reproduction is a special LMTY path. v0.3.x intentionally
                // does not emulate that path yet; leave it to original LMTY and emit a
                // precise diagnostic rather than silently converting an egg-layer into
                // a SpawnMini creature.
                string eggConfig;
                if (HasSpecialEggConfig(tb, out eggConfig))
                {
                    Plugin.Log.LogWarning(
                        $"[Generic Compatibility] prefab={prefabName} has special egg config ({eggConfig}). " +
                        "v0.3.x will not auto-repair this special reproduction path; original LMTY will handle it.");
                    return true;
                }

                if (GetBool(tb, "removeTameable", false))
                    return true;

                ConfigureCharacter(go, character, tb);
                tameable = ConfigureTameable(go, tameable, tb);
                ConfigureFood(monsterAI, tb);

                if (GetBool(tb, "procreation", true))
                {
                    proc = ConfigureProcreation(go, proc, tb);
                    ConfigureOffspring(go, proc, tb);
                }
                else if (proc != null)
                {
                    UnityEngine.Object.DestroyImmediate(proc);
                    Plugin.Log.LogInfo("[BeastAI Adapter] procreation=false; removed Procreation.");
                }

                Procreation finalProc = go.GetComponent<Procreation>();
                Plugin.Log.LogInfo(
                    $"[Generic Compatibility] SUCCESS: LMTY creature settings applied without replacing POV {aiType}. " +
                    $"Tameable={(go.GetComponent<Tameable>() != null)}; " +
                    $"Procreation={(finalProc != null)}; " +
                    $"offspring={(finalProc != null && finalProc.m_offspring != null ? finalProc.m_offspring.name : "<none>" )}");

                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("[BeastAI Adapter] Adapter failed. Falling back to original LMTY AddTame.\n" + ex);
                return true;
            }
        }

        private static void ConfigureCharacter(GameObject go, Character character, object tb)
        {
            string specialGroup = GetString(tb, "specialGroup", "");
            bool hostileToSelf = GetBool(tb, "hostileToSelf", false);

            if (!string.IsNullOrEmpty(specialGroup))
                character.m_group = specialGroup;
            else if (!hostileToSelf)
                character.m_group = CleanPrefabName(go.name);
            else
                character.m_group = "";
        }

        private static Tameable ConfigureTameable(GameObject go, Tameable tameable, object tb)
        {
            if (tameable == null)
            {
                tameable = go.AddComponent<Tameable>();
                Plugin.Log.LogInfo("[BeastAI Adapter] Added missing Tameable.");
            }

            tameable.m_commandable = GetBool(tb, "commandable", true);
            tameable.m_tamingTime = GetFloat(tb, "tamingTime", 1800f);
            tameable.m_fedDuration = GetFloat(tb, "fedDuration", 600f);
            return tameable;
        }

        private static void ConfigureFood(MonsterAI ai, object tb)
        {
            ai.m_consumeRange = GetFloat(tb, "consumeRange", 2f);
            ai.m_consumeSearchInterval = GetFloat(tb, "consumeSearchInterval", 10f);
            ai.m_consumeSearchRange = GetFloat(tb, "consumeSearchRange", 10f);
            ai.m_consumeItems = new List<ItemDrop>();

            string foods = GetString(tb, "consumeItems", "");
            if (string.IsNullOrWhiteSpace(foods))
            {
                Plugin.Log.LogWarning("[BeastAI Adapter] consumeItems is empty.");
                return;
            }

            if (ObjectDB.instance == null)
            {
                Plugin.Log.LogWarning("[BeastAI Adapter] ObjectDB.instance is null while applying food config.");
                return;
            }

            char[] separators = new char[] { ',', ':', ';' };
            string[] tokens = foods.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            List<string> added = new List<string>();
            List<string> missing = new List<string>();

            foreach (string raw in tokens)
            {
                string itemName = raw.Trim();
                if (itemName.Length == 0)
                    continue;

                GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemName);
                ItemDrop itemDrop = itemPrefab != null ? itemPrefab.GetComponent<ItemDrop>() : null;

                if (itemDrop != null)
                {
                    ai.m_consumeItems.Add(itemDrop);
                    added.Add(itemPrefab.name);
                }
                else
                {
                    missing.Add(itemName);
                }
            }

            Plugin.Log.LogInfo(
                "[BeastAI Adapter] Food config: raw=\"" + foods + "\"; " +
                "resolved=[" + string.Join(", ", added.ToArray()) + "]; " +
                "missing=[" + string.Join(", ", missing.ToArray()) + "]; " +
                "count=" + ai.m_consumeItems.Count +
                "; consumeRange=" + ai.m_consumeRange +
                "; searchRange=" + ai.m_consumeSearchRange +
                "; searchInterval=" + ai.m_consumeSearchInterval
            );

            EnsureConsumeBridge(ai.gameObject);
        }

        private static void EnsureConsumeBridge(GameObject go)
        {
            if (go == null)
                return;

            if (go.GetComponent<BeastAIConsumeBridge>() == null)
            {
                go.AddComponent<BeastAIConsumeBridge>();
                Plugin.Log.LogInfo("[BeastAI Adapter] Added BeastAIConsumeBridge to " + CleanPrefabName(go.name));
            }
        }

        private static Procreation ConfigureProcreation(GameObject go, Procreation proc, object tb)
        {
            bool added = false;
            if (proc == null)
            {
                proc = go.AddComponent<Procreation>();
                added = true;
            }

            proc.m_maxCreatures = GetInt(tb, "maxCreatures", 5);

            float chance = GetFloat(tb, "pregnancyChance", 0.66f);
            if (chance > 1f) chance *= 0.01f;
            if (chance > 1f) chance = 0.66f;

            proc.m_pregnancyChance = chance;
            proc.m_pregnancyDuration = GetFloat(tb, "pregnancyDuration", 60f);
            if (proc.m_updateInterval < 20f) proc.m_updateInterval = 20f;

            float size = GetFloat(tb, "size", 1f);
            proc.m_partnerCheckRange = 4f * size;
            proc.m_totalCheckRange = 10f * size;

            ClearProcreationCaches(proc);

            Plugin.Log.LogInfo(
                $"[BeastAI Adapter] Procreation configured: added={added}; max={proc.m_maxCreatures}; " +
                $"chance={proc.m_pregnancyChance}; pregnancy={proc.m_pregnancyDuration}s; " +
                $"partnerRange={proc.m_partnerCheckRange}; totalRange={proc.m_totalCheckRange}");

            return proc;
        }

        private static void ConfigureOffspring(GameObject go, Procreation proc, object tb)
        {
            bool overwrite = GetBool(tb, "offspringOverwrite", false);
            float growTime = GetFloat(tb, "growTime", 1800f);

            if (proc.m_offspring != null && !overwrite)
            {
                ApplyGrowTime(proc.m_offspring, growTime);
                Plugin.Log.LogInfo("[Generic Compatibility] Keeping existing offspring: " + proc.m_offspring.name);
                return;
            }

            object explicitOffspringValue = GetMember(tb, "offspringPrefab");
            GameObject explicitPrefab = explicitOffspringValue as GameObject;
            string explicitOffspring = explicitPrefab != null
                ? CleanPrefabName(explicitPrefab.name)
                : (explicitOffspringValue != null ? explicitOffspringValue.ToString().Trim() : "");

            if (!string.IsNullOrEmpty(explicitOffspring))
            {
                if (explicitPrefab == null && ZNetScene.instance != null)
                    explicitPrefab = ZNetScene.instance.GetPrefab(explicitOffspring);

                if (explicitPrefab == null)
                    throw new InvalidOperationException(
                        "Configured offspringPrefab could not be resolved from ZNetScene: " + explicitOffspring);

                proc.m_offspring = explicitPrefab;
                ApplyGrowTime(explicitPrefab, growTime);
                ClearProcreationCaches(proc);
                Plugin.Log.LogInfo(
                    "[Generic Compatibility] Explicit LMTY offspringPrefab assigned: " + explicitPrefab.name);
                return;
            }

            Type tameManager = AccessTools.TypeByName("LetMeTameYou.TameManager");
            MethodInfo spawnMini = tameManager != null
                ? AccessTools.Method(tameManager, "SpawnMini", new Type[] { typeof(GameObject), typeof(float) })
                : null;

            if (spawnMini == null)
                throw new MissingMethodException("LetMeTameYou.TameManager.SpawnMini(GameObject,float) not found.");

            GameObject offspring = spawnMini.Invoke(null, new object[] { go, growTime }) as GameObject;

            if (offspring == null)
                throw new InvalidOperationException("LMTY SpawnMini returned null.");

            proc.m_offspring = offspring;
            ClearProcreationCaches(proc);
            Plugin.Log.LogInfo("[Generic Compatibility] LMTY offspring assigned: " + offspring.name);
        }

        private static void ApplyGrowTime(GameObject offspring, float growTime)
        {
            if (offspring == null) return;

            Growup growup = offspring.GetComponent<Growup>();
            if (growup != null)
            {
                growup.m_growTime = growTime;
                return;
            }

            EggGrow egg = offspring.GetComponent<EggGrow>();
            if (egg != null && egg.m_grownPrefab != null)
            {
                Growup hatched = egg.m_grownPrefab.GetComponent<Growup>();
                if (hatched != null) hatched.m_growTime = growTime;
            }
        }

        private static void ClearProcreationCaches(Procreation proc)
        {
            AccessTools.Field(typeof(Procreation), "m_offspringPrefab")?.SetValue(proc, null);
            AccessTools.Field(typeof(Procreation), "m_myPrefab")?.SetValue(proc, null);
        }

        private static bool HasSpecialEggConfig(object tb, out string description)
        {
            description = "";
            object value = GetMember(tb, "egg");
            if (value == null)
                return false;

            string text;
            Type valueType = value.GetType();

            if (value is string)
            {
                text = ((string)value).Trim();
            }
            else if (valueType.IsEnum)
            {
                text = value.ToString().Trim();
            }
            else
            {
                return false;
            }

            if (string.IsNullOrEmpty(text) ||
                string.Equals(text, "None", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "False", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "0", StringComparison.OrdinalIgnoreCase))
                return false;

            description = text;
            return true;
        }

        private static bool IsHighConfidencePovAI(MonsterAI ai, out string evidence)
        {
            evidence = "<none>";
            if (ai == null)
                return false;

            Type runtimeType = ai.GetType();
            Type knownPovBeastAI = AccessTools.TypeByName("PassiveTree.BeastAI");

            if (knownPovBeastAI != null &&
                (runtimeType == knownPovBeastAI || knownPovBeastAI.IsAssignableFrom(runtimeType)))
            {
                evidence = "PassiveTree.BeastAI type match";
                return true;
            }

            string fullName = runtimeType.FullName ?? runtimeType.Name ?? "";
            if (fullName.StartsWith("PassiveTree.", StringComparison.Ordinal))
            {
                evidence = "PassiveTree namespace";
                return true;
            }

            string assemblyName = runtimeType.Assembly != null
                ? (runtimeType.Assembly.GetName().Name ?? "")
                : "";

            if (assemblyName.IndexOf("PathOfValheiman", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                evidence = "PathOfValheiman assembly";
                return true;
            }

            return false;
        }

        private static string GetBaseAIType(GameObject go)
        {
            BaseAI[] all = go.GetComponents<BaseAI>();
            if (all == null || all.Length == 0) return "<none>";

            List<string> names = new List<string>();
            foreach (BaseAI ai in all)
                if (ai != null)
                    names.Add(ai.GetType().FullName ?? ai.GetType().Name);

            return string.Join(",", names);
        }

        private static string CleanPrefabName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            const string clone = "(Clone)";
            return name.EndsWith(clone, StringComparison.Ordinal)
                ? name.Substring(0, name.Length - clone.Length)
                : name;
        }

        private static object GetMember(object obj, string name)
        {
            if (obj == null) return null;
            Type t = obj.GetType();

            PropertyInfo p = t.GetProperty(name, AnyInstance);
            if (p != null) return p.GetValue(obj, null);

            FieldInfo f = t.GetField(name, AnyInstance);
            if (f != null) return f.GetValue(obj);

            return null;
        }

        private static bool GetBool(object obj, string name, bool fallback)
        {
            object v = GetMember(obj, name);
            try { return v == null ? fallback : Convert.ToBoolean(v); }
            catch { return fallback; }
        }

        private static int GetInt(object obj, string name, int fallback)
        {
            object v = GetMember(obj, name);
            try { return v == null ? fallback : Convert.ToInt32(v); }
            catch { return fallback; }
        }

        private static float GetFloat(object obj, string name, float fallback)
        {
            object v = GetMember(obj, name);
            try { return v == null ? fallback : Convert.ToSingle(v); }
            catch { return fallback; }
        }

        private static string GetString(object obj, string name, string fallback)
        {
            object v = GetMember(obj, name);
            return v?.ToString() ?? fallback;
        }
    }

    internal sealed class BeastAIConsumeBridge : MonoBehaviour
    {
        private MonsterAI _ai;
        private Tameable _tameable;
        private ZNetView _nview;
        private float _timer;
        private float _nextDiagnostic;
        private float _nextProgressDiagnostic;
        private float _progressTickTimer;
        private float _searchTimer;
        private ItemDrop _targetFood;
        private float _lastObservedTimeLeft = -1f;
        private float _nativeObservationElapsed;
        private bool _nativeProgressConfirmed;
        private bool _fallbackActivated;
        private bool _observationLogged;

        private const float NativeObservationWindow = 6f;

        private static readonly MethodInfo MoveToMethod = AccessTools.Method(
            typeof(BaseAI),
            "MoveTo",
            new Type[] { typeof(float), typeof(Vector3), typeof(float), typeof(bool) }
        );

        private static readonly MethodInfo DecreaseRemainingTimeMethod =
            AccessTools.Method(typeof(Tameable), "DecreaseRemainingTime", new Type[] { typeof(float) });

        private static readonly MethodInfo GetRemainingTimeMethod =
            AccessTools.Method(typeof(Tameable), "GetRemainingTime", Type.EmptyTypes);

        private static readonly MethodInfo TameMethod =
            AccessTools.Method(typeof(Tameable), "Tame", Type.EmptyTypes);

        private void Awake()
        {
            _ai = GetComponent<MonsterAI>();
            _tameable = GetComponent<Tameable>();
            _nview = GetComponent<ZNetView>();

            if (_ai != null && _tameable != null)
            {
                Plugin.Log.LogInfo(
                    "[BeastAI Feed] Runtime bridge awake: prefab=" + CleanName(gameObject.name) +
                    "; AI=" + _ai.GetType().FullName +
                    "; foods=" + (_ai.m_consumeItems != null ? _ai.m_consumeItems.Count : 0)
                );
            }
        }

        private void Update()
        {
            if (_ai == null || _tameable == null)
                return;

            _timer -= Time.deltaTime;
            if (_timer > 0f)
                return;

            _timer = 0.35f;

            if (_ai.m_consumeItems == null || _ai.m_consumeItems.Count == 0)
                return;

            if (_nview != null && _nview.IsValid() && !_nview.IsOwner())
                return;

            bool hungry;
            try
            {
                hungry = _tameable.IsHungry();
            }
            catch
            {
                return;
            }

            if (!hungry)
            {
                _targetFood = null;
                _searchTimer = 0f;
                RunTamingFallbackTick();
                LogTamingProgress();
                return;
            }

            _progressTickTimer = 0f;
            _lastObservedTimeLeft = -1f;
            _nativeObservationElapsed = 0f;
            _nativeProgressConfirmed = false;
            _fallbackActivated = false;
            _observationLogged = false;

            if (!IsValidTarget(_targetFood))
            {
                _targetFood = null;
                _searchTimer -= 0.35f;

                if (_searchTimer <= 0f)
                {
                    _searchTimer = Mathf.Max(1f, _ai.m_consumeSearchInterval);
                    _targetFood = FindNearestAllowedFood(out _);
                }
            }

            if (_targetFood == null)
                return;

            float nearestDistance = Vector3.Distance(transform.position, _targetFood.transform.position);
            float searchRange = Mathf.Max(_ai.m_consumeRange, _ai.m_consumeSearchRange);

            if (nearestDistance > searchRange)
            {
                _targetFood = null;
                return;
            }

            float eatRange = Mathf.Max(0.5f, _ai.m_consumeRange);

            if (nearestDistance > eatRange)
            {
                TryMoveTo(_targetFood.transform.position, eatRange * 0.8f);

                if (Time.time >= _nextDiagnostic)
                {
                    _nextDiagnostic = Time.time + 3f;
                    Plugin.Log.LogInfo(
                        "[BeastAI Feed] Food detected: prefab=" + CleanName(gameObject.name) +
                        "; food=" + CleanName(_targetFood.gameObject.name) +
                        "; distance=" + nearestDistance.ToString("0.00") +
                        "; eatRange=" + eatRange.ToString("0.00") +
                        "; movingTowardFood=True"
                    );
                }
                return;
            }

            ItemDrop consumedTarget = _targetFood;
            _targetFood = null;
            Consume(consumedTarget, nearestDistance);
        }

        private bool IsValidTarget(ItemDrop drop)
        {
            if (drop == null || !drop.gameObject.activeInHierarchy ||
                drop.m_itemData == null || drop.m_itemData.m_shared == null)
                return false;

            if (!IsAllowed(drop))
                return false;

            float searchRange = Mathf.Max(_ai.m_consumeRange, _ai.m_consumeSearchRange);
            return Vector3.Distance(transform.position, drop.transform.position) <= searchRange;
        }

        private void RunTamingFallbackTick()
        {
            if (_tameable.IsTamed())
                return;

            _progressTickTimer += 0.35f;
            if (_progressTickTimer < 3f)
                return;

            float dt = _progressTickTimer;
            _progressTickTimer = 0f;

            if (_nview == null || !_nview.IsValid() || !_nview.IsOwner())
                return;

            bool alerted = false;
            try
            {
                alerted = _ai.IsAlerted();
            }
            catch
            {
                alerted = false;
            }

            ZDO zdo = _nview.GetZDO();
            if (zdo == null)
                return;

            float before = zdo.GetFloat("TameTimeLeft", -1f);
            if (before < 0f)
            {
                // v0.3.3: another taming/feed patch can legitimately mark the creature
                // as fed (IsHungry() == false) before this bridge consumes the ground
                // item itself. In that case TameLastFeeding exists, but TameTimeLeft
                // may still be absent. v0.3.1 returned here forever, so neither the
                // native-observation window nor the fallback could ever start.
                //
                // Only seed the countdown when a real feeding timestamp exists. This
                // prevents a freshly spawned, never-fed creature from starting taming
                // merely because another mod temporarily reports it as not hungry.
                long lastFeeding = zdo.GetLong("TameLastFeeding", 0L);
                if (lastFeeding <= 0L)
                {
                    if (Time.time >= _nextDiagnostic)
                    {
                        _nextDiagnostic = Time.time + 3f;
                        Plugin.Log.LogWarning(
                            "[Adaptive Taming] Fed-state has no TameTimeLeft and no TameLastFeeding; " +
                            "countdown not initialized: prefab=" + CleanName(gameObject.name)
                        );
                    }
                    return;
                }

                before = Mathf.Max(0f, _tameable.m_tamingTime);
                zdo.Set("TameTimeLeft", before);

                _lastObservedTimeLeft = before;
                _nativeObservationElapsed = 0f;
                _nativeProgressConfirmed = false;
                _fallbackActivated = false;
                _observationLogged = false;

                Plugin.Log.LogWarning(
                    "[Adaptive Taming] Fed creature had no TameTimeLeft; initialized countdown: prefab=" +
                    CleanName(gameObject.name) +
                    "; TameTimeLeft=" + before.ToString("0.0") +
                    "; TameLastFeeding=" + lastFeeding
                );
            }

            if (alerted)
            {
                if (Time.time >= _nextDiagnostic)
                {
                    _nextDiagnostic = Time.time + 3f;
                    Plugin.Log.LogInfo(
                        "[BeastAI Feed] Taming paused: prefab=" + CleanName(gameObject.name) +
                        "; reason=alerted; TameTimeLeft=" + before.ToString("0.0")
                    );
                }
                _lastObservedTimeLeft = before;
                return;
            }

            if (_lastObservedTimeLeft >= 0f && before < _lastObservedTimeLeft - 0.01f)
            {
                if (!_nativeProgressConfirmed)
                {
                    Plugin.Log.LogInfo(
                        "[Adaptive Taming] Native progress detected; fallback remains inactive: prefab=" +
                        CleanName(gameObject.name) +
                        "; previous=" + _lastObservedTimeLeft.ToString("0.0") +
                        "; current=" + before.ToString("0.0")
                    );
                }

                _nativeProgressConfirmed = true;
                _fallbackActivated = false;
                _nativeObservationElapsed = 0f;
                _lastObservedTimeLeft = before;
                return;
            }

            if (_nativeProgressConfirmed)
            {
                _lastObservedTimeLeft = before;
                return;
            }

            _nativeObservationElapsed += dt;

            if (!_fallbackActivated && _nativeObservationElapsed < NativeObservationWindow)
            {
                if (!_observationLogged)
                {
                    _observationLogged = true;
                    Plugin.Log.LogInfo(
                        "[Adaptive Taming] Observing native timer before fallback: prefab=" +
                        CleanName(gameObject.name) +
                        "; window=" + NativeObservationWindow.ToString("0") + "s" +
                        "; TameTimeLeft=" + before.ToString("0.0")
                    );
                }

                _lastObservedTimeLeft = before;
                return;
            }

            if (!_fallbackActivated)
            {
                _fallbackActivated = true;
                Plugin.Log.LogWarning(
                    "[Adaptive Taming] Native timer did not advance; fallback activated: prefab=" +
                    CleanName(gameObject.name) +
                    "; observed=" + _nativeObservationElapsed.ToString("0.0") + "s" +
                    "; TameTimeLeft=" + before.ToString("0.0")
                );
            }

            try
            {
                if (DecreaseRemainingTimeMethod != null)
                {
                    DecreaseRemainingTimeMethod.Invoke(_tameable, new object[] { dt });
                }
                else
                {
                    zdo.Set("TameTimeLeft", Mathf.Max(0f, before - dt));
                }

                float after = GetRemainingTimeMethod != null
                    ? (float)GetRemainingTimeMethod.Invoke(_tameable, null)
                    : zdo.GetFloat("TameTimeLeft", -1f);

                _lastObservedTimeLeft = after;

                Plugin.Log.LogInfo(
                    "[BeastAI Feed] Fallback taming tick: prefab=" + CleanName(gameObject.name) +
                    "; before=" + before.ToString("0.0") +
                    "; after=" + after.ToString("0.0") +
                    "; alerted=False"
                );

                if (after <= 0f && !_tameable.IsTamed())
                {
                    if (TameMethod != null)
                        TameMethod.Invoke(_tameable, null);

                    Plugin.Log.LogInfo(
                        "[BeastAI Feed] Taming completed by fallback: prefab=" + CleanName(gameObject.name) +
                        "; tamed=" + _tameable.IsTamed()
                    );
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(
                    "[BeastAI Feed] Fallback taming tick failed on " +
                    CleanName(gameObject.name) + ": " + ex
                );
            }
        }

        private void LogTamingProgress()
        {
            if (_tameable.IsTamed())
                return;

            if (Time.time < _nextProgressDiagnostic)
                return;

            _nextProgressDiagnostic = Time.time + 15f;

            if (_nview == null || !_nview.IsValid())
                return;

            ZDO zdo = _nview.GetZDO();
            if (zdo == null)
                return;

            float timeLeft = zdo.GetFloat("TameTimeLeft", -1f);
            float total = _tameable.m_tamingTime;
            float pct = (timeLeft >= 0f && total > 0f)
                ? Mathf.Clamp01(1f - (timeLeft / total)) * 100f
                : -1f;

            Plugin.Log.LogInfo(
                "[BeastAI Feed] Taming progress: prefab=" + CleanName(gameObject.name) +
                "; hungry=False" +
                "; TameTimeLeft=" + timeLeft.ToString("0.0") +
                "; tamingTime=" + total.ToString("0.0") +
                "; approxPercent=" + (pct >= 0f ? pct.ToString("0.0") : "unknown") +
                "; tamed=" + _tameable.IsTamed()
            );
        }

        private ItemDrop FindNearestAllowedFood(out float nearestDistance)
        {
            nearestDistance = float.MaxValue;
            ItemDrop nearest = null;

            float searchRange = Mathf.Max(_ai.m_consumeRange, _ai.m_consumeSearchRange);
            Vector3 here = transform.position;

            ItemDrop[] allDrops = UnityEngine.Object.FindObjectsByType<ItemDrop>(
                UnityEngine.FindObjectsSortMode.None
            );

            foreach (ItemDrop drop in allDrops)
            {
                if (drop == null || !drop.gameObject.activeInHierarchy || drop.m_itemData == null || drop.m_itemData.m_shared == null)
                    continue;

                if (!IsAllowed(drop))
                    continue;

                float distance = Vector3.Distance(here, drop.transform.position);
                if (distance > searchRange || distance >= nearestDistance)
                    continue;

                nearest = drop;
                nearestDistance = distance;
            }

            return nearest;
        }

        private bool IsAllowed(ItemDrop drop)
        {
            string sharedName = drop.m_itemData.m_shared.m_name;
            string prefabName = CleanName(drop.gameObject.name);

            foreach (ItemDrop allowed in _ai.m_consumeItems)
            {
                if (allowed == null || allowed.m_itemData == null || allowed.m_itemData.m_shared == null)
                    continue;

                if (allowed.m_itemData.m_shared.m_name == sharedName)
                    return true;

                if (CleanName(allowed.gameObject.name) == prefabName)
                    return true;
            }

            return false;
        }

        private void Consume(ItemDrop drop, float distance)
        {
            try
            {
                if (drop == null)
                    return;

                if (!drop.RemoveOne())
                    return;

                if (_ai.m_onConsumedItem != null)
                    _ai.m_onConsumedItem.Invoke(null);

                if (_nview != null && _nview.IsValid())
                {
                    ZDO zdo = _nview.GetZDO();
                    if (zdo != null)
                    {
                        long nowTicks = ZNet.instance != null
                            ? ZNet.instance.GetTime().Ticks
                            : DateTime.UtcNow.Ticks;

                        zdo.Set("TameLastFeeding", nowTicks);

                        float timeLeft = zdo.GetFloat("TameTimeLeft", -1f);
                        bool initializedTimeLeft = false;

                        if (timeLeft < 0f)
                        {
                            timeLeft = _tameable.m_tamingTime;
                            zdo.Set("TameTimeLeft", timeLeft);
                            initializedTimeLeft = true;
                        }

                        _lastObservedTimeLeft = timeLeft;
                        _nativeObservationElapsed = 0f;
                        _nativeProgressConfirmed = false;
                        _fallbackActivated = false;
                        _observationLogged = false;

                        Plugin.Log.LogInfo(
                            "[BeastAI Feed] Feeding state updated: TameLastFeeding=" + nowTicks +
                            "; TameTimeLeft=" + timeLeft.ToString("0.0") +
                            "; initialized=" + initializedTimeLeft +
                            "; tamingTime=" + _tameable.m_tamingTime.ToString("0.0")
                        );
                    }
                }

                Plugin.Log.LogInfo(
                    "[BeastAI Feed] CONSUMED: prefab=" + CleanName(gameObject.name) +
                    "; food=" + CleanName(drop.gameObject.name) +
                    "; distance=" + distance.ToString("0.00") +
                    "; tamed=" + _tameable.IsTamed()
                );
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("[BeastAI Feed] Consume failed on " + CleanName(gameObject.name) + ": " + ex);
            }
        }

        private void TryMoveTo(Vector3 point, float stopDistance)
        {
            if (MoveToMethod == null)
                return;

            try
            {
                MoveToMethod.Invoke(_ai, new object[] { 0.35f, point, stopDistance, false });
            }
            catch
            {
            }
        }

        private static string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            int paren = name.IndexOf('(');
            int space = name.IndexOf(' ');
            int cut = -1;

            if (paren >= 0) cut = paren;
            if (space >= 0 && (cut < 0 || space < cut)) cut = space;

            return cut >= 0 ? name.Substring(0, cut) : name;
        }
    }
}
