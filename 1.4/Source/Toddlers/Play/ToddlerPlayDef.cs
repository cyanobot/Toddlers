using RimWorld;
using Verse;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toddlers
{
    class ToddlerPlayDef : Def
    {
        public JobDef jobDef;
        private ToddlerPlayGiver workerInt;
        public Type workerClass = typeof(ToddlerPlayGiver);
        public float selectionWeight = 1f;

        public ToddlerPlayGiver Worker
        {
            get
            {
                if (this.workerInt == null)
                {
                    this.workerInt = (ToddlerPlayGiver)Activator.CreateInstance(this.workerClass);
                    this.workerInt.def = this;
                }
                return this.workerInt;
            }
        }
    }
}
