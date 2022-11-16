using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using TD_Find_Lib;

namespace Ctrl_F
{
	public class CtrlFRefresh : RefreshFindDesc
	{
		public CtrlFRefresh(FindDescription desc): base(desc, "Ctrl-F", 60, false)
		{
		}

		public override void OpenUI(FindDescription desc)
		{
			if (Current.Game.GetComponent<CtrlFGameComponent>().window is CtrlFWindowSearch window)
			{
				if (Find.WindowStack.IsOpen(window))
					Find.WindowStack.Notify_ClickedInsideWindow(window);
				else
					Find.WindowStack.Add(window);
			}
			//Ctrl-F UI should be open if there is a refreshing list.
			//else
			//	CtrlFWindowSearch.OpenWith(desc);
		}
	}
}
