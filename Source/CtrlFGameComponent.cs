using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;
using TD_Find_Lib;

namespace Ctrl_F
{
	[DefOf]
	public static class CtrlFDefOf
	{
		public static KeyBindingDef OpenCtrlF;
	}
	//GameComponent to handle keypress, contiuous refreshing list, and alerts
	class CtrlFGameComponent : GameComponent
	{
		public CtrlFWindowSearch window;
		public CtrlFGameComponent(Game g):base() { }
		
		//Ctrl-F handler
		public override void GameComponentOnGUI()
		{
			if (CtrlFDefOf.OpenCtrlF.KeyDownEvent && Event.current.control)
			{
				Log.Message($"CtrlFGameComponent: {Event.current}");
				if(Event.current.shift && window != null)
				{
					// Open what already exists, without changing the filters.
					if (!Find.WindowStack.IsOpen(window))
					{
						window.findDesc.RemakeList();
						Find.WindowStack.Add(window);
					}
					Event.current.Use();
					return;
				}

				FindDescription desc = new FindDescription(Find.CurrentMap)
				{ name = "Ctrl-F Search" };

				ListFilter filter = FilterForSelected();
				bool selectedFilter = filter != null;

				if (!selectedFilter)
					filter = ListFilterMaker.MakeFilter(ListFilterMaker.Filter_Name);

				desc.Children.Add(filter, remake: selectedFilter, focus: true);
				window = CtrlFWindowSearch.OpenWith(desc, locked: selectedFilter);
				Event.current.Use();
			}
		}


		public static ListFilter FilterForSelected()
		{
			if (Find.Selector.SingleSelectedThing is Thing thing)
			{
				ThingDef def = thing.def;
				if (Find.Selector.SelectedObjectsListForReading.All(o => o is Thing t && t.def == def))
					return FilterForThing(thing);
			}
			else if (Find.Selector.SelectedZone is Zone zone)
			{
				ListFilterZone filterZone = (ListFilterZone)ListFilterMaker.MakeFilter(ListFilterMaker.Filter_Zone);
				filterZone.sel = zone;
				return filterZone;
			}

			var defStuffs = Find.Selector.SelectedObjectsListForReading.Select(o => ((o as Thing).def, (o as Thing).Stuff)).Distinct().ToList();

			if (defStuffs.Count > 0)
			{
				ListFilterGroup filterGroup = (ListFilterGroup)ListFilterMaker.MakeFilter(ListFilterMaker.Filter_Group);
				foreach ((ThingDef def, ThingDef stuffDef) in defStuffs)
					filterGroup.Children.Add(FilterForThing(def, stuffDef));
				return filterGroup;
			}

			return null;
		}


		public static ListFilter FilterForThing(Thing thing) => FilterForThing(thing.def, thing.Stuff);

		public static ListFilter FilterForThing(ThingDef def, ThingDef stuffDef)
		{
			ListFilterThingDef filterDef = (ListFilterThingDef)ListFilterMaker.MakeFilter(ListFilterMaker.Filter_Def);
			filterDef.sel = def;
			if(stuffDef == null)
				return filterDef;

			ListFilterGroup filterGroup = (ListFilterGroup)ListFilterMaker.MakeFilter(ListFilterMaker.Filter_Group);
			filterGroup.any = false;  //All

			filterGroup.Children.Add(filterDef, remake: false);

			ListFilterStuff filterStuff = (ListFilterStuff)ListFilterMaker.MakeFilter(ListFilterMaker.Filter_Stuff);
			filterStuff.sel = stuffDef;
			filterGroup.Children.Add(filterStuff, remake: false);

			return filterGroup;
		}
	}
}
