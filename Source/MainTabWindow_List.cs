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
	public class MainTabWindow_List : MainTabWindow
	{
		private FindDescription findDesc;
		private FindDescriptionDrawer filterDrawer;
		private ThingListDrawer thingsDrawer;
		public void SetFindDesc(FindDescription desc = null, bool locked = false)
		{
			Current.Game.GetComponent<TD_Find_Lib.TDFindLibGameComp>().RemoveRefresh(findDesc);

			findDesc = desc ?? new FindDescription(Find.CurrentMap) { name = "Ctrl-F" };
			filterDrawer = new FindDescriptionDrawer(findDesc) { locked = locked };

			thingsDrawer?.Close();
			thingsDrawer = new ThingListDrawer(findDesc);
		}

		public override void PostClose()
		{
			thingsDrawer.Close();
		}

		public override Vector2 RequestedTabSize => new Vector2(900, base.RequestedTabSize.y);

		public override void PreOpen()
		{
			base.PreOpen();
			if (findDesc == null)
			{
				SetFindDesc();
				findDesc.Children.Add(ListFilterMaker.MakeFilter(ListFilterMaker.Filter_Name), remake: false, focus: true);
				//Don't make the list - everything would match.
			}
			else
				findDesc.RemakeList();	
		}

		public override void DoWindowContents(Rect fillRect)
		{
			Rect filterRect = fillRect.LeftPart(0.60f);
			Rect listRect = fillRect.RightPart(0.39f);

			GUI.color = Color.grey;
			Widgets.DrawLineVertical(listRect.x - 3, 0, listRect.height);
			GUI.color = Color.white;

			filterDrawer.DrawFindDescription(filterRect, row =>
			{
				FilterStorageUtil.ButtonOpenSettings(row);
				if(!filterDrawer.locked)
					FilterStorageUtil.ButtonChooseLoadFilter(row, d => SetFindDesc(d.CloneForUse(Find.CurrentMap)));

				FilterStorageUtil.ButtonChooseExportFilter(row, filterDrawer.findDesc, "Ctrl-F");
			});
			thingsDrawer.DrawThingList(listRect, row =>
			{
				//Manual refresh
				if (row.ButtonIcon(TexUI.RotRightTex, "TD.Refresh".Translate()))
					findDesc.RemakeList();

				//Continuous refresh
				var comp = Current.Game.GetComponent<TDFindLibGameComp>();
				bool refresh = comp.IsRefreshing(findDesc);
				if (row.ButtonIconColored(TexUI.ArrowTexRight,
					Find.TickManager.Paused ? "(Does not refresh when paused)" : "TD.ContinuousRefreshAboutEverySecond".Translate(),
					refresh ? Color.green : Color.white,
					Color.Lerp(Color.green, Color.white, 0.5f)))
				{
					if (refresh)
						comp.RemoveRefresh(findDesc);
					else
						comp.RegisterRefresh(findDesc, "Ctrl-F", 60); //every 60 or so
				}

				if (Find.TickManager.Paused)
				{
					// Thank you publicizer
					row.IncrementPosition(-WidgetRow.IconSize);
					GUI.color = new Color(1, 1, 1, .5f);
					row.Icon(FindTex.Cancel);
					GUI.color = Color.white;
				}
			});
		}

		public static void OpenWith(FindDescription desc, bool locked = false)
		{
			MainTabWindow_List tab = CtrlFDefOf.TD_List.TabWindow as MainTabWindow_List;
			tab.SetFindDesc(desc, locked);
			Find.MainTabsRoot.SetCurrentTab(CtrlFDefOf.TD_List);
		}
	}
}
