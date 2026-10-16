# Technical details

## Plugin identity

```text
GUID:    com.rehyun.povlmtybeastaiadapter
Name:    POV LMTY BeastAI Adapter
Version: 0.3.3
Target:  netstandard2.1
```

Hard BepInEx dependencies declared by the plugin:

```text
treextr.pathofvalheiman
meldurson.LetMeTameYou
```

## Harmony interception

The plugin locates `LetMeTameYou.TameManager.AddTame(GameObject, ...)` by reflection and installs a Harmony Prefix.

The Prefix performs a trace for every LMTY target, then decides whether to:

- return `true`: original LMTY handles the prefab;
- return `false`: adapter applied the compatible LMTY settings directly.

## Conservative POV identification

A matching structural shape is not enough by itself. The runtime MonsterAI type must also be identified as POV with high confidence.

This prevents the adapter from taking over a similar component shape created by an unrelated AI mod.

## Food bridge

The bridge is added only on repaired POV targets. It uses LMTY's resolved food configuration already applied to `MonsterAI.m_consumeItems`.

Searches are throttled and target-cached to avoid the performance issue observed in the 0.2.4/0.2.5 development branch.

## Taming fallback

The bridge watches `TameTimeLeft` after feeding. It first gives the game/LMTY native logic an opportunity to progress.

If the creature is already fed but `TameTimeLeft` is absent, v0.3.3 initializes the missing countdown only when a valid `TameLastFeeding` timestamp exists. This avoids starting taming on never-fed creatures.

Only after a six-second calm observation window with no progress does it invoke the fallback countdown path.

## Breeding

`Procreation` is added only when LMTY configuration enables procreation. The adapter applies max creature count, pregnancy chance/duration and configured ranges, then resolves offspring through existing/explicit/mini paths.

## Logging philosophy

The adapter favors explicit diagnostics because compatibility failures are highly dependent on runtime prefab shape and mod versions.

When reporting a problem, include the surrounding lines beginning with:

```text
[LMTY Trace] BEGIN prefab=<name>
```

and any later `Generic Compatibility`, `BeastAI Adapter`, `BeastAI Feed`, or `Adaptive Taming` messages.
