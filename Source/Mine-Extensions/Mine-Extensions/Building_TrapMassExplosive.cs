// Assembly-CSharp, Version=1.5.9214.33606, Culture=neutral, PublicKeyToken=null
// RimWorld.Building_TrapExplosive
using RimWorld;
using UnityEngine;
using Verse;

namespace mine_extensions
{
	public class Building_TrapMassExplosive : Building_Trap
	{
        public float reqMass = 300f;
		public override void SpringSub(Pawn p)
		{
			GetComp<CompExplosive>().StartWick(p);
		}

        public override float SpringChance(Pawn p)
        {
            float num = 1f;
            if ((MassUtility.GearAndInventoryMass(p) + p.GetStatValue(StatDefOf.Mass)) < reqMass)
            {
                return 0f;
            }
            if (p.kindDef.immuneToTraps)
            {
                return 0f;
            }
            if (KnowsOfTrap(p))
            {
                if (p.IsNonMutantAnimal)
                {
                    num = 0.2f;
                    num *= def.building.trapPeacefulWildAnimalsSpringChanceFactor;
                }
                else
                {
                    num = 0.25f;
                }
            }
            num *= this.GetStatValue(StatDefOf.TrapSpringChance) * p.GetStatValue(StatDefOf.PawnTrapSpringChance);
            return Mathf.Clamp01(num);
        }
    }
}
