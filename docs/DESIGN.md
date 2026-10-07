# Spa & Sauna: Design (as implemented)

This file describes the mechanics as they exist in the code and defs today. Code comments refer to an original design spec, with its "Resolved Decision #N", "Phase 1–4" and "Asset Pipeline section". That spec is **not in the repo**. If you still have it, put it in `docs/` so those references resolve.

## 1. Sauna room

A room gets the `Sauna` role (`RoomRoleWorker_Sauna`, score 10000) when it:

- contains or touches at least one building with `CompSaunaHeater`,
- contains or touches at least one sittable building (`building.isSittable`),
- doesn't touch the map edge and isn't a doorway,
- has heaters of **only one archetype**. A 1x1 and a 2x2 of the same archetype can share a room.

Temperature and decor do **not** affect the role. They only gate or scale the buff when a session ends.

### One heater type per room

This rule is enforced in three layers (`SaunaMixedHeaters.cs`), so the player always sees why:

1. **Placement is refused** (`PlaceWorker_SaunaNoMixedHeaters`). Placing a heater in an enclosed room that already has a heater, blueprint or frame of another archetype turns the ghost red. The cursor shows "Only one sauna heater type per room: this room already has a Detoxified heater", and each conflicting heater gets a red outline and a red line from the ghost. Unenclosed areas, such as before the walls are up, aren't checked.
2. **A pulsing red ✕ appears over every heater involved** if a room becomes mixed anyway, for example when a wall is removed and two saunas merge (`MapComponent_SaunaMixedHeaters`). Each heater re-checks every 120 ticks.
3. **An alert, "Mixed sauna heaters"** (`Alert_SaunaMixedHeaters`, high priority), lists those heaters, and clicking it jumps to them. The heater's inspect panel names the clashing types, and the right-click option shows as a disabled "Use sauna (mixed heater types in this room)".

The consequence: a mixed room isn't a Sauna, so no sessions start there and none pay out.

## 2. Session flow

| Step | Where |
|---|---|
| The joy AI picks a heater from the JoyGiverDef's `thingDefs`. The heater must be powered, not forbidden and socially proper, and in a Sauna room with a reservable seat. | `JoyGiver_Sauna` |
| Or the player right-clicks the heater and picks **Use sauna**. | `Building_SaunaHeater` |
| The pawn walks to the seat and relaxes for `joyDuration` = 2500 ticks (1 in-game hour). They gain joy, comfort and (see §6) psyfocus. | `JobDriver_UseSauna` |
| All or nothing: the buff is only applied if the toil ran to completion (`ticksLeftThisToil <= 0`). | `JobDriver_UseSauna.ApplyRoomBuff` |
| When the session ends, the room is checked again. It must still be a Sauna, and its temperature must be within the heater's `[minGateTemperature, maxGateTemperature]`. | same |
| Apparel is hidden while any sauna job runs. This only affects rendering; the clothes stay equipped. | `HarmonyPatches_Sauna` |

**Heatstroke gate:** a pawn whose Heatstroke severity is 0.2 or higher can't start a session, and an ongoing session fails. A cold plunge removes heatstroke.

## 3. Heater archetypes

All heaters are electric, need the `Sauna_Research` project, and come in two sizes:

| | 1x1 | 2x2 |
|---|---|---|
| Base power | 800 W | 1000 W |
| Rated room size | 12 tiles | 25 tiles |
| Heat push (`heatRatePerInterval`, °C per 60 ticks) | 2.5 | 5 |
| Cost | 100 Steel | 250 Steel + 80 Stony stuff |
| Mote frequency | every 250 ticks | every 125 ticks |

Every tile above the rated size adds 5% to the power draw. The heater pushes heat scaled by `room.CellCount`, so large rooms still warm up at the same rate (`CompSaunaHeaterClimateControl`).

It stops pushing once the room reaches the **room heating ceiling**: the lowest `maxGateTemperature` among all powered sauna heaters in the room (`SaunaUtility.GetRoomHeatingCeiling`). For a valid sauna (one archetype) this is just the heater's own maximum. It only matters while a merged room is mixed (see §1): there it stops the hotter heater from cooking the room until the player fixes it.

| Archetype | Heater flavor | JoyKind | Gate °C | Buff at 100% intensity | Effect |
|---|---|---|---|---|---|
| **Serenity** | meditation brazier | Meditative | 30–40 | Mental break threshold −10%; also grants **Vitality** (pain ×0.90) and **Clarity** (+15% learning) | thin smoke |
| **Detoxified** | dry Finnish heater | Hydrotherapy | 70–90 | Immunity gain +15%, toxic resistance +15%, addiction recovery +50% | none (glow only) |
| **Purified** | low-heat Laconium | Hydrotherapy | 40–55 | Immunity gain +15%, injury healing +15% | heat glow |
| **Radiant** | view-oriented / glass | BathRelaxing | 30–40 | Social impact +15%, beauty +3, negotiation +15%, conversion power +20% (Ideology) | micro sparks |
| **Rejuvenated** | candlelit ambience | BathRelaxing | 32–42 | Rest fall rate −20%, lifespan +5%, cancer rate −20% (Biotech) | fire glow |
| **Invigorated** | löyly infusion | Hydrotherapy | 70–90 | Move speed +0.05, global work speed +10% | air puff |
| **Cleansed** | banya/hammam wet steam | Hydrotherapy | 45–60 | Tend quality +15%, surgery success +10%, food poison chance −15%, cleaning speed +15% | dense room-filling steam (custom flecks) |

