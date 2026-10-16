# Changelog

## 0.3.3 - Fed-state timer initialization fix

Current stable release.

- Fixed an adaptive-fallback dead state where a creature could be `hungry=False` while `TameTimeLeft` was still missing (`-1`).
- If a valid `TameLastFeeding` timestamp exists but `TameTimeLeft` does not, the adapter now safely initializes the countdown from the configured taming time and starts the normal native-observation/fallback flow.
- Never-fed creatures are not initialized: the repair requires a real feeding timestamp.
- No changes to the generic POV structure detection, food list parsing, breeding configuration, or offspring logic.
- Live validation confirmed a 4-star Deer progressed from the repaired 120-second countdown to `tamed=True`; the final log contained no `NullReferenceException`, general `Exception`, or `[Error` entry.

## 0.3.2

- Updated the project and Thunderstore icon with a new custom emblem.
- Bumped package/plugin version to 0.3.2.
- No gameplay or compatibility logic changes from 0.3.1.


## 0.3.1 - Generic compatibility + adaptive taming fallback

- Removed prefab-name dependency from the compatibility decision.
- Retained high-confidence POV structural detection introduced in 0.3.0.
- Added adaptive taming fallback:
  - observes the native timer for six calm seconds after feeding;
  - keeps fallback disabled if native progress is detected;
  - enables fallback only when the timer is confirmed stalled;
  - ignores alerted time during the observation window.
- Stopped periodic taming-progress diagnostics for already-tamed creatures while keeping the food bridge active for breeding.
- Validated 4-star Deer taming through fallback completion.
- Validated 3-star Hatchling taming through untouched original LMTY path.
- Final validation log contained no `NullReferenceException`, general `Exception`, or `[Error` entry.

## 0.3.0 - Generic structural auto-repair

- Removed the Deer/Hare/Hatchling prefab allowlist.
- Inspects every creature passed to `LetMeTameYou.TameManager.AddTame()`.
- Auto-repairs only the confirmed POV incompatible shape:
  - `MonsterAI != null`
  - `Character != null`
  - `Humanoid == null`
  - `AnimalAI == null`
  - high-confidence POV AI identification.
- Unknown/non-POV custom AI with a similar shape is logged but not modified.
- Added generic `offspringPrefab` support, validated with `Seal -> Seal_Pup` configuration.
- Kept normal LMTY creatures on the original LMTY path.
- Intentionally did not reimplement LMTY's special `egg:` path for incompatible POV targets.

## 0.2.7 - Stable pre-generic repair

- Replaced unsupported `ItemDrop.s_instances` approach with `FindObjectsByType<ItemDrop>(FindObjectsSortMode.None)`.
- Removed the frequent all-scene `ItemDrop` scan that caused periodic hitches.
- Added target caching and throttled searches based on `consumeSearchInterval`.
- Added taming-progress fallback using `Tameable.DecreaseRemainingTime()` / `Tame()` when native progress was absent.
- Preserved POV `PassiveTree.BeastAI` while restoring LMTY feeding/taming/breeding behavior.
- Deer end-to-end feeding, taming, breeding and offspring behavior became functional.

## 0.2.6 - Development-only / build failed

- Attempted to replace scene-wide food searches with `ItemDrop.s_instances`.
- Did not compile against Valheim 1.0.20 because `ItemDrop.s_instances` does not exist in that game assembly.
- The design was corrected in 0.2.7.

## 0.2.5 - Runtime bridge diagnostics

- Prevented repeated resetting of `TameTimeLeft`.
- Added detailed taming-progress logs.
- Confirmed food consumption and `hungry=False` state.
- Revealed that `TameTimeLeft` itself remained frozen.
- Exposed a periodic hitch caused by frequent scene-wide item scans.

## 0.2.4 - Feeding-state bridge

- Replaced the unavailable `Tameable.ResetFeedingTimer()` call with direct feeding-state/ZDO updates.
- Added runtime food-consumption bridge for POV BeastAI creatures.

## 0.2.3 - Development-only / build failed

- First runtime BeastAI consume-bridge attempt.
- Did not compile against Valheim 1.0.20 because `Tameable.ResetFeedingTimer()` was not available.

## 0.2.2 - Food parsing and diagnostics

- Added flexible food parsing for comma/colon/semicolon separators.
- Added resolved/missing-food diagnostics.
- Confirmed Deer food prefabs resolved correctly, ruling out the YML parser as the cause of non-feeding behavior.

## 0.2.0 - Multi-target adapter branch

- Expanded beyond Deer to Deer/Hare/Drake/Hatchling test targets.
- Introduced structural interception of POV's incompatible MonsterAI/no-Humanoid/no-AnimalAI shape.
- Preserved compatible LMTY targets on the original code path.

## 0.1.1 - Diagnostic expansion

- Added tracing that confirmed Hare exhibited the same POV/LMTY component mismatch as Deer.

## 0.1.0 - Deer proof of concept

- Added a Harmony Prefix around `LetMeTameYou.TameManager.AddTame()`.
- Bypassed LMTY only for the known POV-incompatible Deer structure.
- Preserved POV BeastAI.
- Applied LMTY `Tameable`, food, `Procreation`, and mini-offspring settings directly.
