using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static Toddlers.Toddlers_Mod;

namespace Toddlers
{
    class LogUtil
    {
        [Conditional("DEBUG")]
        public static void DebugLog(string message) => Log.Message(message);
    }
}
