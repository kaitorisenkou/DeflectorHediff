using CompDeflector;
using HarmonyLib;
using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace DeflectorHediff {
    public class HediffComp_Deflector : HediffComp {
        public HediffCompProperties_Deflector Props {
            get {
                return (HediffCompProperties_Deflector)this.props;
            }
        }
        public bool Deflect(DamageInfo dinfo) {
            var weapon = dinfo.Weapon;
            if (weapon == null)
                return false;

            if (DeflectSuccess(dinfo)) {
                this.GiveDeflectJob(dinfo);
                DeflectLearn();
                return true;
            }
            this.lastShotReflected = false;
            return false;
        }

        public bool DeflectSuccess(DamageInfo dinfo) {
            if (dinfo.Weapon.IsMeleeWeapon && !Props.canDeflectMelee) {
                return false;
            }
            float num = Props.baseDeflectChance;
            if (Props.useSkillInCalc && Props.deflectSkill != null) {
                num += Pawn.skills.GetSkill(Props.deflectSkill).Level * Props.deflectRatePerSkillPoint;
            }
            if (Props.useManipulationInCalc) {
                num *= Pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation);
            }
            float ransu = UnityEngine.Random.Range(0f, 1f);
            //MoteMaker.ThrowText(Pawn.DrawPos, Pawn.Map, "rand:" + ransu + "\n num:" + num, 6f);
            return UnityEngine.Random.Range(0f, 1f) < num;
        }
        public int ReflectSuccess() {
            if (!Props.canReflect)
                return -3;
            float num = UnityEngine.Random.Range(0, 100) + Pawn.skills.GetSkill(Props.reflectSkill).Level * Props.reflectRatePerSkillPoint;
            if (num > 90) return 2;
            if (num > 80) return 1;
            if (num > 30) return -1;
            return -2;
        }

        public void ResolveDeflectVerb() {
            CopyAndReturnNewVerb(null);
        }
        public void GiveDeflectJob(DamageInfo dinfo) {
            Pawn enemy = dinfo.Instigator as Pawn;
            if (enemy == null)
                return;
            this.ResolveDeflectVerb();
            int successRate = ReflectSuccess();
            if (successRate < -2)
                return;
            if (successRate > -2) 
                ReflectLearn();
            lastAccuracyRoll = successRate;
            Job job = JobMaker.MakeJob(CompDeflectorDefOf.CastDeflectVerb);
            job.playerForced = true;
            job.locomotionUrgency = LocomotionUrgency.Sprint;
            Pawn_EquipmentTracker equipment = enemy.equipment;
            Verb verb;
            if (equipment == null) {
                verb = null;
            } else {
                verb = equipment.PrimaryEq?.PrimaryVerb;
            }
            if (verb == null)
                return;
            if (Pawn == null || Pawn.Dead)
                return;
            Verb_Deflected newVerb = (Verb_Deflected)this.CopyAndReturnNewVerb(verb);
            Verb_Deflected verb_Deflected = (Verb_Deflected)this.ReflectionHandler(newVerb, lastAccuracyRoll);
            verb_Deflected.lastShotReflected = this.lastShotReflected;
            verb_Deflected.verbTracker = Pawn.VerbTracker;
            enemy = this.ResolveDeflectionTarget(enemy);
            job.targetA = enemy;
            job.verbToUse = verb_Deflected;
            job.killIncappedTarget = enemy.Downed;
            Pawn.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
        }
        public Verb CopyAndReturnNewVerb(Verb newVerb = null) {
            if (newVerb != null) {
                this.deflectVerb = (Verb_Deflected)Activator.CreateInstance(typeof(Verb_Deflected));
                this.deflectVerb.caster = Pawn;
                VerbProperties verbProps = new VerbProperties {
                    hasStandardCommand = newVerb.verbProps.hasStandardCommand,
                    defaultProjectile = newVerb.verbProps.defaultProjectile,
                    range = newVerb.verbProps.range,
                    muzzleFlashScale = newVerb.verbProps.muzzleFlashScale,
                    warmupTime = 0f,
                    defaultCooldownTime = 0f,
                    soundCast = this.Props.deflectSound
                };
                this.deflectVerb.verbProps = verbProps;
            } else {
                if (this.deflectVerb != null) {
                    return this.deflectVerb;
                }
                this.deflectVerb = (Verb_Deflected)Activator.CreateInstance(typeof(Verb_Deflected));
                this.deflectVerb.caster = Pawn;
                this.deflectVerb.verbProps = this.Props.DeflectVerb;
            }
            return this.deflectVerb;
        }

        public virtual Pawn ResolveDeflectionTarget(Pawn defaultTarget = null) {
            Pawn near = (Pawn)GenClosest.ClosestThingReachable(Pawn.Position, Pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.InteractionCell, TraverseParms.For(Pawn, Danger.Deadly, TraverseMode.ByPawn, false, false, false), 9999f, (Thing t) => t is Pawn && t != Pawn, null, 0, -1, false, RegionType.Set_Passable, false);
            if (near == null) {
                return defaultTarget;
            }
            return near;
        }
        public virtual Verb ReflectionHandler(Verb newVerb, int successRate) {
            if (this.Props.canReflect) {
                VerbProperties verbProperties = new VerbProperties {
                    hasStandardCommand = newVerb.verbProps.hasStandardCommand,
                    defaultProjectile = newVerb.verbProps.defaultProjectile,
                    range = newVerb.verbProps.range,
                    muzzleFlashScale = newVerb.verbProps.muzzleFlashScale,
                    warmupTime = 0f,
                    defaultCooldownTime = 0f,
                    soundCast = this.Props.deflectSound
                };
                switch (successRate) {
                    case -2:
                        MoteMaker.ThrowText(Pawn.DrawPos, Pawn.Map, "SWSaber_TextMote_CriticalFailure".Translate(), 6f);
                        verbProperties.accuracyLong = 999f;
                        verbProperties.accuracyMedium = 999f;
                        verbProperties.accuracyShort = 999f;
                        this.lastShotReflected = true;
                        break;

                    case -1:
                        //verbPropertiesForcedMissRadius.Invoke(verbProperties) = 50f;
                        verbProperties.accuracyLong = 0f;
                        verbProperties.accuracyMedium = 0f;
                        verbProperties.accuracyShort = 0f;
                        verbProperties.accuracyTouch = 0f;
                        this.lastShotReflected = false;
                        break;
                    case 1:
                        verbProperties.accuracyLong = 999f;
                        verbProperties.accuracyMedium = 999f;
                        verbProperties.accuracyShort = 999f;
                        this.lastShotReflected = true;
                        break;
                    case 2:
                        MoteMaker.ThrowText(Pawn.DrawPos, Pawn.Map, "SWSaber_TextMote_CriticalSuccess".Translate(), 6f);
                        verbProperties.accuracyLong = 999f;
                        verbProperties.accuracyMedium = 999f;
                        verbProperties.accuracyShort = 999f;
                        this.lastShotReflected = true;
                        break;

                }
                newVerb.verbProps = verbProperties;
                return newVerb;
            }
            return newVerb;
        }

        public void DeflectLearn() {
            var skill = Props.deflectSkill;
            if (skill == null) {
                return;
            }
            Pawn.skills.Learn(skill, Props.deflectSkillLearnRate);
        }
        public void ReflectLearn() {
            var skill = Props.reflectSkill;
            if (skill == null) {
                return;
            }
            Pawn.skills.Learn(skill, Props.reflectSkillLearnRate);
        }

        public bool lastShotReflected;
        public Verb_Deflected deflectVerb;
        int lastAccuracyRoll;
        private static readonly AccessTools.FieldRef<VerbProperties, float> verbPropertiesForcedMissRadius = AccessTools.FieldRefAccess<VerbProperties, float>("forcedMissRadius");
    }

    public class HediffCompProperties_Deflector : HediffCompProperties {
        public HediffCompProperties_Deflector() {
            this.compClass = typeof(HediffComp_Deflector);
        }

        public float baseDeflectChance = 0.3f;
        public bool canDeflectMelee = false;
        public float deflectRatePerSkillPoint = 0.015f;
        public SkillDef deflectSkill;
        public float deflectSkillLearnRate = 250f;
        public SoundDef deflectSound;
        public VerbProperties DeflectVerb;

        public bool canReflect = false;
        public float reflectRatePerSkillPoint = 3f;
        public SkillDef reflectSkill;
        public float reflectSkillLearnRate = 500f;

        public bool useManipulationInCalc = false;
        public bool useSkillInCalc = false;
    }
}
