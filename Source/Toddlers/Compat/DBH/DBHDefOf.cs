using RimWorld;
using Verse;

namespace Toddlers
{
    [DefOf]
    public static class DBHDefOf
    {
        //DBH defs

        [MayRequireAnyOf("Dubwise.DubsBadHygiene,Dubwise.DubsBadHygiene.Lite")]
        public static NeedDef Hygiene;

        [MayRequireAnyOf("Dubwise.DubsBadHygiene,Dubwise.DubsBadHygiene.Lite")]
        public static NeedDef DBHThirst;

        [MayRequireAnyOf("Dubwise.DubsBadHygiene,Dubwise.DubsBadHygiene.Lite")]
        public static NeedDef Bladder;


        [MayRequireAnyOf("Dubwise.DubsBadHygiene,Dubwise.DubsBadHygiene.Lite")]
        public static WorkGiverDef washPatient;

        [MayRequireAnyOf("Dubwise.DubsBadHygiene,Dubwise.DubsBadHygiene.Lite")]
        public static HediffDef Washing;

        [MayRequireAnyOf("Dubwise.DubsBadHygiene,Dubwise.DubsBadHygiene.Lite")]
        public static SoundDef shower_Ambience;

        [MayRequireAnyOf("Dubwise.DubsBadHygiene,Dubwise.DubsBadHygiene.Lite")]
        public static EffecterDef WashingEffect;

        //new defs

        [MayRequireAnyOf("Dubwise.DubsBadHygiene,Dubwise.DubsBadHygiene.Lite")]
        public static JobDef CYB_WashBaby;

        [MayRequireAnyOf("Dubwise.DubsBadHygiene,Dubwise.DubsBadHygiene.Lite")]
        public static JobDef CYB_BatheToddler;

        [MayRequireAnyOf("Dubwise.DubsBadHygiene,Dubwise.DubsBadHygiene.Lite")]
        public static JobDef ToddlerBeWashed;

        static DBHDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DBHDefOf));
        }
    }
}
