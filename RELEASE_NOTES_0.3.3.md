# POV LMTY BeastAI Adapter v0.3.3

v0.3.3 fixes a taming-progress dead state discovered while validating the Gale/Thunderstore-installed package.

## Fix

A repaired POV BeastAI creature could become fed (`hungry=False`) while its ZDO still had no `TameTimeLeft` value (`-1`). In that state, v0.3.1/0.3.2 could return before starting the native-timer observation, so adaptive taming never began.

v0.3.3 now initializes the missing countdown **only when a real `TameLastFeeding` timestamp already exists**. It then keeps the existing conservative behavior: observe native taming for six calm seconds, leave native progress alone if it works, and activate fallback only when the native timer is confirmed stalled.

## Live validation

Validated on Valheim 1.0.20 with POV 4.13.10, LMTY 0.1.0 and BetterTames 0.9.0 present in the test profile:

- 4-star Deer matched the generic POV incompatible structure and was auto-repaired.
- Missing `TameTimeLeft` was initialized to the configured 120 seconds after a valid feeding timestamp was detected.
- Native progress remained stalled during the 6-second observation window, so fallback activated.
- Fallback reduced the timer from 120 seconds to 0 and completed taming with `tamed=True`.
- No `NullReferenceException`, general `Exception`, or `[Error` entry was present in the validation log.

## Unchanged behavior

- No prefab-name allowlist was added.
- POV `PassiveTree.BeastAI` is still preserved.
- Normal LMTY creatures continue through original LMTY untouched.
- Food parsing, breeding configuration, offspring handling, and the special `egg:` limitation are unchanged.
