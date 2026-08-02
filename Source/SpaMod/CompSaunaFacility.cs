using Verse;

namespace SpaMod
{
    public class CompProperties_SaunaFacility : CompProperties
    {
        // Points feeding the room's intensity formula (5% per point, top 3 highest-value
        // linked facilities count — see SaunaUtility.GetIntensityMultiplier). Separate
        // from vanilla CompProperties_Facility, which has no such field.
        public int facilityBonus = 1;

        public CompProperties_SaunaFacility()
        {
            compClass = typeof(CompSaunaFacility);
        }
    }

    public class CompSaunaFacility : ThingComp
    {
        public CompProperties_SaunaFacility Props => (CompProperties_SaunaFacility)props;
    }
}
