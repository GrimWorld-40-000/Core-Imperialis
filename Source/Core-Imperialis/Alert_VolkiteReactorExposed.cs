using System.Collections.Generic;
using System.Linq;
using GW_Frame.Comps.ThingComps;
using RimWorld;
using Verse;

namespace Core_Imp
{
	public class Alert_VolkiteReactorExposed: Alert_Critical
	{
		private List<Thing> reactorOutdoors = new List<Thing>();

		public Alert_VolkiteReactorExposed()
		{
			defaultPriority = AlertPriority.Critical;
		}

		private List<Thing> ReactorOutdoors
		{
			get
			{
				reactorOutdoors.Clear();
				foreach (var thing in Find.Maps.Where(map => map.mapPawns.AnyColonistSpawned).SelectMany(map => map.listerThings.GetAllThings(thing => thing.def == CoreImperialisDefOf.GW_VolkiteReactor)))
				{
					if (!thing.TryGetComp(out CompMustBeIndoors indoors)) continue;
					if (indoors.ShouldAlertNow) reactorOutdoors.Add(thing);
				}
				return reactorOutdoors;
			}
		}

		public override AlertReport GetReport() => AlertReport.CulpritsAre(ReactorOutdoors);

		public override string GetLabel()
		{
			return "GW_ReactorOutdoorsLabel".Translate();
		}

		public override TaggedString GetExplanation()
		{
			return "GW_ReactorOutdoorsDesc".Translate();
		}
	}
}