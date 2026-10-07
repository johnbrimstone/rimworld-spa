using RimWorld;
using UnityEngine;
using Verse;

namespace SpaMod
{
    public class CompProperties_SaunaHeaterMotes : CompProperties
    {
        public CompProperties_SaunaHeaterMotes()
        {
            compClass = typeof(CompSaunaHeaterMotes);
        }
    }

    // Steam/smoke visual flavor per archetype, matching the design spec's "match the
    // theme" table. Uses FleckMaker (the modern lightweight-particle API) rather than
    // MoteMaker — verified by decompiling vanilla's own Building_SteamGeyser, which
    // throws its steam via FleckMaker.ThrowAirPuffUp, not a mote. Detoxified isn't
    // attached to this comp at all (dry heater: glow only, no smoke/steam per spec).
    public class CompSaunaHeaterMotes : ThingComp
    {
        // Ticks between throws for a 1x1 heater; a 2x2 would throw twice as often
        // (Resolved Decision #30 — kept as pure flavor after heater size stopped
        // driving buff intensity).
        private const int BaseIntervalTicks = 250;

        // Cleansed (steam bath) gets a much denser, room-filling effect instead of the
        // single per-heater puff every other archetype uses — John asked for "a lot" of
        // steam specifically for this archetype, not just a stronger puff at the heater's
        // own tile. Uses the real supplied steam1.png/steam2.png art (Sauna_Steam1/2
        // FleckDefs) instead of vanilla's AirPuff preset, previously unused in the mod.
        private const int CleansedSteamIntervalTicks = 30;
        private const int CleansedSteamPuffsPerInterval = 3;

        public override void CompTick()
        {
            base.CompTick();

            if (!parent.Spawned)
            {
                return;
            }

            CompSaunaHeater heaterComp = parent.TryGetComp<CompSaunaHeater>();
            if (heaterComp == null)
            {
                return;
            }

            CompPowerTrader power = parent.TryGetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
            {
                return;
            }

            bool isLarge = heaterComp.Props.heatTier == SaunaHeatTier.Large;

            if (heaterComp.Props.archetype == SaunaArchetype.Cleansed)
            {
                int steamInterval = isLarge ? CleansedSteamIntervalTicks / 2 : CleansedSteamIntervalTicks;
                if (parent.IsHashIntervalTick(steamInterval))
                {
                    ThrowRoomFillingSteam();
                }
                return;
            }

            int interval = isLarge ? BaseIntervalTicks / 2 : BaseIntervalTicks;
            if (!parent.IsHashIntervalTick(interval))
            {
                return;
            }

            ThrowThemedEffect(heaterComp.Props.archetype);
        }

        // Throws several steam puffs per call at random cells across the whole sauna
        // room (not just the heater's own tile), so the room reads as genuinely misty
        // rather than showing one puff source in a corner.
        private void ThrowRoomFillingSteam()
        {
            Room room = parent.GetRoom();
            Map map = parent.Map;
            if (room == null)
            {
                return;
            }

            for (int i = 0; i < CleansedSteamPuffsPerInterval; i++)
            {
                Vector3 loc = room.Cells.RandomElement().ToVector3Shifted();
                FleckDef fleckDef = Rand.Bool ? SaunaDefOf.Sauna_Steam1 : SaunaDefOf.Sauna_Steam2;

                FleckCreationData data = FleckMaker.GetDataStatic(loc, map, fleckDef, Rand.Range(1.2f, 2f));
                data.rotationRate = Rand.Range(-30f, 30f);
                data.velocityAngle = Rand.Range(0f, 360f);
                data.velocitySpeed = Rand.Range(0.05f, 0.15f);
                map.flecks.CreateFleck(data);
            }
        }

        private void ThrowThemedEffect(SaunaArchetype archetype)
        {
            Vector3 loc = parent.TrueCenter();
            Map map = parent.Map;

            switch (archetype)
            {
                case SaunaArchetype.Invigorated:
                    // Infusion steam-splash — same effect vanilla's Steam Geyser uses.
                    // Cleansed (steam bath) has its own, much denser room-filling effect —
                    // see ThrowRoomFillingSteam.
                    FleckMaker.ThrowAirPuffUp(loc, map);
                    break;
                case SaunaArchetype.Serenity:
                    // Meditation brazier — thin incense smoke.
                    FleckMaker.ThrowSmoke(loc, map, 0.8f);
                    break;
                case SaunaArchetype.Purified:
                    // Low-heat stone — gentle warmth shimmer, no smoke/steam.
                    FleckMaker.ThrowHeatGlow(parent.Position, map, 1.5f);
                    break;
                case SaunaArchetype.Radiant:
                    // View-oriented, glass-walled — a light glint rather than heat/steam.
                    FleckMaker.ThrowMicroSparks(loc, map);
                    break;
                case SaunaArchetype.Rejuvenated:
                    // Candlelit ambience.
                    FleckMaker.ThrowFireGlow(loc, map, 1f);
                    break;
            }
        }
    }
}
