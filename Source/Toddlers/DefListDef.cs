using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Toddlers
{
    public class DefListDef : Def
    {
        public List<ThingDef> whitelist = new List<ThingDef>();
        public List<ThingDef> blacklist = new List<ThingDef>();
    }
}
