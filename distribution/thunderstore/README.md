# POV LMTY BeastAI Adapter

A compatibility layer for **PathOfValheiman (POV)** and **LetMeTameYou (LMTY)** on Valheim.

The adapter fixes a specific interoperability problem where POV converts some normally passive `AnimalAI` creatures into its custom `PassiveTree.BeastAI`, while LMTY expects a different component layout when adding taming and breeding support.

**Current stable version: 0.3.1**

## What problem does this solve?

In the tested combination of POV 4.13.10 and LMTY 0.1.0, Deer/Hare-style creatures can reach LMTY with this runtime shape:

```text
AI         = PassiveTree.BeastAI
MonsterAI  = true
AnimalAI   = false
Character  = true
Humanoid   = false
Tameable   = true
```

LMTY sees `MonsterAI` and therefore does not take its normal `AnimalAI` conversion path. In the affected path it later expects data associated with a `Humanoid`, but POV's converted creature does not have one. The original result was a `NullReferenceException` in `LetMeTameYou.TameManager.AddTame()`.

This adapter does **not** replace POV's AI. Instead, it intercepts only the confirmed incompatible structure and applies the LMTY creature settings directly.

## Generic auto-repair in 0.3.1

There is no prefab-name allowlist.

A creature is automatically repaired only when all of these are true:

- `MonsterAI` exists
- `Character` exists
- `Humanoid` is missing
- `AnimalAI` is missing
- the runtime AI is identified with high confidence as POV (`PassiveTree.BeastAI`, another `PassiveTree.*` type, or a type from a PathOfValheiman assembly)

Normal LMTY targets such as `MonsterAI + Humanoid` creatures are passed through untouched.

If the component layout looks suspicious but the AI cannot be confidently attributed to POV, the adapter logs a warning and does not modify the creature.

## What the repair applies

For a confirmed POV/LMTY incompatible target, the adapter reuses LMTY's already-parsed creature configuration and applies:

- `Tameable` creation when missing
- LMTY `commandable`, `tamingTime`, and `fedDuration`
- food list and consume ranges
- a `BeastAIConsumeBridge` for ground-food consumption
- adaptive taming-progress fallback
- `Procreation` when enabled
- existing offspring when available
- explicit `offspringPrefab` when configured (for example `Seal -> Seal_Pup`)
- otherwise LMTY's `SpawnMini()` offspring path

## Adaptive taming fallback

The adapter does not immediately force its own taming countdown.

After a successful meal it observes the native `TameTimeLeft` for six seconds of calm, non-alerted time:

1. If the native timer moves, the fallback remains disabled.
2. If the timer does not move, the fallback activates automatically.
3. Alerted time does not count toward the observation window.
4. Once the timer reaches zero, the adapter completes taming through the game's `Tameable` path.

This was added because POV `BeastAI` creatures could eat successfully while the native taming countdown remained frozen.

## Performance

An earlier development build scanned all `ItemDrop` objects every ~0.35 seconds per compatible creature and caused noticeable periodic hitches.

The current bridge instead:

- searches only while a compatible creature is hungry and needs a target;
- uses `FindObjectsByType<ItemDrop>(FindObjectsSortMode.None)`;
- throttles searches using the creature's `consumeSearchInterval`;
- caches the selected food target while moving toward it.

The observed hitch disappeared after this change.

## Tested environment

The current release was validated with:

- Valheim **1.0.20**
- BepInEx **5.4.23.5** / BepInExPack Valheim **5.4.2351**
- PathOfValheiman **4.13.10**
- LetMeTameYou **0.1.0**
- BetterTames **0.9.0** present in the test profile (optional, not a hard dependency)

Validated behavior includes:

