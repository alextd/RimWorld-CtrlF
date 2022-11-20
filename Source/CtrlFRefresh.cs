﻿using System;
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
			CtrlFWindowSearch.OpenWith(desc, remake:false);
		}
	}
}