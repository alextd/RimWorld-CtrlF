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
	//GameComponent to handle keypress
	class CtrlFGameComponent : GameComponent
	{
		public CtrlFGameComponent(Game g):base() { }
		
		//Ctrl-F handler
		public override void GameComponentOnGUI()
		{
			if (CtrlFDefOf.OpenCtrlF.KeyDownEvent && Event.current.control)
			{
				if (Event.current.shift)
				{
					CtrlFSearchWindow.Open();
					Event.current.Use();
					return;
				}

				QuerySearch search = new QuerySearch()
				{ name = "TD.CtrlFSearch".Translate(), active = true };

				ThingQuery query = Event.current.alt ? QueryForSelected() : null;
				bool selectedThing = query != null; //aka Event.current.alt && something's selected

				if (!selectedThing)
					query = ThingQueryMaker.MakeQuery(ThingQueryMaker.Query_Name);

				search.Children.Add(query, remake: selectedThing, focus: true);
				CtrlFSearchWindow.OpenWith(search, remake: false);
				Event.current.Use();
			}
		}


		public static ThingQuery QueryForSelected()
		{
			if (Find.Selector.SingleSelectedThing is Thing thing)
			{
				ThingDef def = thing.def;
				if (Find.Selector.SelectedObjectsListForReading.All(o => o is Thing t && t.def == def))
					return QueryForThing(thing);
			}
			else if (Find.Selector.SelectedZone is Zone zone)
			{
				ThingQueryZone queryZone = (ThingQueryZone)ThingQueryMaker.MakeQuery(ThingQueryMaker.Query_Zone);
				queryZone.sel = zone;
				return queryZone;
			}

			var defStuffs = Find.Selector.SelectedObjectsListForReading.Select(o => ((o as Thing).def, (o as Thing).Stuff)).Distinct().ToList();

			if (defStuffs.Count > 0)
			{
				ThingQueryAndOrGroup queryGroup = (ThingQueryAndOrGroup)ThingQueryMaker.MakeQuery(ThingQueryMaker.Query_AndOrGroup);
				foreach ((ThingDef def, ThingDef stuffDef) in defStuffs)
					queryGroup.Children.Add(QueryForThing(def, stuffDef));
				return queryGroup;
			}

			return null;
		}


		public static ThingQuery QueryForThing(Thing thing) => QueryForThing(thing.def, thing.Stuff);

		public static ThingQuery QueryForThing(ThingDef def, ThingDef stuffDef)
		{
			ThingQueryThingDef queryDef = (ThingQueryThingDef)ThingQueryMaker.MakeQuery(ThingQueryMaker.Query_Def);
			queryDef.sel = def;
			if(stuffDef == null)
				return queryDef;

			ThingQueryAndOrGroup queryGroup = (ThingQueryAndOrGroup)ThingQueryMaker.MakeQuery(ThingQueryMaker.Query_AndOrGroup);
			queryGroup.Children.matchAllQueries = true;

			queryGroup.Children.Add(queryDef, remake: false);

			ThingQueryStuff queryStuff = (ThingQueryStuff)ThingQueryMaker.MakeQuery(ThingQueryMaker.Query_Stuff);
			queryStuff.sel = stuffDef;
			queryGroup.Children.Add(queryStuff, remake: false);

			return queryGroup;
		}
	}
}
