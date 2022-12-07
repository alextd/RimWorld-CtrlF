using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using TD_Find_Lib;

namespace Ctrl_F
{
	public class CtrlFRefresh : RefreshQuerySearch
	{
		public CtrlFRefresh(QuerySearch search): base(search, "TD.CtrlF".Translate(), 60, false)
		{
		}

		public override void OpenUI(QuerySearch search)
		{
			MainButtonWorker_ToggleCtrlFWindow.OpenWith(search, remake:false);
		}
	}
}
