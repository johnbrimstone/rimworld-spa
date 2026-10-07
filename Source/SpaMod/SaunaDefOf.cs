using RimWorld;
using Verse;

namespace SpaMod
{
    [DefOf]
    public static class SaunaDefOf
    {
        public static RoomRoleDef Sauna;

        // Split into 3 buckets by JoyKindDef (Phase 4) so RimWorld's variety-seeking
        // joy AI treats different archetypes as different recreation types — see
        // SaunaUtility.JobDefForArchetype for which bucket each archetype uses.
        public static JobDef Sauna_UseHeater_Hydrotherapy;

        public static JobDef Sauna_UseHeater_Meditative;

        public static JobDef Sauna_UseHeater_BathRelaxing;

        public static HediffDef Sauna_Serenity;

        public static HediffDef Sauna_Detoxified;

        public static HediffDef Sauna_Purified;

        public static HediffDef Sauna_Radiant;

        public static HediffDef Sauna_Rejuvenated;

        public static HediffDef Sauna_Invigorated;

        public static HediffDef Sauna_Cleansed;

        // Cross-cutting / exclusive buffs (Phase 2) — granted alongside the archetype
        // buff above, not instead of it.
        public static HediffDef Sauna_HeatNumbed;

        public static HediffDef Sauna_Vitality;

        public static HediffDef Sauna_Clarity;

        public static HediffDef Sauna_SaunaBonding;

        public static HediffDef Sauna_FreshAir;

        public static HediffDef Sauna_EasedJoints;

        public static HediffDef Sauna_DeeplyRelaxed;

        // Vanilla old-age hediffs, used to gate Eased Joints eligibility.
        public static HediffDef BadBack;

        public static HediffDef Frail;

        // Cold Plunge Tub (Phase 3) — standalone building, not linked to any heater.
        public static JobDef Sauna_UseColdPlungeTub;

        public static HediffDef Sauna_ColdPlunge;

        // Custom steam flecks (Common/Textures/steam1.png/steam2.png) for Cleansed's
        // room-filling steam effect — see CompSaunaHeaterMotes.
        public static FleckDef Sauna_Steam1;

        public static FleckDef Sauna_Steam2;

        static SaunaDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SaunaDefOf));
        }
    }
}
