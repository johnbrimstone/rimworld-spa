using Verse;

namespace SpaMod
{
    // Per the design spec: a room qualifies as a Sauna if it contains at least one sauna
    // heater and at least one sittable furniture piece. Facility count and room
    // temperature don't affect this scoring — they only gate/scale the buff itself, in
    // JobDriver_UseSauna.ApplyRoomBuff (see SaunaUtility.GetIntensityMultiplier and
    // CompSaunaHeater.IsRoomTemperatureInRange).
    public class RoomRoleWorker_Sauna : RoomRoleWorker
    {
        private const float Score = 10000f;

        public override float GetScore(Room room)
        {
            if (room == null || room.TouchesMapEdge || room.IsDoorway)
            {
                return 0f;
            }

            bool hasHeater = false;
            bool hasSeat = false;

            foreach (Thing thing in room.ContainedAndAdjacentThings)
            {
                if (!hasHeater && thing.TryGetComp<CompSaunaHeater>() != null)
                {
                    hasHeater = true;
                }
                else if (!hasSeat && (thing.def.building?.isSittable ?? false))
                {
                    hasSeat = true;
                }

                if (hasHeater && hasSeat)
                {
                    return Score;
                }
            }

            return 0f;
        }
    }
}
