using RimWorld;
using System.Collections.Generic;
using Verse.AI;

namespace DeflectorHediff {
    public class JobDriver_CastDeflectVerb : JobDriver {
        public override void ExposeData() {
            base.ExposeData();
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed) {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils() {
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);
            yield return Toils_Combat.CastVerb(TargetIndex.A, TargetIndex.B, canHitNonTargetPawns: false);
        }
    }
}
