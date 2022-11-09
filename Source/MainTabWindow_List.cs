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
		public void SetFindDesc(FindDescription d = null, bool locked = false)
		{
			Current.Game.GetComponent<TD_Find_Lib.TDFindLibGameComp>().RemoveRefresh(findDesc);

			findDesc = d ?? new FindDescription(Find.CurrentMap) { name = "Ctrl-F" };
			filterDrawer = new FindDescriptionDrawer(findDesc, locked);
			thingsDrawer = new ThingListDrawer(findDesc);
		}

		public override void PostClose()
		{
			Current.Game.GetComponent<TD_Find_Lib.TDFindLibGameComp>().RemoveRefresh(findDesc);
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
				FilterStorageUtil.ButtonChooseLoadFilter(row, d => SetFindDesc(d.CloneForUse(Find.CurrentMap)));
			});
			thingsDrawer.DrawThingList(listRect);
		}

		public static void OpenWith(FindDescription desc, bool locked = false, bool remake = true)
		{
			MainTabWindow_List tab = CtrlFDefOf.TD_List.TabWindow as MainTabWindow_List;
			tab.SetFindDesc(desc, locked);
			if(remake)	tab.findDesc.RemakeList();
			Find.MainTabsRoot.SetCurrentTab(CtrlFDefOf.TD_List);
		}
	}
}
