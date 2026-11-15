using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using SecondShift.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.Beavers;
using Timberborn.DwellingSystem;

namespace SecondShift.Patching;

[HarmonyPatch(typeof(DwellerHomeAssigner), "AssignDweller")]
[UsedImplicitly]
internal class DwellerHomeAssignerPatcher
{
	[UsedImplicitly]
	public static bool Prefix(AutoAssignableDwelling dwelling, IEnumerable<Beaver> primaryBeavers, IEnumerable<Beaver> secondaryBeavers, ref bool __result)
	{
		bool result = true;
		if (dwelling._dwelling.AdultSlots == 2 && dwelling._dwelling.FreeAdultSlots == 1)
		{
			foreach (Beaver item in primaryBeavers.Concat(secondaryBeavers))
			{
				TwoShiftsWorkingHours component = ((BaseComponent)item).GetComponent<TwoShiftsWorkingHours>();
				if (component == null)
				{
					continue;
				}
				Dweller? obj = dwelling._dwelling.AdultDwellers.FirstOrDefault();
				TwoShiftsWorkingHours twoShiftsWorkingHours = ((obj != null) ? ((BaseComponent)obj).GetComponent<TwoShiftsWorkingHours>() : null);
				if (twoShiftsWorkingHours == null)
				{
					continue;
				}
				Dweller component2 = ((BaseComponent)item).GetComponent<Dweller>();
				if (component.GoesToSleepTogetherWith(twoShiftsWorkingHours))
				{
					if (component2.IsLookingForBetterHome() && dwelling.CanAssignDweller(component2))
					{
						dwelling.AssignDweller(component2);
						__result = true;
						return false;
					}
				}
				else if (component2.HasHome)
				{
					__result = false;
					result = false;
				}
			}
		}
		return result;
	}
}
