# POV LMTY BeastAI Adapter v0.3.1

v0.3.1 is the first release considered stable enough for normal use in the tested POV/LMTY setup.

## Highlights

- Generic structural compatibility detection; no Deer/Hare prefab allowlist.
- Preserves PathOfValheiman `PassiveTree.BeastAI` instead of replacing it.
- Automatically repairs the confirmed `MonsterAI + Character + no Humanoid + no AnimalAI` POV shape.
- Leaves normal LMTY creatures untouched.
- Restores configured food consumption for repaired POV BeastAI creatures.
- Adds an adaptive taming fallback only after confirming that the native timer is stalled.
- Restores `Procreation` and offspring configuration, including explicit `offspringPrefab` support.
- Retains the 0.2.7 performance fix that removed the frequent scene-wide item scan.

## Final validation

Validated on Valheim 1.0.20 with POV 4.13.10 and LMTY 0.1.0:

- 4-star Deer: food -> automatic stalled-timer detection -> fallback -> taming complete.
- Deer breeding: pregnancy and birth observed.
- 3-star Hatchling: original LMTY path, taming complete without adapter intervention.
- Hatchling breeding and egg laying observed in prior validation.
- Hare and Seal: generic POV incompatible structure detected; Seal explicit `Seal_Pup` offspring configuration applied.
- Final validation log: no `NullReferenceException`, general `Exception`, or `[Error` entry.

## Known limitation

The adapter does not currently reimplement LMTY's special `egg:` reproduction initialization for a creature that also matches the repaired POV BeastAI structure. Such a case is logged and deferred instead of guessed.

Dedicated-server and broad multiplayer behavior are not yet exhaustively validated.
