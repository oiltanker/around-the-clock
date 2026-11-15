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
	// AutoAssignableDwelling is internal: access it as object via reflection.
	// AssignDweller is static; the dwelling is its first parameter (name-matched).
	[UsedImplicitly]
	public static bool Prefix(object dwelling, IEnumerable<Beaver> primaryBeavers, IEnumerable<Beaver> secondaryBeavers, ref bool __result)
	{
		bool result = true;
		var dwellingComponent = (Dwelling)Access.GetField(dwelling, "_dwelling");
		if (dwellingComponent.AdultSlots == 2 && dwellingComponent.FreeAdultSlots == 1)
		{
			foreach (Beaver item in primaryBeavers.Concat(secondaryBeavers))
			{
				TwoShiftsWorkingHours component = ((BaseComponent)item).GetComponent<TwoShiftsWorkingHours>();
				if (component == null)
				{
					continue;
				}
				Dweller? obj = dwellingComponent.AdultDwellers.FirstOrDefault();
				TwoShiftsWorkingHours twoShiftsWorkingHours = ((obj != null) ? ((BaseComponent)obj).GetComponent<TwoShiftsWorkingHours>() : null);
				if (twoShiftsWorkingHours == null)
				{
					continue;
				}
				Dweller component2 = ((BaseComponent)item).GetComponent<Dweller>();
				if (component.GoesToSleepTogetherWith(twoShiftsWorkingHours))
				{
					if (component2.IsLookingForBetterHome() && (bool)Access.Invoke(dwelling, "CanAssignDweller", component2))
					{
						Access.Invoke(dwelling, "AssignDweller", component2);
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
