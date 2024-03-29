using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using Verse;
using RimWorld;

using CompDeflector;

namespace DeflectorHediff {
    [StaticConstructorOnStartup]
    public class DeflectorHediff {
        static DeflectorHediff() {
            Log.Message("[DeflectorHediff] Now active");
            var harmony = new Harmony("kaitorisenkou.DeflectorHediff");
            ManualPatch(harmony);
            //harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("[DeflectorHediff] Harmony patch complete!");
        }

        static void ManualPatch(Harmony harmony) {
            Type typeFromHandle = typeof(DeflectorHediff);
            harmony.Patch(AccessTools.Method(typeof(Thing), "TakeDamage", null, null), new HarmonyMethod(typeFromHandle, "TakeDamage_PreFix", null), null, null, null);
        }

        public static bool TakeDamage_PreFix(Thing __instance, ref DamageInfo dinfo) {
            if (NoDeflect(dinfo)) {
                return true;
            }
            var health = (__instance as Pawn)?.health;
            if (health != null) {
                var hediffs = health.hediffSet.hediffs;
                foreach (var i in hediffs) {
                    var deflector = i.TryGetComp<HediffComp_Deflector>();
                    if (deflector == null) continue;
                    var weapon = dinfo.Weapon;
                    if (deflector.Deflect(dinfo)) {
                        dinfo.SetAmount(0f);
                        return true;
                    }

                }
            }
            return true;
        }

        public static List<DamageDef> NoDeflectList = new List<DamageDef>() {
            DamageDefOf.Bomb,
            DamageDefOf.Flame
        };
        public static bool NoDeflect(DamageInfo dinfo) {
            if(dinfo.Def.isExplosive) {
                return true;
            }
            return NoDeflectList.Any(t => t == dinfo.Def);
        }
    }
}
