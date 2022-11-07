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
		public static KeyBindingDef OpenFindTab;
		public static MainButtonDef TD_List;
	}
	//GameComponent to handle keypress, contiuous refreshing list, and alerts
	class ListEverythingGameComp : GameComponent
	{
		public ListEverythingGameComp(Game g):base() { }
		
		//Ctrl-F handler
		public override void GameComponentOnGUI()
		{
			if (CtrlFDefOf.OpenFindTab.IsDownEvent && Event.current.control)
			{
				FindDescription desc = new FindDescription(Find.CurrentMap);

				ListFilter filter = FilterForSelected();
				bool selectedFilter = filter != null;

				if (!selectedFilter)
					filter = ListFilterMaker.MakeFilter(ListFilterMaker.Filter_Name);

				desc.Children.Add(filter, focus: true);
				MainTabWindow_List.OpenWith(desc, locked: selectedFilter, remake: selectedFilter);
			}
		}


		public static ListFilter FilterForSelected()
		{
			if (Find.Selector.SingleSelectedThing is Thing thing)
			{
				ThingDef def = thing.def;
				if (Find.Selector.SelectedObjectsListForReading.All(o => o is Thing t && t.def == def))
				{
					ListFilterThingDef filterDef = (ListFilterThingDef)ListFilterMaker.MakeFilter(ListFilterMaker.Filter_Def);
					filterDef.sel = thing.def;
					return filterDef;
				}
			}
			else if (Find.Selector.SelectedZone is Zone zone)
			{
				ListFilterZone filterZone = (ListFilterZone)ListFilterMaker.MakeFilter(ListFilterMaker.Filter_Zone);
				filterZone.sel = zone;
				return filterZone;
			}

			var defs = Find.Selector.SelectedObjectsListForReading.Select(o => (o as Thing).def).ToHashSet();

			if (defs.Count > 0)
			{
				ListFilterGroup groupFilter = (ListFilterGroup)ListFilterMaker.MakeFilter(ListFilterMaker.Filter_Group);
				foreach (ThingDef def in defs)
				{
					ListFilterThingDef defFilter = (ListFilterThingDef)ListFilterMaker.MakeFilter(ListFilterMaker.Filter_Def);
					defFilter.sel = def;
					groupFilter.Children.Add(defFilter);
				}
				return groupFilter;
			}

			return null;
		}

		//continuousRefresh
		public bool continuousRefresh = false;
		public override void GameComponentTick()
		{
			if (Find.TickManager.TicksGame % 60 != 0) return; //every second I guess?

			if (continuousRefresh)
			{
				MainTabWindow_List tab = CtrlFDefOf.TD_List.TabWindow as MainTabWindow_List;
				if (tab.IsOpen)
					tab.findDesc.RemakeList();
			}
		}
	}
}
