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
		public QuerySearch search;
		private CtrlFQuerySearchDrawer queryDrawer;
		private CtrlFListWindow listWindow;

		public void SetSearch(QuerySearch newSearch = null, bool locked = false)
		{
			CtrlFRefresh prevRefresher = Current.Game.GetComponent<TDFindLibGameComp>().GetRefresher<CtrlFRefresh>(newSearch);

			this.search = newSearch ?? new QuerySearch()
			{ name = "TD.CtrlFSearch".Translate(), active = true };

			queryDrawer = new(search, "TD.CtrlFSearch".Translate()) { locked = locked };

			listWindow.SetSearch(search);

			if (prevRefresher != null)
				prevRefresher.search = search;
		}

		public CtrlFSearchWindow()
		{
			listWindow = new CtrlFListWindow(this);

			doCloseX = true;
			closeOnAccept = true;	//Actually overriden. Hahah.
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
			if (search == null)
			{
				SetSearch();
				search.Children.Add(ThingQueryMaker.MakeQuery<ThingQueryName>(), remake: false, focus: true);
				//Don't make the list - everything would match.
			}
		}

		public override void PostOpen()
		{
			if(!Find.WindowStack.IsOpen(listWindow))
				Find.WindowStack.Add(listWindow);
		}

		public override void OnCancelKeyPressed()
		{
			if (!search.OnCancelKeyPressed())
				base.OnCancelKeyPressed();
		}

		public override void PostClose()
		{
			if(!listWindow.separated)
				Find.WindowStack.TryRemove(listWindow, false);
		}

		public override void OnAcceptKeyPressed()
		{
			listWindow.thingsDrawer.GoToFirst();
			Event.current.Use();
		}


		public override void DoWindowContents(Rect fillRect)
		{
			queryDrawer.DrawQuerySearch(fillRect, row =>
			{
				SearchStorage.ButtonOpenLibrary(row);
				SearchStorage.ButtonChooseImportSearch(row,
					d => SetSearch(d, locked: queryDrawer.locked),
					CtrlFReceiver.transferTag,
					QuerySearch.CloneArgs.use);
				SearchStorage.ButtonChooseExportSearch(row, queryDrawer.search, CtrlFReceiver.transferTag);
			});
		}


		public static CtrlFSearchWindow window = new CtrlFSearchWindow();
		public static void OpenWith(QuerySearch search, bool locked = false, bool remake = true)
		{
			if (search != window.search)
				window.SetSearch(search, locked);

			Open(remake);
		}
		public static void Open(bool remake = true)
		{
			if (remake)
				window.search?.RemakeList();

			if (!Find.WindowStack.IsOpen(window))
				Find.WindowStack.Add(window);
			else
				Find.WindowStack.Notify_ClickedInsideWindow(window);
		}
	}

	public class CtrlFQuerySearchDrawer : QuerySearchDrawer
	{
		public CtrlFQuerySearchDrawer(QuerySearch search, string title) : base(search, title)
		{ }
	}

	public class CtrlFListWindow : Window
	{
		private CtrlFSearchWindow parent;
		private QuerySearch search;
		public CtrlFThingListDrawer thingsDrawer;
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
			//closeOnCancel = false;
			//closing controlled by Search window.
			preventCameraMotion = false;
			resizeable = true;
			draggable = true;
		}

		public void SetSearch(QuerySearch search = null)
		{
			this.search = search;

			thingsDrawer = new CtrlFThingListDrawer(search, this);
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
			Widgets.Label(headerRect, "TD.CtrlFFoundThings".Translate());
			Text.Anchor = default;

			fillRect.yMin = headerRect.yMax;

			thingsDrawer.DrawThingList(fillRect);
		}
	}

	public class CtrlFThingListDrawer : ThingListDrawer
	{
		private CtrlFListWindow parent;

		public CtrlFThingListDrawer(QuerySearch search, CtrlFListWindow parent) : base(search)
		{
			this.parent = parent;
		}

		public void Close()
		{
			Current.Game.GetComponent<TDFindLibGameComp>().RemoveRefresh(search);
		}

		public override void DrawIconButtons(WidgetRow row)
		{
			base.DrawIconButtons(row);

			//Manual refresh
			if (row.ButtonIcon(TexUI.RotRightTex, "TD.Refresh".Translate()))
				search.RemakeList();

			//Continuous refresh
			var comp = Current.Game.GetComponent<TDFindLibGameComp>();
			bool refresh = comp.IsRefreshing(search);
			if (row.ButtonIconColored(TexUI.ArrowTexRight,
				Find.TickManager.Paused ? "TD.DoesNotRefreshWhenPaused".Translate() : "TD.ContinuousRefreshAboutEverySecond".Translate(),
				refresh ? Color.green : Color.white,
				Color.Lerp(Color.green, Color.white, 0.5f)))
			{
				if (refresh)
					comp.RemoveRefresh(search);
				else
					comp.RegisterRefresh(new CtrlFRefresh(search)); //every 60 or so
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
			if (row.ButtonIcon(parent.separated ? CtrlFTex.Separated : CtrlFTex.Connected, "TD.ToggleToKeepThisWindowOpen".Translate()))
			{
				if (Event.current.button == 1)
					CtrlFSearchWindow.Open(false);
				else
					parent.separated = !parent.separated;
			}
		}


		public void GoToFirst()
		{
			if (search.result.allThings.FirstOrDefault() is Thing thing)
			{
				Find.Selector.ClearSelection();
				CameraJumper.TryJump(thing);
				TrySelect.Select(thing);
			}
		}
	}

	[StaticConstructorOnStartup]
	public class CtrlFReceiver : ISearchReceiver
	{
		static CtrlFReceiver()
		{
			SearchTransfer.Register(new CtrlFReceiver());
		}

		public static string transferTag = "Ctrl-F";//notranslate
		public string Source => transferTag;
		public string ReceiveName => "TD.ViewInCtrlF".Translate();
		public QuerySearch.CloneArgs CloneArgs => QuerySearch.CloneArgs.use;

		public bool CanReceive() => Find.CurrentMap != null;
		public void Receive(QuerySearch search) => CtrlFSearchWindow.OpenWith(search);
	}
}