| Creature/path | Result |
|---|---|
| Deer / POV `PassiveTree.BeastAI` | Auto-repair triggered; feeding works; stalled native timer detected; fallback completed taming |
| High-star Deer | 4-star Deer successfully tamed in live testing |
| Deer breeding | Feeding, pregnancy and birth observed in live testing |
| Hare | Generic incompatible structure detected and auto-repair configured |
| Seal | Generic incompatible structure detected; `Seal_Pup` offspring prefab assigned |
| Hatchling / Drake | Not auto-repaired; passed through to original LMTY; taming, breeding and egg laying observed |
| High-star Hatchling | 3-star Hatchling successfully tamed in live testing |

No `NullReferenceException`, general `Exception`, or `[Error` entry was present in the final 0.3.1 validation log.

## Special egg path limitation

LMTY's custom `egg:` reproduction path is intentionally **not reimplemented** for a POV creature that also matches the incompatible auto-repair structure.

If that combination is detected, the adapter logs the situation and defers to original LMTY rather than silently converting an egg layer to a mini-offspring path.

Current tested Hatchling/Drake behavior does not hit this limitation because Hatchling has a normal `MonsterAI + Humanoid` structure and is handled by original LMTY.

## Installation

### Thunderstore / Gale / r2modman

Install the package through your Thunderstore-compatible mod manager. Required dependencies should be installed automatically when declared by the package.

### Manual

Install BepInEx, PathOfValheiman and LetMeTameYou, then place:

```text
POVLMTYBeastAIAdapter.dll
```

under:

```text
Valheim/BepInEx/plugins/POVLMTYBeastAIAdapter/
```

Do not load older versions of this adapter at the same time. They share the same BepInEx plugin GUID:

```text
com.rehyun.povlmtybeastaiadapter
```

Also remove/disable the old experimental `POVLMTYDeerAdapter` and `POVDeerBreedingFix` while using this adapter.

## Building from source

Requirements:

- .NET SDK capable of building `netstandard2.1`
- local Valheim installation
- BepInEx core files from your local Valheim/Gale profile

The project deliberately references your **local** Valheim/BepInEx assemblies. Game files and third-party mod binaries are not included in this repository.

On the original Windows/Gale test setup, run:

```text
build_and_install_gale_default.bat
```

Or build manually and override the paths if needed:

```powershell
dotnet build -c Release `
  -p:ValheimDir="C:\Program Files (x86)\Steam\steamapps\common\Valheim" `
  -p:BepInExCoreDir="$env:APPDATA\com.kesomannen.gale\valheim\profiles\default\BepInEx\core"
```

## Logs / troubleshooting

Useful adapter log prefixes:

```text
[LMTY Trace]
[Generic Compatibility]
[BeastAI Adapter]
[BeastAI Feed]
[Adaptive Taming]
```

A repaired Deer-style creature should normally show:

```text
[Generic Compatibility] AUTO-REPAIR triggered
[Adaptive Taming] Observing native timer before fallback
```

If its native taming countdown is broken:

```text
[Adaptive Taming] Native timer did not advance; fallback activated
[BeastAI Feed] Fallback taming tick
```

A normal LMTY creature should not show `AUTO-REPAIR`.

## Compatibility scope

This project is intentionally conservative. It is a POV/LMTY compatibility adapter, not a general replacement taming system.

The adapter modifies only the high-confidence POV component shape that was reproduced and diagnosed during development. Unknown custom-AI mods are not automatically patched.

Dedicated-server and broad multiplayer behavior have **not** been exhaustively validated; please report logs for those environments before assuming full support.

## Credits

- **PathOfValheiman** by treextr
- **LetMeTameYou** by Meldurson
- **BepInEx / BepInExPack Valheim** and the Valheim modding ecosystem

This repository contains the adapter code only. It does not redistribute PathOfValheiman, LetMeTameYou, Valheim game assemblies, or BepInEx binaries.

## Development history

See [`CHANGELOG.md`](CHANGELOG.md) for release-by-release changes and [`docs/DEVELOPMENT_HISTORY.md`](docs/DEVELOPMENT_HISTORY.md) for the full debugging trail and root-cause analysis.
