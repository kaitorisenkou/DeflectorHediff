using RimWorld;
using Verse;

namespace DeflectorHediff {
    [DefOf]
    public static class DeflectorDefOf {
        public static JobDef CastDeflectVerb;
        //public static StatDef MeleeWeapon_DeflectionChance;

        static DeflectorDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(DeflectorDefOf));
    }
}
