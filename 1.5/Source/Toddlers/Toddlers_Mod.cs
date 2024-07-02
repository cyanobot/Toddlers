using Verse;
using Verse.AI;
using UnityEngine;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Toddlers
{
    class Toddlers_Mod : Mod
    {
        public static ModContentPack mcp;

        public static bool injuredCarryLoaded;
        public static bool dressPatientsLoaded;
        public static bool DBHLoaded;
        public static bool HARLoaded;
        public static bool celsiusLoaded;

        public Toddlers_Mod(ModContentPack mcp) : base(mcp)
        {
            Toddlers_Mod.mcp = mcp; 
            GetSettings<Toddlers_Settings>();
        }

        public override string SettingsCategory()
        {
            return "Toddlers";
        }

        public override void DoSettingsWindowContents(Rect inRect) => Toddlers_Settings.DoSettingsWindowContents(inRect);
    }
}
