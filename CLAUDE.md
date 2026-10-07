# CLAUDE.md

A RimWorld 1.6 mod, "Spa & Sauna" (`johnbrimstone.spa`). It uses C# with Harmony plus XML defs.

- Overview and build steps: [README.md](README.md)
- Mechanics and numbers: [docs/DESIGN.md](docs/DESIGN.md)
- Status, known bugs and to-dos: [ROADMAP.md](ROADMAP.md). Keep it current when you finish or find work.

## Build

```bash
dotnet build "Source/SpaMod/SpaMod.csproj"
```

The build targets net472 against `Krafs.Rimworld.Ref` 1.6.4871 and `Lib.Harmony.Ref`. A post-build target copies the DLL and PDB into `1.6/Assemblies/`, which is gitignored. There are no automated tests. Verification means checking in game, usually through the right-click "Use sauna" order.

## Layout

- `1.6/Defs/<DefType>/` holds XML defs. Load order is set in `About/LoadFolders.xml`: `Common`, then `1.6`.
- `Common/Textures/` holds textures. They are flat (no subfolders), so `texPath` is just the file name.
- `ArtSource/` holds raw and superseded art. The game doesn't load it. Move superseded art here instead of deleting it.
- `Source/SpaMod/` uses one namespace, `SpaMod`. Classes follow vanilla naming (`CompX` + `CompProperties_X`, `JobDriver_X`, `JoyGiver_X`).
- Every def name starts with `Sauna` or `Sauna_`.

## Conventions and hard-won lessons

- **Comments explain why.** They record verified engine behaviour ("confirmed via decompile"). Keep that density and style. Don't assert engine behaviour you haven't checked.
- **XML inheritance replaces list fields; it doesn't merge them.** Each concrete heater repeats its full `<comps>` block. If you change one heater's comps, update all 14 (7 archetypes × 1x1/2x2), or deliberately skip some (Detoxified has no motes; only the 1x1 non-Cleansed heaters have the glow overlay).
- **Buildings must set `ParentName="BuildingBase"`.** Without it, a def falls back to `drawerType RealtimeOnly` (linked graphics break) and loses rubble, repair and refund behaviour.
- **Set `<tickerType>Normal</tickerType>` explicitly** on anything whose comps need `CompTick` (heaters, herb basket). ThingDef's default is `Never`, and vanilla `BuildingBase` does **not** override it (see Core `Buildings_Base.xml`).
- **Comp `PostDraw()` only runs for `RealtimeOnly` or `MapMeshAndRealTime` drawers.** `MapMeshOnly` skips `DynamicDrawPhase` entirely. Overlay comps need `MapMeshAndRealTime` (the 1x1 heater base sets it).
- **Verify engine behaviour by decompiling.** A RimWorld install is at `M:/SteamLibrary/steamapps/common/RimWorld` (vanilla XML is in `Data/*/Defs`), and `ilspycmd` is installed: `ilspycmd -t Verse.Thing ".../RimWorldWin64_Data/Managed/Assembly-CSharp.dll"`.
- **Never mutate shared `Props`.** Per-instance values, such as the scaled power draw, go on the comp instance (`CompPowerTrader.PowerOutput`).
- **Intensity scaling** uses Hediff severity with `multiplyStatChangesBySeverity`. Mood mirrors the hediff stage through `ThoughtWorker_Hediff`, which is why each buff has 10 identical stages at the reachable severities. If you change facility bonus values, recompute those stage thresholds in both the HediffDefs and the ThoughtDefs.
- **New heater variants should be XML only.** `CompProperties_SaunaHeater` carries archetype, tier, rated size and temperature gate. A new archetype also needs:
  - the `SaunaArchetype` enum value,
  - the switch cases in `SaunaUtility.JobDefForArchetype`, `JobDriver_UseSauna.HediffDefForArchetype` and `SaunaUtility.HasActiveSaunaBuff`,
  - a HediffDef and ThoughtDef pair,
  - an entry in the right JoyGiverDef's `thingDefs`.
- **DLC content** goes behind `MayRequire="Ludeon.RimWorld.<DLC>"`. The mod must load without any DLC.
- **Material tinting:** use `stuffCategories` + `costStuffCount` with a plain `Cutout` texture whose fill is light grey. Outlines stay black and the fill takes the material colour.
- The user is the art author (referred to as "John" in comments) and supplies the PNGs. Don't redraw art; crop or wire up what's provided.
