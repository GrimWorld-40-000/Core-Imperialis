using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse.Sound;
using Verse;
using System.Reflection;
using RimWorld.QuestGen;
using static System.Collections.Specialized.BitVector32;
using static RimWorld.IdeoFoundation_Deity;
using static UnityEngine.GraphicsBuffer;
using HarmonyLib;
using System.Runtime;
using UnityEngine.Diagnostics;
using RimWorld.Planet;
using Verse.Noise;

namespace megaraid
{

    [StaticConstructorOnStartup]
    public class BombardmentRaid : GameCondition
    {

        public override bool AllowEnjoyableOutsideNow(Map map) => false;

        public override void Init()
        {
            Find.LetterStack.ReceiveLetter(LetterMaker.MakeLetter("Bombardment!", "An intense bombardment is occurring targetting your colony", LetterDefOf.NegativeEvent, new LookTargets(SingleMap.Center, SingleMap)));
            if (SingleMap.Area <= 40000)
            {
                bombIntervalTicks = 30;
            } else if (SingleMap.Area <= 50625)
            {
                bombIntervalTicks = 25;
            } else if (SingleMap.Area <= 62500)
            {
                bombIntervalTicks = 20;
            } else if (SingleMap.Area <= 75625)
            {
                bombIntervalTicks = 15;
            } else if (SingleMap.Area <= 90000)
            {
                bombIntervalTicks = 10;
            } else
            {
                bombIntervalTicks = 5;
            }

            if (SingleMap.TileInfo.hilliness == Hilliness.Mountainous)
            {
                noLaserChance = 40;
            } else if (SingleMap.TileInfo.hilliness == Hilliness.LargeHills)
            {
                noLaserChance = 50;
            }
            // Verse.Log.Message("Bombardment: bombInterval(" + bombIntervalTicks + ") noLaser(" + noLaserChance +")" );
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (!nextExplosionCell.IsValid)
                {
                    GetNextExplosionCell();
                }
                if (projectiles == null)
                {
                    projectiles = new List<Bombardment.BombardmentProjectile>();
                }
            }
        }
        private System.Random rand = new System.Random();

        int spreadTicks = 10;
        int bombIntervalTicks = 15;
        private int ticksToNextEffect;
        private IntVec3 nextExplosionCell = new IntVec3();
        private List<Bombardment.BombardmentProjectile> projectiles = new List<Bombardment.BombardmentProjectile>();

        int laserBeamTicks = 50;
        int laserMoveCurr = 0;
        private IntVec3 oldCell;

        int noLaserChance = 60;

