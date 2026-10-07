# Spa & Sauna (RimWorld 1.6)

A RimWorld mod that adds buildable sauna rooms. **Sauna** is a room role, like Bedroom or Hospital. A room counts as a sauna when it has a sauna heater and something to sit on. Colonists relax there for an in-game hour. When they finish, they get a mood buff and a set of stat effects. The heater variant you build decides which buff they get.

- **Package ID:** `johnbrimstone.spa`
- **Supported version:** 1.6
- **Requires:** [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)
- **Optional DLC hooks:** Royalty (psyfocus), Ideology (conversion power), Biotech (cancer rate). The mod runs without any of these.

## Features

- **7 heater archetypes**, each built in a 1x1 and a 2x2 size: Serenity, Detoxified, Purified, Radiant, Rejuvenated, Invigorated, Cleansed. Each one has its own buff, its own target temperature range, its own particle effect and its own recreation type.
- **Room-driven buffs.** A session only counts if the pawn stays the full hour. Partial sessions give nothing. The room also has to be inside the heater's temperature range when the session ends.
- **Decor facilities.** Up to 3 linked decorations raise room intensity from 100% to as much as 150%. Mood and stat effects scale with intensity.
- **Cross-cutting buffs:** Heat-Numbed, Fresh Air, Sauna Bonding, Eased Joints (elderly pawns), and a slowly stacking Deeply Relaxed effect.
- **Cold plunge tub.** Contrast therapy after a sauna session. It cures heatstroke and gives its own buff.
- **Smart heating.** Heaters warm the room up to their range and stop there. Power draw goes up when the room is larger than the heater is rated for.
- **Flavor:** pawns are drawn without clothing during a session (they stay equipped), each archetype has its own steam or smoke effect, and right-click "Use sauna" orders are available.

See [docs/DESIGN.md](docs/DESIGN.md) for the full mechanics and numbers, and [ROADMAP.md](ROADMAP.md) for what is done and what is left.

## Repository layout

```
About/                 About.xml, LoadFolders.xml (mod metadata)
1.6/Defs/              All XML defs, one folder per def type
1.6/Assemblies/        Compiled SpaMod.dll (build output, gitignored)
Common/Textures/       In-game textures (version-independent)
Source/SpaMod/         C# source + SpaMod.csproj
ArtSource/             Raw and superseded art. Not loaded by the game; exclude it from Workshop uploads.
docs/                  Design documentation
```

## Building

You need the .NET SDK. The project targets `net472` and uses NuGet reference assemblies (`Krafs.Rimworld.Ref`, `Lib.Harmony.Ref`), so you don't need a local RimWorld install to compile.

```bash
dotnet build "Source/SpaMod/SpaMod.csproj"
```

After a build, `SpaMod.dll` and `SpaMod.pdb` are copied automatically into `1.6/Assemblies/`.

## Testing in game

1. Link or copy this folder into `RimWorld/Mods/`. A directory junction works well on Windows.
2. Enable Harmony, then Spa & Sauna, in the mod list.
3. Research **Sauna** (needs Electricity). Build a heater and a sauna bench in an enclosed room, then power the heater.
4. Right-click the heater with a colonist selected and choose **Use sauna** to start a session without waiting for the joy AI.
