using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Toddlers
{
    public static class HARDebug
    {
        public const bool HAR_DEBUG_LOGGING = false;

        public static void HARDebugLog(string message)
        {
            if (HAR_DEBUG_LOGGING)
            {
                Log.Message("[Toddlers][HAR Debug] " + message);
            }
        }
    }
}
