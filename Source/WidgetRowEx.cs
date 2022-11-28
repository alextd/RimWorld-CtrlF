using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace Ctrl_F
{
	public static class WidgetRowEx
	{
		public static bool ButtonIconColored(this WidgetRow row, Texture2D tex, string tooltip = null, Color? color = null, Color? mouseoverColor = null, Color? backgroundColor = null, Color? mouseoverBackgroundColor = null, bool doMouseoverSound = true, float overrideSize = -1f)
		{
			float num = ((overrideSize > 0f) ? overrideSize : 24f);
			float num2 = (24f - num) / 2f;
			row.IncrementYIfWillExceedMaxWidth(num);
			Rect rect = new Rect(row.LeftX(num) + num2, row.curY + num2, num, num);
			if (doMouseoverSound)
			{
				Verse.Sound.MouseoverSounds.DoRegion(rect);
			}
			if (mouseoverBackgroundColor.HasValue && Mouse.IsOver(rect))
			{
				Widgets.DrawRectFast(rect, mouseoverBackgroundColor.Value);
			}
			else if (backgroundColor.HasValue && !Mouse.IsOver(rect))
			{
				Widgets.DrawRectFast(rect, backgroundColor.Value);
			}
			bool result = Widgets.ButtonImage(rect, tex, color ?? Color.white, mouseoverColor ?? GenUI.MouseoverColor);
			row.IncrementPosition(num);
			if (!tooltip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, tooltip);
			}
			return result;
		}
	}
}
