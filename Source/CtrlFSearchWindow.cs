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
	public class CtrlFSearchWindow : Window
	{
		public FindDescription findDesc;
		private CtrlFFindDescriptionDrawer filterDrawer;
		private CtrlFListWindow listWindow;

		public void SetFindDesc(FindDescription desc = null, bool locked = false)
		{
			CtrlFRefresh prevRefresher = Current.Game.GetComponent<TDFindLibGameComp>().GetRefresher<CtrlFRefresh>(findDesc);

			findDesc = desc ?? new FindDescription()
			{ name = "Ctrl-F Search", active = true };

			filterDrawer = new(findDesc, "Ctrl-F Search") { locked = locked };

			listWindow.SetFindDesc(findDesc);

			if (prevRefresher != null)
				prevRefresher.desc = findDesc;
		}

		public CtrlFSearchWindow()
		{
			listWindow = new CtrlFListWindow(this);

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
			windowRect.x = Window.StandardMargin;
			windowRect.y = Window.StandardMargin;
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
			if(!Find.WindowStack.IsOpen(listWindow))
				Find.WindowStack.Add(listWindow);
		}

		public override void PostClose()
		{
			if(!listWindow.separated)
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


		public static CtrlFSearchWindow window = new CtrlFSearchWindow();
		public static void OpenWith(FindDescription desc, bool locked = false, bool remake = true)
		{
			if (desc != window.findDesc)
				window.SetFindDesc(desc, locked);

			if (remake)
				desc.RemakeList();

			if (!Find.WindowStack.IsOpen(window))
				Find.WindowStack.Add(window);
			else
				Find.WindowStack.Notify_ClickedInsideWindow(window);
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

	public class CtrlFFindDescriptionDrawer : FindDescriptionDrawer
	{
		public CtrlFFindDescriptionDrawer(FindDescription findDesc, string title) : base(findDesc, title)
		{ }
}

	public class CtrlFListWindow : Window
	{
		private CtrlFSearchWindow parent;
		private FindDescription findDesc;
		private CtrlFThingListDrawer thingsDrawer;
		private bool _separated;

		public bool separated
		{
			get => _separated;
			set
			{
				_separated = value;
				doCloseX = _separated;
			}
		}

		public CtrlFListWindow(CtrlFSearchWindow parent)
		{
			this.parent = parent;
			//soundAppear = null;
			//soundClose = SoundDefOf.TabClose;
			//doCloseX = true;
			closeOnAccept = false;
			closeOnCancel = false;
			//closing controlled by Search window.
			preventCameraMotion = false;
			resizeable = true;
			draggable = true;
		}

		public void SetFindDesc(FindDescription desc = null)
		{
			findDesc = desc;

			thingsDrawer = new CtrlFThingListDrawer(desc, this);
		}

		public override Vector2 InitialSize => new Vector2(360, 720);

		public override void SetInitialSizeAndPosition()
		{
			base.SetInitialSizeAndPosition();
			windowRect.x = UI.screenWidth - windowRect.width - Window.StandardMargin;
			windowRect.y = Window.StandardMargin;
		}

		public override void PostClose()
		{
			Find.WindowStack.TryRemove(parent, false);
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
		private CtrlFListWindow parent;

		public CtrlFThingListDrawer(FindDescription findDesc, CtrlFListWindow parent) : base(findDesc)
		{
			this.parent = parent;
		}

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

			//Keep open
			if (row.ButtonIcon(parent.separated ? CtrlFFindTex.Separated : CtrlFFindTex.Connected, "Toggle to keep this window open when the search window is closed"))
				parent.separated = !parent.separated;
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
		public void Receive(FindDescription desc) => CtrlFSearchWindow.OpenWith(desc);
	}
}
