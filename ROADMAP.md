# Roadmap & Status

Last reviewed: 2026-10-07. The last commit is `c33a68d` (initial v1). The latest DLL in `1.6/Assemblies` was built after the newest source edit.

## Done (committed in v1)

- [x] Sauna room role: heater plus seat, scored by `RoomRoleWorker_Sauna`
- [x] 7 archetypes × 2 sizes = 14 heater defs, each with its own temperature gate, motes and JoyKind
- [x] All-or-nothing 1-hour session job, joy AI, and right-click "Use sauna"
- [x] Archetype buffs with intensity-scaled stats and mood (10-stage workaround)
- [x] Cross-cutting buffs: Heat-Numbed, Vitality, Clarity, Sauna Bonding, Fresh Air, Eased Joints, Deeply Relaxed
- [x] 7 decor facilities feeding the top-3 intensity formula
- [x] 6 cosmetic buildings
- [x] Cold plunge tub: hard-gated on a sauna buff, cures heatstroke
- [x] Dynamic power draw and capped heat push (`CompSaunaHeaterClimateControl`)
- [x] Heatstroke safety gate
- [x] Royalty psyfocus, plus Ideology and Biotech stats behind `MayRequire`
- [x] Apparel hidden during sessions (Harmony)
- [x] Research project (`Sauna_Research`, needs Electricity)

## In progress (uncommitted in the working tree)

- [ ] Sauna bench: real linked atlas art, Wood/Stony stuff tinting, `BuildingBase` parent, UI icon
- [ ] 1x1 heaters: real `SaunaHeaterElectric` art plus the `CompSaunaHeaterGlowOverlay` ember pulse
- [ ] Cleansed 1x1: dedicated tall steam-panel art (`SaunaHeaterSteam1x1`)
- [ ] 2x2 heaters (all except Cleansed): shared `SaunaHeater2x2_Purified` art, built from Stony stuff
- [ ] Cleansed: dense room-filling steam using the custom `Sauna_Steam1/2` FleckDefs

**Next step:** test these in game, then commit. `ArtSource/` and the new textures are untracked.

## Bugs found in review

Fixed 2026-10-07. The build passes, but none of these fixes have been tested in game yet.

- [x] **2x2 heaters were never used by the joy AI.** The `_2x2` defNames are now listed in the matching JoyGiverDefs.
- [x] **The cold plunge made cold tolerance worse.** `ComfyTemperatureMin` went from +8 to −8.
- [x] **The glow overlay never drew.** `SaunaHeaterBase_1x1` now uses `drawerType MapMeshAndRealTime`, and the comp is `[StaticConstructorOnStartup]`. The cause was confirmed by decompiling: `MapMeshOnly` skips `DynamicDrawPhase`.
- [x] **Facilities and cosmetics didn't inherit `BuildingBase`.** Both now do. The herb basket also gets an explicit `tickerType Normal`, because `BuildingBase` doesn't set one, so its fuel actually drains now.
- [x] **Rooms with more than one heater used an arbitrary heater.** The buff now comes from the job's target heater. It only falls back to any heater in the room if the target is gone.

Still open:

- [ ] **Intensity isn't clamped.** `GetIntensityMultiplier` can go above 1.5. For example, 3 herb baskets give 12 points, which is 1.6. The hediff's `maxSeverity` clamps it in practice. The comment in `ThoughtDefs_Sauna.xml` that says the "max reachable sum is 10" is wrong. Fix: clamp explicitly.
- [ ] **Two heaters in one room can fight over temperature.** For example, a Serenity heater stops at 40 °C while a Detoxified heater keeps pushing to 90 °C. Decide whether to block mixed archetypes in one room.

In-game test checklist for the fixes:

- [ ] A colonist uses a 2x2 heater without being ordered to.
- [ ] The ember glow pulses on powered 1x1 heaters and disappears when they're switched off.
- [ ] The herb basket's fuel drains.
- [ ] Facilities and cosmetics still render.
- [ ] Cold Plunge shows −8 °C on the minimum comfortable temperature.

## Cleanup

- [ ] Fix stale comments:
  - `CompSaunaHeater.cs` mentions `CompSaunaHeaterPowerScaling` and `CompProperties_HeatPusher.heatPerSecond`. Both are now `CompSaunaHeaterClimateControl` / `heatRatePerInterval`.
  - The 2x2 header in the heater XML says "heatPerSecond 7 vs. 3.5". The actual values are 5 vs. 2.5.
  - The 1x1 base comment lists the wrong comps.
  - `RoomRoleDefs_Sauna.xml` says "no temperature gate yet".
  - The parameter-name comment in `HarmonyPatches_Sauna.cs` is garbled.
- [ ] Move `Common/Textures/sauna.png` (unused 1254px concept art, no alpha) to `ArtSource/`. Consider moving `SaunaHeaterElectric_m.png` too, which isn't used at runtime.
- [ ] Check whether `SaunaSeating_Blueprint.png` is actually used. Vanilla linked buildings wire blueprints through `blueprintGraphicData`.
- [ ] Lounge chair and outdoor cooling bench use `Graphic_Multi` but `rotatable=false`, so they always face south.
- [ ] Put the design spec in `docs/` so the "Resolved Decision #N" references in comments resolve.

## To do: features and content

- [ ] **Art.** In-game textures for:
  - Facilities. Art is ready in `ArtSource/` for the candle rack, heated stone pile, moody glowlight, prayer candle bundle, singing bowl and waterfall (`SaunaSmallRockWaterfall`). Move each into `Common/Textures/` and update `texPath`.
  - Herb basket (no art yet)
  - Cleansed 2x2 (dedicated steam-bath art; still uses the vanilla heater placeholder)
  - Cold plunge tub (placeholder MultiAnalyzer). `SaunaIceBarrel.png` may fit here.
  - All 6 cosmetics
- [ ] Decide what to do with the unused art: `SaunaEncens.png` (incense), `SaunaSmallCrystal.png`, `SaunaIceBarrel.png`. They could become new facilities, or replace art for existing items.
- [ ] Herb basket: when empty, stop counting toward linked facilities (custom active check).
- [ ] Waterfall: add a PlaceWorker that requires a stone floor (from the spec).
- [ ] Localization: move hardcoded English strings into `Languages/English/Keyed/`. These include the float menu labels, the inspect string and the focus explanation.
- [ ] Balance pass in game:
  - Heatstroke exposure at 70–90 °C (Detoxified and Invigorated)
  - Invigorated's `MoveSpeed +0.05` is an absolute offset, about +1%. Is that intended?
  - Stone 2x2 heaters have Flammability 1.0
  - Power cost
  - Research cost and tree position (`researchViewX/Y = 6,6`)
- [ ] Release prep:
  - `About/Preview.png`
  - `modVersion` and `url` in About.xml
  - Exclude `ArtSource/` and `Source/` from the Workshop upload
  - Ship a release build of `1.6/Assemblies/SpaMod.dll` (it's gitignored)

## Later

- [ ] Hospitality soft-dependency: guest billing and a spa staff role, as announced in About.xml
