using Verse;

namespace SpaMod
{
    // Per the design spec: a room qualifies as a Sauna if it contains at least one sauna
    // heater and at least one sittable furniture piece. Facility count and room
    // temperature don't affect this scoring — they only gate/scale the buff itself, in
    // JobDriver_UseSauna.ApplyRoomBuff (see SaunaUtility.GetIntensityMultiplier and
    // CompSaunaHeater.IsRoomTemperatureInRange).
    //
    // One heater type per room: a room containing heaters of more than one archetype
    // never scores, so no session can start or pay out there. Mixing is blocked at
    // placement (PlaceWorker_SaunaNoMixedHeaters) and flagged visually if it happens
    // anyway through merged rooms (MapComponent_SaunaMixedHeaters, Alert_SaunaMixedHeaters).
    public class RoomRoleWorker_Sauna : RoomRoleWorker
    {
        private const float Score = 10000f;

        public override float GetScore(Room room)
        {
            if (!SaunaUtility.IsEnclosedRoom(room))
            {
                return 0f;
            }

            SaunaArchetype? heaterArchetype = null;
            bool hasSeat = false;

            // No early exit once a heater and seat are found: every heater has to be seen
            // to rule out a mixed room.
            foreach (Thing thing in room.ContainedAndAdjacentThings)
            {
                SaunaArchetype? archetype = SaunaUtility.HeaterArchetypeOf(thing, includePlanned: false);
                if (archetype != null)
                {
                    if (heaterArchetype != null && heaterArchetype.Value != archetype.Value)
                    {
                        return 0f;
                    }
                    heaterArchetype = archetype;
                }
                else if (!hasSeat && (thing.def.building?.isSittable ?? false))
                {
                    hasSeat = true;
                }
            }

            return heaterArchetype != null && hasSeat ? Score : 0f;
        }
    }
}
