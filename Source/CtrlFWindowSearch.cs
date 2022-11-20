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
	public class CtrlFWindowSearch : Window
	{
		public FindDescription findDesc;
		private FindDescriptionDrawer filterDrawer;
		private CtrlFWindowList listWindow;

		public void SetFindDesc(FindDescription desc = null, bool locked = false)
		{
			Current.Game.GetComponent<TD_Find_Lib.TDFindLibGameComp>().RemoveRefresh(findDesc);

			findDesc = desc ?? new FindDescription()
				{ name = "Ctrl-F Search", active = true };

			filterDrawer = new FindDescriptionDrawer(findDesc, "Ctrl-F Search") { locked = locked };

			listWindow.SetFindDesc(findDesc);
		}

		public CtrlFWindowSearch()
		{
			listWindow = new CtrlFWindowList();

			layer = WindowLayer.GameUI;
			doCloseButton = true;
			doCloseX = true;
			closeOnAccept = false;
			//closeOnCancel = false;
			preventCameraMotion = false;
			resizeable = true;
			draggable = true;
		}

		public override Vector2 InitialSize => new Vector2(600, 600);

		public override void SetInitialSizeAndPosition()
		{
			base.SetInitialSizeAndPosition();
			windowRect.x = 0f;
			windowRect.y = 0f;
		}

		public override void PreOpen()
		{
			base.PreOpen();
			if (findDesc == null)
			{
				SetFindDesc();
				findDesc.Children.Add(ListFilterMaker.MakeFilter(ListFilterMaker.Filter_Name), remake: false, focus: true);
				//Don't make the list - everything would match.
			}
		}

		public override void PostOpen()
		{
			Find.WindowStack.Add(listWindow);
		}

		public override void PostClose()
		{
			Find.WindowStack.TryRemove(listWindow, false);
		}

		public override void DoWindowContents(Rect fillRect)
		{
			filterDrawer.DrawFindDescription(fillRect, row =>
			{
				FilterStorageUtil.ButtonOpenSettings(row);
				FilterStorageUtil.ButtonChooseImportFilter(row,
					d => SetFindDesc(d, locked: filterDrawer.locked),
					"Ctrl-F",
					FindDescription.CloneArgs.use);
				FilterStorageUtil.ButtonChooseExportFilter(row, filterDrawer.findDesc, "Ctrl-F");
			});
		}


		public static CtrlFWindowSearch window = new CtrlFWindowSearch();
		public static void OpenWith(FindDescription desc, bool locked = false, bool remake = true)
		{
			if(desc != window.findDesc)
				window.SetFindDesc(desc, locked);

			if (remake)
				desc.RemakeList();

			if (!Find.WindowStack.IsOpen(window))
				Find.WindowStack.Add(window);
		}
		public static void Open()
		{
			//Set to top ?
			if (!Find.WindowStack.IsOpen(window))
			{
				window.findDesc?.RemakeList();
				Find.WindowStack.Add(window);
			}
		}
}

	public class CtrlFWindowList : Window
	{
		private FindDescription findDesc;
		private CtrlFThingListDrawer thingsDrawer;
		public void SetFindDesc(FindDescription desc = null)
		{
			findDesc = desc;

			thingsDrawer?.Close();
			thingsDrawer = new CtrlFThingListDrawer(desc);
		}

		public CtrlFWindowList()
		{
			layer = WindowLayer.GameUI;
			//soundAppear = null;
			//soundClose = SoundDefOf.TabClose;
			//doCloseButton = true;
			//doCloseX = true;
			closeOnAccept = false;
			closeOnCancel = false;
			//closing controlled by Search window.
			preventCameraMotion = false;
			resizeable = true;
			draggable = true;
		}

		public override Vector2 InitialSize => new Vector2(360, 720);

		public override void SetInitialSizeAndPosition()
		{
			base.SetInitialSizeAndPosition();
			windowRect.x = UI.screenWidth - windowRect.width;
			windowRect.y = 0;
		}

		public override void PostClose()
		{
			thingsDrawer.Close();
		}

		public override void DoWindowContents(Rect fillRect)
		{
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.UpperCenter;
			Rect headerRect = fillRect.TopPartPixels(Text.LineHeight).AtZero();
			Widgets.Label(headerRect, "Ctrl-F Found Things");
			Text.Anchor = default;

			fillRect.yMin = headerRect.yMax;

			thingsDrawer.DrawThingList(fillRect);
		}
	}

	class CtrlFThingListDrawer : ThingListDrawer
	{
		public CtrlFThingListDrawer(FindDescription findDesc) : base(findDesc)
		{ }

		public void Close()
		{
			Current.Game.GetComponent<TDFindLibGameComp>().RemoveRefresh(findDesc);
		}

		public override void DrawIconButtons(WidgetRow row)
		{
			base.DrawIconButtons(row);

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
					comp.RegisterRefresh(new CtrlFRefresh(findDesc)); //every 60 or so
			}

			if (Find.TickManager.Paused)
			{
				// Thank you publicizer
				row.IncrementPosition(-WidgetRow.IconSize);
				GUI.color = new Color(1, 1, 1, .5f);
				row.Icon(FindTex.Cancel);
				GUI.color = Color.white;
			}

		}
	}

	[StaticConstructorOnStartup]
	public class CtrlFReceiver : IFilterReceiver
	{
		static CtrlFReceiver()
		{
			FilterTransfer.Register(new CtrlFReceiver());
		}

		public string Source => "Ctrl-F";
		public string ReceiveName => "View in Ctrl-F";
		public FindDescription.CloneArgs CloneArgs => FindDescription.CloneArgs.use;

		public bool CanReceive() => Find.CurrentMap != null;
		public void Receive(FindDescription desc) => CtrlFWindowSearch.OpenWith(desc);
	}
}
