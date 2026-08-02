using RimWorld;
using Verse;

namespace SpaMod
{
    // Grants a meditation focus strength bonus only once the heater has all 3 counted
    // decorative facilities linked (spec: "a sauna room only grants Psyfocus gain once
    // it is fully decorated"). Reuses the same capped count as the buff-amplification
    // stage, so "fully decorated" means the same thing everywhere in the mod.
    public class FocusStrengthOffset_SaunaFullyDecorated : FocusStrengthOffset
    {
        public override bool CanApply(Thing parent, Pawn user = null)
        {
            return SaunaUtility.CountLinkedFacilities(parent) >= SaunaUtility.MaxCountedFacilities;
        }

        public override float GetOffset(Thing parent, Pawn user = null)
        {
            return offset;
        }

        public override string GetExplanation(Thing parent)
        {
            return "Fully decorated sauna: " + GetOffset(parent).ToStringWithSign("0%");
        }

        public override string GetExplanationAbstract(ThingDef def = null)
        {
            return "Fully decorated sauna: " + offset.ToStringWithSign("0%");
        }
    }
}
