using HarmonyLib;
using Verse;

namespace SpaMod
{
    // Hides apparel rendering (body clothes + headgear) for pawns currently doing the
    // sauna job — appearance only, WornApparel is never modified. There's no data-driven
    // hook for this in vanilla (the closest, Pawn.Swimming, also forces the water-specific
    // NoBody flag and feeds into unrelated water/pathing checks, so reusing it would be
    // riskier than this narrow, isolated patch). __1 is the PawnDrawParms argument by
    // position — used instead of the parameter name because the two target methods name
    // it differently ("parms" vs "n"'s sibling "parms" is fine, but the node param itself
    // is named "node" on one override and "n" on the other).
    [HarmonyPatch(typeof(PawnRenderNodeWorker_Apparel_Body), nameof(PawnRenderNodeWorker_Apparel_Body.CanDrawNow))]
    public static class Patch_HideBodyApparel_InSauna
    {
        public static void Postfix(PawnDrawParms __1, ref bool __result)
        {
            if (__result && SaunaUtility.IsUsingSauna(__1.pawn))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(PawnRenderNodeWorker_Apparel_Head), nameof(PawnRenderNodeWorker_Apparel_Head.CanDrawNow))]
    public static class Patch_HideHeadApparel_InSauna
    {
        public static void Postfix(PawnDrawParms __1, ref bool __result)
        {
            if (__result && SaunaUtility.IsUsingSauna(__1.pawn))
            {
                __result = false;
            }
        }
    }
}
