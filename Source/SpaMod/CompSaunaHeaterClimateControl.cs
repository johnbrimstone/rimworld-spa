using RimWorld;
using Verse;

namespace SpaMod
{
    public class CompProperties_SaunaHeaterClimateControl : CompProperties
    {
        // Degrees (C) added to the room per push interval, independent of room size.
        // Room.PushHeat divides the pushed energy by room.CellCount (confirmed via
        // decompile), so we push heatRatePerInterval * room.CellCount each time to
        // cancel that division out — a bigger room costs more power (see below) but
        // heats at the same absolute rate as a room at the heater's rated size, not a
        // slower one. 2x2 uses a higher value than 1x1 (heats faster — Resolved
        // Decision #30), same for every archetype at a given size.
        public float heatRatePerInterval = 2.5f;

        public CompProperties_SaunaHeaterClimateControl()
        {
            compClass = typeof(CompSaunaHeaterClimateControl);
        }
    }

    // Resolved Decision #31: "Room size is gated by heater power, dynamically... Going
    // over that rating increases power draw by +5% per tile over the rating." Vanilla
    // CompPowerTrader only supports one flat basePowerConsumption, so this comp
    // recalculates the sibling CompPowerTrader's live PowerOutput instead, based on the
    // heater's current room size vs. its CompSaunaHeater.Props.ratedRoomSize.
    //
    // Also owns active room heating (replacing vanilla CompHeatPusherPowered): that comp
    // reads a single fixed heatPerSecond off its own CompProperties, which can't be
    // scaled per-instance without corrupting every heater sharing that ThingDef's Props.
    // Folding heat-push into this comp keeps heating capability scaling in step with the
    // power cost increase — a heater in an oversized room draws more power *and* pushes
    // more heat to compensate, instead of paying more for the same (now-diluted-by-a-
    // bigger-room) output.
    public class CompSaunaHeaterClimateControl : ThingComp
    {
        private const int RecalcIntervalTicks = 60;
        private const float PowerIncreasePerTileOverRating = 0.05f;

        private float baseWatts = -1f;

        public CompProperties_SaunaHeaterClimateControl Props => (CompProperties_SaunaHeaterClimateControl)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            CompPowerTrader powerTrader = parent.TryGetComp<CompPowerTrader>();
            if (powerTrader != null)
            {
                // Captured once at spawn: CompProperties_Power.PowerConsumption is the
                // XML-authored base value (800/1000). We only ever overwrite the live
                // PowerOutput on the comp instance, never the shared Props.
                baseWatts = powerTrader.Props.PowerConsumption;
            }

            RecalculatePower();
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!parent.Spawned || !parent.IsHashIntervalTick(RecalcIntervalTicks))
            {
                return;
            }

            RecalculatePower();
            PushHeatIfPowered();
        }

        private void RecalculatePower()
        {
            if (baseWatts < 0f)
            {
                return;
            }

            CompPowerTrader powerTrader = parent.TryGetComp<CompPowerTrader>();
            CompSaunaHeater heaterComp = parent.TryGetComp<CompSaunaHeater>();
            if (powerTrader == null || heaterComp == null)
            {
                return;
            }

            int tilesOverRating = TilesOverRating(heaterComp);
            float multiplier = 1f + tilesOverRating * PowerIncreasePerTileOverRating;
            // CompPowerTrader.PowerOutput is negative for a net consumer.
            powerTrader.PowerOutput = -(baseWatts * multiplier);
        }

        private void PushHeatIfPowered()
        {
            CompPowerTrader powerTrader = parent.TryGetComp<CompPowerTrader>();
            if (powerTrader == null || !powerTrader.PowerOn)
            {
                return;
            }

            CompSaunaHeater heaterComp = parent.TryGetComp<CompSaunaHeater>();
            if (heaterComp == null)
            {
                return;
            }

            Room room = parent.GetRoom();
            if (room == null || room.UsesOutdoorTemperature)
            {
                return;
            }

            // Stop actively heating once the room is at/above the archetype's own gate
            // ceiling — matches vanilla CompHeatPusher's heatPushMaxTemperature, which
            // this comp otherwise replaced when heat-pushing was folded in here. Without
            // this the heater runs away indefinitely (reported: a 2x2 heater hit 200C in
            // a 26-tile room). Reusing maxGateTemperature means no new field is needed —
            // it's already the exact ceiling the buff gate checks against.
            if (room.Temperature >= heaterComp.Props.maxGateTemperature)
            {
                return;
            }

            room.PushHeat(Props.heatRatePerInterval * room.CellCount);
        }

        private int TilesOverRating(CompSaunaHeater heaterComp)
        {
            Room room = parent.GetRoom();
            int tilesOverRating = room != null ? room.CellCount - heaterComp.Props.ratedRoomSize : 0;
            return tilesOverRating > 0 ? tilesOverRating : 0;
        }
    }
}
