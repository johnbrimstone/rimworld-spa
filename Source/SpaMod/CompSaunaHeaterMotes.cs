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

            int interval = heaterComp.Props.heatTier == SaunaHeatTier.Large
                ? BaseIntervalTicks / 2
                : BaseIntervalTicks;
            if (!parent.IsHashIntervalTick(interval))
            {
                return;
            }

            ThrowThemedEffect(heaterComp.Props.archetype);
        }

        private void ThrowThemedEffect(SaunaArchetype archetype)
        {
            Vector3 loc = parent.TrueCenter();
            Map map = parent.Map;

            switch (archetype)
            {
                case SaunaArchetype.Cleansed:
                case SaunaArchetype.Invigorated:
                    // Steam heater / infusion steam-splash — same effect vanilla's
                    // Steam Geyser uses.
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
