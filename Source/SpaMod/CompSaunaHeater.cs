using Verse;

namespace SpaMod
{
    public enum SaunaArchetype
    {
        Detoxified,
        Purified,
        Serenity,
        Radiant,
        Rejuvenated,
        Invigorated,
        Cleansed,
    }

    // 1x1 vs 2x2 heater. Per Resolved Decision #27/#30/#31, size no longer scales buff
    // intensity or a direct "capacity" stat — it only drives mote throw frequency
    // (CompSaunaHeaterMotes), room-size power rating (CompSaunaHeaterPowerScaling), and
    // active heating rate (CompProperties_HeatPusher.heatPerSecond).
    public enum SaunaHeatTier
    {
        Small,
        Large,
    }

    public class CompProperties_SaunaHeater : CompProperties
    {
        public SaunaArchetype archetype = SaunaArchetype.Serenity;
        public SaunaHeatTier heatTier = SaunaHeatTier.Small;

        // Resolved Decision #31: room size (in tiles) this heater's base power draw is
        // rated for — 12 for 1x1, 25 for 2x2. Read by CompSaunaHeaterPowerScaling.
        public int ratedRoomSize = 12;

        // "Room temperature acts as a simple gate... the room must be within the heater
        // variant's expected temperature range for the buff to apply at all" (Room &
        // Sauna Definition). No numeric range is given in the spec for this — it's a
        // Claude Code implementation detail, chosen per archetype to match its real-world
        // theme (e.g. a dry Finnish-style heater runs hotter than a meditation brazier).
        // Read by JobDriver_UseSauna's temperature gate; maxGateTemperature also feeds
        // the heater's CompProperties_HeatPusher.heatPushMaxTemperature so it naturally
        // stops actively heating once the room is within range.
        public float minGateTemperature = 30f;
        public float maxGateTemperature = 45f;

        public CompProperties_SaunaHeater()
        {
            compClass = typeof(CompSaunaHeater);
        }
    }

    // Marker comp: identifies a building as a sauna heater and carries the archetype/heatTier
    // that the room role scoring and JobDriver read. New heater variants are pure XML —
    // no new C# comp needed per variant.
    public class CompSaunaHeater : ThingComp
    {
        public CompProperties_SaunaHeater Props => (CompProperties_SaunaHeater)props;

        // Resolved Decision #31 / "Room & Sauna Definition" temperature gate: is this
        // room currently within the heater variant's expected range? Room may be null
        // (not spawned / not enclosed yet), which reads as "not in range".
        public bool IsRoomTemperatureInRange(Room room)
        {
            if (room == null)
            {
                return false;
            }

            float temperature = room.Temperature;
            return temperature >= Props.minGateTemperature && temperature <= Props.maxGateTemperature;
        }

        public override string CompInspectStringExtra()
        {
            int max = SaunaUtility.MaxCountedFacilities;
            int count = SaunaUtility.CountLinkedFacilities(parent);
            string suffix = count >= max ? " (fully decorated)" : "";
            string facilityLine = "Linked sauna facilities: " + count + " / " + max + suffix;

            Room room = parent.GetRoom();
            string tempLine = "Sauna temperature range: " + Props.minGateTemperature.ToStringTemperature("F0")
                + " ~ " + Props.maxGateTemperature.ToStringTemperature("F0")
                + (IsRoomTemperatureInRange(room) ? " (in range)" : " (out of range)");

            // Explains why a room never reaches this heater's range when another, cooler
            // archetype's heater shares it (see SaunaUtility.GetRoomHeatingCeiling).
            float? ceiling = SaunaUtility.GetRoomHeatingCeiling(room);
            if (ceiling != null && ceiling.Value < Props.maxGateTemperature)
            {
                tempLine += "\nRoom heating capped at " + ceiling.Value.ToStringTemperature("F0")
                    + " by another active sauna heater";
            }

            return facilityLine + "\n" + tempLine;
        }
    }
}
