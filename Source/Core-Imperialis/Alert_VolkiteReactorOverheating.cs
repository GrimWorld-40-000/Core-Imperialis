using System.Collections.Generic;
using System.Linq;
using GW_Frame.Comps.ThingComps;
using RimWorld;
using Verse;

namespace Core_Imp
{
	public class Alert_VolkiteReactorOverheating: Alert_Critical
	{
		private List<Thing> reactorOverheatingResult = new List<Thing>();

		public Alert_VolkiteReactorOverheating()
		{
			
			defaultPriority = AlertPriority.Critical;
		}

		private List<Thing> ReactorOverheating
		{
			get
			{
				reactorOverheatingResult.Clear();
				foreach (var thing in Find.Maps.Where(map => map.mapPawns.AnyColonistSpawned).SelectMany(map => map.listerThings.GetAllThings(thing => thing.def == CoreImperialisDefOf.GW_VolkiteReactor)))
				{
					if (!thing.TryGetComp(out CompDamagedByTemperature temperature)) continue;
					if (temperature.IsTooHot) reactorOverheatingResult.Add(thing);
				}
				return reactorOverheatingResult;
			}
		}

		public override AlertReport GetReport() => AlertReport.CulpritsAre(ReactorOverheating);

		public override string GetLabel()
		{
			return "GW_ReactorHotLabel".Translate();
		}

		public override TaggedString GetExplanation()
		{
			return "GW_ReactorHotDesc".Translate();
		}
	}
}