using Verse;
using UnityEngine;
using System.Collections.Generic;


namespace Toddlers
{
    public class CrawlingTweak : DefModExtension
    {
        public class Tweak
        {
            public string target;
            public Rot4 rotation;
            public Vector2 offset;
        }

        public List<Tweak> tweaks;

        public Vector2 HeadOffset(Rot4 rot)
        {
            Tweak tweak = tweaks.Find(x => x.target == "Head" && x.rotation == rot);
            if (rot == Rot4.West && tweak == null)
            {
                tweak = tweaks.Find(x => x.target == "Head" && x.rotation == Rot4.East);
                if (tweak != null) return new Vector2(-1*tweak.offset.x,tweak.offset.y);
            }
            if (tweak == null) return Vector2.zero;
            return tweak.offset;
        }

        public Vector2 BodyAddonOffset(BodyAddon bodyAddon, Rot4 rot)
        {
            string name = bodyAddon.name;

            Tweak tweak = tweaks.Find(x => x.target == name && x.rotation == rot);
            if (rot == Rot4.West && tweak == null)
            {
                tweak = tweaks.Find(x => x.target == name && x.rotation == Rot4.East);
                if (tweak != null) return new Vector2(-1 * tweak.offset.x, tweak.offset.y);
            }
            if (tweak == null) return Vector2.zero;
            return tweak.offset;
        }
    }
}