Every archetype buff:

- has mood +6 × intensity (+6.0 to +9.0) through a `ThoughtWorker_Hediff`,
- lasts 30000–60000 ticks (12–24 in-game hours),
- has its severity set to the intensity multiplier.

The 10 identical-looking stages in each HediffDef are on purpose: they let the paired ThoughtDef's mood stage track severity. The header comment in `HediffDefs_Sauna.xml` explains why.

The three JoyKinds (vanilla `Meditative` plus the custom `Hydrotherapy` and `BathRelaxing`) map to three JobDefs and three JoyGiverDefs. Because of that, the joy AI's variety logic treats the archetypes as different kinds of recreation.

## 4. Facilities and intensity

Decor uses vanilla's facility linking (`CompFacility` / `CompAffectedByFacilities`, default range 8). Each item also has a `CompSaunaFacility.facilityBonus`.

```
intensity = min(1.5, 1 + 0.05 × (sum of the top 3 facilityBonus values among active linked facilities))
```

If more than one heater is in the room, intensity and archetype come from the heater the session's job targeted.

| Facility | Bonus | Cost | Notes |
|---|---|---|---|
| Singing bowl | 3 | 35 Wood | |
| Candle rack | 2 | 25 Wood | |
| Prayer candle bundle | 2 | 25 Wood | Serenity-themed reskin of the candle rack |
| Moody glowlight | 2 | 15 Steel | has a glower |
| Herb basket | 4 | 15 Wood | refuels with herbal medicine. The higher bonus is meant to pay for the upkeep. |
| Small waterfall | 3 | 60 Steel | |
| Heated stone pile | 3 | 35 Stony stuff | |

"Fully decorated" means 3 active linked facilities. It unlocks psyfocus (§6) and doubles Deeply Relaxed gain.

## 5. Cross-cutting buffs

All of these are granted on a completed session, alongside the archetype buff.

| Buff | Who gets it | Effect |
|---|---|---|
| Heat-Numbed | everyone | Pain shock threshold +10%, max comfortable temperature +8 °C (scales with intensity) |
| Fresh Air | everyone | +2 mood |
| Sauna Bonding | another colonist is also in a sauna job in the room when the session ends | +4 mood |
| Eased Joints | age 50+ or has Bad Back or Frail | +3 mood |
| Deeply Relaxed | everyone | Severity is the stack count: +1 per session (+2 if fully decorated), cap 25, decays by 1 per day. Each stack lowers the mental break threshold by 1%. |

## 6. Psyfocus (Royalty)

Psyfocus can come from two places:

- Each heater has a `Minimal` meditation focus. It adds +25% focus strength **only when the room is fully decorated** (`FocusStrengthOffset_SaunaFullyDecorated`).
- A psycaster using the sauna job also gains psyfocus during the session, but only when the room is fully decorated.

## 7. Cold plunge tub

- 2x2, 50 Steel, needs Sauna research. It is standalone: it doesn't link to facilities and doesn't count toward the facility cap.
- **Hard gate:** only usable while the pawn has one of the 7 archetype buffs. This applies to both the joy AI and right-click orders.
- The session takes 417 ticks (~10 in-game minutes) and is all or nothing.
- It grants **Cold Plunge** (pain ×0.85, `ComfyTemperatureMin` +8, fixed strength) and removes any Heatstroke.

## 8. Other buildings

- **Sauna bench:** sittable, comfort 0.8. Built from Woody or Stony stuff (60) and tinted by the material. Adjacent benches join into one row through a linked graphic (`linkFlags Custom1`). Not rotatable.
- **Cosmetic only** (beauty, no mechanics): towel rack, robe hook, locker, lounge chair (sittable), thermometer, outdoor cooling bench (sittable).

## 9. Code map

| File | Responsibility |
|---|---|
| `SpaMod.cs` | Harmony bootstrap |
| `SaunaDefOf.cs` | DefOf references |
| `SaunaUtility.cs` | Facility counting and intensity, seat search, archetype→JobDef mapping, buff and heatstroke checks |
| `CompSaunaHeater.cs` | Archetype and tier enums, heater props, temperature gate, inspect string |
| `CompSaunaHeaterClimateControl.cs` | Dynamic power draw and capped heat push |
| `CompSaunaHeaterMotes.cs` | Per-archetype flecks |
| `CompSaunaHeaterGlowOverlay.cs` | Pulsing ember overlay on powered 1x1 heaters |
| `CompSaunaFacility.cs` | `facilityBonus` |
| `RoomRoleWorker_Sauna.cs` | Room role scoring (rejects mixed rooms) |
| `SaunaMixedHeaters.cs` | One-type-per-room rule: placement check, ✕ overlay, alert |
| `JoyGiver_Sauna.cs` / `JobDriver_UseSauna.cs` | Sauna session AI and job, buff application |
| `Building_SaunaHeater.cs` | "Use sauna" float menu |
| `JoyGiver_ColdPlungeTub.cs` / `JobDriver_UseColdPlungeTub.cs` / `Building_ColdPlungeTub.cs` | Cold plunge |
| `HediffComp_DetoxifiedAddictionRecovery.cs` | Addiction recovery boost for Detoxified |
| `FocusStrengthOffset_SaunaFullyDecorated.cs` | Royalty meditation focus offset |
| `HarmonyPatches_Sauna.cs` | Hide apparel during sessions |
