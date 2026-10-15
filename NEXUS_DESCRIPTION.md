# POV LMTY BeastAI Adapter

Compatibility patch for **PathOfValheiman** + **LetMeTameYou**.

## What it fixes

PathOfValheiman can convert some normally passive creatures (such as Deer/Hare-style prefabs) into its custom `PassiveTree.BeastAI`. In the tested POV 4.13.10 + LetMeTameYou 0.1.0 combination, that can leave LMTY with a MonsterAI-compatible creature that has no `Humanoid` and no longer has `AnimalAI`, causing LMTY's normal setup path to fail.

This adapter detects that exact POV structure and applies the LMTY taming/breeding configuration without replacing POV's AI.

## Features

- no prefab-name allowlist;
- high-confidence POV AI detection;
- normal LMTY creatures are left untouched;
- food bridge for repaired POV BeastAI creatures;
- adaptive taming fallback only when the native timer is actually stalled;
- breeding and offspring setup, including explicit `offspringPrefab` support;
- performance fix for the earlier food-search implementation.

## Requirements

- BepInExPack Valheim
- PathOfValheiman
- LetMeTameYou

BetterTames is optional and was present during testing, but is not required by this plugin.

## Tested

Final validation used Valheim 1.0.20, PathOfValheiman 4.13.10 and LetMeTameYou 0.1.0.

Observed working:

- 4-star Deer taming;
- Deer feeding, pregnancy and birth;
- 3-star Hatchling taming through original LMTY;
- Hatchling breeding and egg laying;
- generic Hare/Seal compatibility detection;
- no `NullReferenceException`, general `Exception`, or `[Error` in the final validation log.

## Installation

Extract the archive into the Valheim game directory so the DLL ends up at:

```text
BepInEx/plugins/POVLMTYBeastAIAdapter/POVLMTYBeastAIAdapter.dll
```

Do not run older versions of this adapter, `POVLMTYDeerAdapter`, or the old `POVDeerBreedingFix` at the same time.

## Known limitation

The custom LMTY `egg:` initialization path is not generically reimplemented for a POV creature that also requires this adapter's structural repair.

Dedicated-server and broad multiplayer combinations are not yet exhaustively validated.

## Credits

PathOfValheiman by treextr. LetMeTameYou by Meldurson. This download contains only the compatibility adapter and does not redistribute either mod or Valheim game files.
