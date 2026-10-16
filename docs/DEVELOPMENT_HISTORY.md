# Development history and root-cause analysis

This document records the problem diagnosis, failed approaches, fixes, and validation that led from the original Deer-specific proof of concept through the current v0.3.3 release.

## 1. Original symptom

With LetMeTameYou enabled, Deer could be configured for taming/breeding normally when PathOfValheiman was disabled.

When PathOfValheiman was enabled, LMTY crashed while processing the same creature with a `NullReferenceException` in `LetMeTameYou.TameManager.AddTame()`.

This was reproduced as an A/B difference:

- POV OFF + LMTY -> Deer path worked.
- POV ON + LMTY -> LMTY `AddTame` failed.

## 2. What POV changes

POV's Any Beast path mutates some normally passive creatures. For Deer/Hare-style cases, the runtime result observed during development was:

```text
AI         = PassiveTree.BeastAI
MonsterAI  = true
AnimalAI   = false
Character  = true
Humanoid   = false
Tameable   = true
```

The important part is not the creature's name; it is the component shape after POV has modified the prefab.

## 3. Why LMTY failed

LMTY 0.1.0 contains an AnimalAI conversion path that prepares normally passive creatures for its taming system.

For the POV-mutated Deer, a MonsterAI-compatible custom AI already exists (`PassiveTree.BeastAI`). LMTY therefore does not use the same AnimalAI conversion path. The resulting object still has no Humanoid, and a later LMTY path expects the MonsterAI-style creature to have the structure it normally works with.

The crash was therefore a cross-mod structural mismatch rather than a bad food list or a broken Deer prefab by itself.

## 4. Why replacing POV AI was rejected

The purpose of the adapter is compatibility, not to disable POV's creature behavior.

Replacing `PassiveTree.BeastAI` with a vanilla/LMTY AI would defeat the point of running POV and could break other POV mechanics. The chosen design preserves POV AI and fills in the LMTY-facing taming/breeding behavior around it.

## 5. Feeding problem

Once the initial NRE was bypassed, the next problem was that the POV BeastAI path did not consume the configured LMTY food in the way needed by this combination.

Food parsing was tested first. Deer successfully resolved all configured vegetarian items, which ruled out malformed YML parsing.

A dedicated `BeastAIConsumeBridge` was then introduced to:

- find allowed ground food;
- move toward it using the existing AI movement method;
- consume one item;
- invoke the AI consume callback;
- update the tame feeding state.

## 6. Taming countdown problem

After food consumption worked, Deer showed `hungry=False` but `TameTimeLeft` remained unchanged.

This demonstrated that feeding alone was not sufficient: the native taming countdown for the POV BeastAI path was not progressing.

The first stable workaround in 0.2.7 invoked the game's internal taming countdown/completion methods when required.

In 0.3.1 this became adaptive:

```text
meal
  -> initialize/observe TameTimeLeft
  -> watch native progress for 6 calm seconds
  -> native moved? keep fallback off
  -> native stalled? enable fallback
```

This avoids forcing the fallback on a future compatible POV creature whose native timer happens to work correctly.

## 7. Performance regression and fix

An early consume bridge performed a scene-wide `FindObjectsOfType<ItemDrop>()` roughly every 0.35 seconds per compatible creature while searching for food.

The user observed periodic ~0.3-second hitches beginning with that runtime bridge.

The 0.2.7 fix:

- uses `FindObjectsByType<ItemDrop>(FindObjectsSortMode.None)`;
- only scans when hungry and no valid cached target exists;
- respects `consumeSearchInterval` (commonly ~10 seconds);
- caches the target while moving toward it.

The periodic hitch disappeared after this change.

## 8. From per-prefab fixes to generic repair

The early adapter targeted explicit names such as Deer and Hare.

During investigation, a broader set of POV-modified creatures was identified, including passive/young prefabs and a few MonsterAI cases. That list was useful for diagnosis, but it became clear that **membership in that list does not mean the creature needs repair**.

The correct question is:

> Does this particular LMTY target currently have the same incompatible POV component structure?

That led to v0.3.0's generic structural rule rather than a growing prefab allowlist.

## 9. Current high-confidence rule

v0.3.x auto-repairs only when:

```text
MonsterAI != null
Character != null
Humanoid == null
AnimalAI == null
POV AI identification == high confidence
```

Examples observed in final testing:

- Deer -> auto-repair
- Hare -> auto-repair
- Seal -> auto-repair
- Hatchling -> no repair (`Humanoid=True`), original LMTY path
- Bjorn/Unbjorn-style MonsterAI targets with Humanoid -> original LMTY path

## 10. Offspring handling

The adapter prefers existing LMTY information:

1. keep an already configured offspring when present;
2. use explicit `offspringPrefab` when LMTY configuration provides it;
3. otherwise use LMTY's mini-offspring creation path.

`Seal -> Seal_Pup` was used as the first generic explicit-offspring validation.

## 11. Egg reproduction

LMTY supports custom `egg:` reproduction definitions.

The adapter intentionally does not emulate that specialized initialization for a creature that also requires the POV incompatibility repair. If such a case is detected, it is logged and left to LMTY instead of guessing.

Hatchling/Drake does not currently require this repair because its runtime structure is compatible with original LMTY. Taming, breeding and egg laying were observed to work through the original LMTY path.

## 12. Final validation of v0.3.1

Final validation demonstrated both sides of the design:

### Repaired path

A fresh high-star Deer:

- consumed configured food;
- entered the six-second native-timer observation window;
- was correctly diagnosed as stalled;
- activated fallback;
- counted from 180 seconds to zero;
- finished taming as a 4-star Deer.

Earlier v0.3.x validation also observed Deer pregnancy and birth.

### Unmodified path

A spawned high-star Hatchling:

- did not match the incompatible structural rule;
- remained on original LMTY handling;
- successfully tamed as a 3-star Hatchling.

Hatchling breeding and egg laying had also been observed previously.

### Error check

The final validation log contained no match for:

- `NullReferenceException`
- `Exception`
- `[Error`

## 13. Why development stopped at 0.3.1

At this point the adapter met the intended scope:

- it fixes the reproduced POV/LMTY structural conflict;
- it is not tied to a prefab name list;
- it does not modify normal LMTY creatures;
- it self-enables taming fallback only when needed;
- feeding and breeding work for the repaired Deer path;
- the previous performance regression is gone;
- final validation produced no exception/error entries.

Future changes should therefore be driven by a new reproducible failure rather than speculative expansion.

## 14. v0.3.2 packaging/icon update

v0.3.2 changed package branding and the public icon only. The runtime compatibility logic remained the validated v0.3.1 implementation.

## 15. v0.3.3 missing `TameTimeLeft` regression found during distribution validation

A clean Gale/Thunderstore-style test exposed a state not covered by the earlier validation: a repaired Deer could already report `hungry=False` while its ZDO had no `TameTimeLeft` entry, so reads returned `-1`. The adaptive fallback treated a missing timer as non-actionable and returned before starting its six-second observation window.

The fix is intentionally conservative. When `TameTimeLeft` is missing, the adapter now checks for a real positive `TameLastFeeding` timestamp. Only then does it initialize the countdown from `Tameable.m_tamingTime`. It then follows the same native-observation and fallback decision path used before.

Live validation with a 4-star Deer showed:

- missing timer initialized to 120 seconds;
- native timer remained stalled for the 6-second observation window;
- fallback activated and reduced the countdown to 0;
- Valheim completed taming and the adapter logged `tamed=True`;
- no `NullReferenceException`, general `Exception`, or `[Error` entry was present in the final log.