        public override void GameConditionTick()
        {
            Map map = SingleMap;

            // Explosion handle
            if (!nextExplosionCell.IsValid)
            {
                ticksToNextEffect = bombIntervalTicks;
                GetNextExplosionCell();
            }
            ticksToNextEffect--;
            if (ticksToNextEffect <= 0 && base.TicksLeft >= bombIntervalTicks)
            {
                // Trigger explosion
                SoundDefOf.Bombardment_PreImpact.PlayOneShot(new TargetInfo(nextExplosionCell, map, false));
                if (Rand.Range(0, noLaserChance) == 0)
                {
                    PowerBeam obj = (PowerBeam)GenSpawn.Spawn(ThingDefOf.PowerBeam, nextExplosionCell, map);
                    obj.duration = rand.Next(500, 2000);
                    obj.StartStrike();
                }

                // Add new explosion
                projectiles.Add(new Bombardment.BombardmentProjectile(100, nextExplosionCell));
                ticksToNextEffect = bombIntervalTicks + Rand.Range(-spreadTicks, spreadTicks);
                GetNextExplosionCell();
            }

            // Laser Tornado on mountain bases
            if (false)
            {
                // MoveLaserTornado(map);
            }

            // Trigger projectile explosions at the end of countdown
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                projectiles[i].Tick();
                Draw();
                if (projectiles[i].LifeTime <= 0)
                {
                    IntVec3 targetCell = projectiles[i].targetCell;
                    DamageDef bomb = Rand.Range(1, 10) > 2 ? DamageDefOf.Bomb : DamageDefOf.Flame;
                    GenExplosion.DoExplosion(targetCell, map, Rand.Range(3f, 6f), bomb, null);
                    projectiles.RemoveAt(i);
                }
            }
        }

        private void Draw()
        {
            if (projectiles.NullOrEmpty())
            {
                return;
            }
            for (int i = 0; i < projectiles.Count; i++)
            {
                projectiles[i].Draw(ProjectileMaterial);
            }
        }


        private void GetNextExplosionCell()
        {
            List<IntVec3> cells = SingleMap.AllCells.ToList();
            System.Random r = new System.Random();
            nextExplosionCell = cells[r.Next(cells.Count)];
        }

        private void MoveLaserTornado(Map map)
        {
            laserMoveCurr--;
            if (laserMoveCurr <= 0)
            {

                // On startup?
                if (oldCell == null)
                {
                    oldCell = nextExplosionCell;
                    GetNextExplosionCell();
                }


                // Move to a random position
                oldCell.x += Rand.Range(-10, 10);
                oldCell.y += Rand.Range(-10, 10);
                oldCell.z += Rand.Range(-10, 10);
                if (!oldCell.InBounds(map) || Rand.Range(0, 300) == 0)
                {
                    // Temporairly despawn if out of bounds
                    oldCell = nextExplosionCell;
                    GetNextExplosionCell();
                    laserMoveCurr = laserBeamTicks + 1000;
                    return;
                }

                // Spawn beam
                PowerBeam obj = (PowerBeam)GenSpawn.Spawn(ThingDefOf.PowerBeam, oldCell, map);
                obj.duration = 60;
                obj.StartStrike();
                laserMoveCurr = laserBeamTicks;
            }
        }

        private static readonly Material ProjectileMaterial = MaterialPool.MatFrom("Things/Projectile/Bullet_Big", ShaderDatabase.Transparent, Color.white);
    }




    public class RaidTrigger : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            executeRaid(parms);
            return true;
        }

        public static void executeRaid(IncidentParms parms)
        {
            if (parms.points <= 5000)
            {
                parms.points = 5000;
            }

            System.Random rand = new System.Random();

            // Bombardment
            GameConditionDef bombdef = new GameConditionDef();
            bombdef.conditionClass = typeof(megaraid.BombardmentRaid);
            bombdef.endMessage = "The bombardment is ending";
            var bombDur = rand.Next(6000, 10000);
            var bombCond = GameConditionMaker.MakeCondition(bombdef, bombDur);
            parms.target.GameConditionManager.RegisterCondition(bombCond);


            // Possible Extra Conditions
            if (rand.NextDouble() < 0.3)
            {
                // Drone
                // var cond = GameConditionMaker.MakeCondition(GameConditionDefOf.PsychicDrone, rand.Next(15000, 25000));
                // parms.target.GameConditionManager.RegisterCondition(cond);

            }

            if (rand.NextDouble() < 0.5)
            {
                // Solar Flare
                var cond = GameConditionMaker.MakeCondition(GameConditionDefOf.SolarFlare, rand.Next(10000, 30000));
                parms.target.GameConditionManager.RegisterCondition(cond);

            }


            if (parms.faction == null)
            {
                parms.faction = Find.FactionManager.RandomEnemyFaction(minTechLevel: TechLevel.Industrial);
            }

            // Trigger the first wave
            IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(incCat: IncidentCategoryDefOf.ThreatBig, target: parms.target);
            incidentParms.forced = true;
            incidentParms.faction = parms.faction;
            incidentParms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            incidentParms.target = parms.target;
            incidentParms.raidArrivalMode = PawnsArrivalModeDefOf.RandomDrop;
            incidentParms.raidNeverFleeIndividual = true;
            incidentParms.points = (float) (parms.points);
            var raidTime = rand.Next(1000, 2000);
            Find.Storyteller.incidentQueue.Add(IncidentDefOf.RaidEnemy, Find.TickManager.TicksGame + raidTime, incidentParms, 240000);


            // Trigger the second wave
            IncidentParms secondWave = StorytellerUtility.DefaultParmsNow(incCat: IncidentCategoryDefOf.ThreatBig, target: parms.target);
            secondWave.forced = true;
            secondWave.faction = parms.faction;
            secondWave.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            secondWave.target = parms.target;
            secondWave.points = (float) (parms.points);
            secondWave.raidArrivalMode = PawnsArrivalModeDefOf.RandomDrop;
            incidentParms.raidNeverFleeIndividual = true;
            Find.Storyteller.incidentQueue.Add(IncidentDefOf.RaidEnemy, Find.TickManager.TicksGame + raidTime, incidentParms, 240000);
        }
    }


    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var inst = new Harmony("rimworld.megaraid.dialog");
            inst.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static FieldInfo MapFieldInfo;
    }


    internal class FactionDialogMaker_Patch
    {
        [HarmonyPatch(typeof(FactionDialogMaker), "FactionDialogFor")]
        public class FactionDialogFor
        {
            private static Texture2D insultTex;
            [HarmonyPostfix]
            public static void Listener(ref DiaNode __result, Pawn negotiator, Faction faction)
            {
                DiaOption opt = new DiaOption("Bombardment Raid");
                opt.action = delegate
                {
                    IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(incCat: IncidentCategoryDefOf.ThreatBig, target: negotiator.Map);
                    incidentParms.target = negotiator.Map;
                    incidentParms.faction = faction;
                    RaidTrigger.executeRaid(incidentParms);
                };


                if (faction.AllyOrNeutralTo(negotiator.Faction) || faction.def.techLevel < TechLevel.Industrial)
                {
                    opt.disabled = true;
                }

                opt.resolveTree = true;
                __result.options.Insert(__result.options.Count - 1, opt);

            }
        }
    }

}
