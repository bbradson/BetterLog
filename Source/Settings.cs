// Copyright (c) 2022 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Linq;

namespace BetterLog;

public class Settings : ModSettings
{
	public const string LOGGING_LIMIT_SLIDER_LABEL = "Max messages limit:";
	public const string SHOW_MAIN_BUTTON = "Display a log button in the main button bar";

	public const string SHOW_MAIN_BUTTON_TOOLTIP
		= "Show a button for opening the log window at the bottom right in the main button bar.";

	public static bool ButtonDefLoaded { get; set; }

	public static int LoggingLimit
	{
		get => _loggingLimit;
		set => _loggingLimit = value;
	}

	public static Color LogMessageColor
	{
		get => _logMessageColor;
		set => _logMessageColor = value;
	}

	public static Color LogWarningColor
	{
		get => _logWarningColor;
		set => _logWarningColor = value;
	}

	public static Color LogErrorColor
	{
		get => _logErrorColor;
		set => _logErrorColor = value;
	}

	public static bool ShowMainButton
	{
		get => _showMainButton;
		set
		{
			_showMainButton = value;
			if (ButtonDefLoaded)
				LogMainButtonDef.DebugLog.buttonVisible = value;
		}
	}

	public static List<Color> AllColors
		=> _allColors
			??= DefDatabase<ColorDef>.AllDefsListForReading.Select(static def => def.color)
				.Union(typeof(ColorLibrary).GetFields(AccessTools.allDeclared)
					.Select(static field => (Color)field.GetValue(null)))
				.Union(new[] { Color.red, Color.yellow, Color.white }).ToList();

	public override void ExposeData()
	{
		base.ExposeData();

		foreach (var patchClass in FishPatch.AllPatchClasses)
		{
			var patchHolder = patchClass.PatchHolder;
			Scribe_Deep.Look(ref patchHolder, patchClass.GetType().Name, patchClass.GetType());
			patchClass.PatchHolder = patchHolder;
		}

		Scribe_Values.Look(ref _loggingLimit, "loggingLimit", 2000);

		var showMainButton = ShowMainButton;
		Scribe_Values.Look(ref showMainButton, "showMainButton", true);
		ShowMainButton = showMainButton;

		var logMessageColor = LogMessageColor;
		Scribe_Values.Look(ref logMessageColor, "logMessageColor", Color.white);
		LogMessageColor = logMessageColor;

		var logWarningColor = LogWarningColor;
		Scribe_Values.Look(ref logWarningColor, "logWarningColor", Color.yellow);
		LogWarningColor = logWarningColor;

		var logErrorColor = LogErrorColor;
		Scribe_Values.Look(ref logErrorColor, "logErrorColor", ColorLibrary.LogError);
		LogErrorColor = logErrorColor;
	}

	public static void DoSettingsWindowContents(Rect inRect)
	{
		Widgets.BeginScrollView(inRect, ref _scrollPosition, _scrollRect);
		var ls = new Listing_Standard { maxOneColumn = true };
		ls.Begin(_scrollRect);

		ContentAbovePatches(ls, inRect);

		foreach (var patchClass in FishPatch.AllPatchClasses)
		{
			//if (patchClass is IHasDescription classWithDescription)
			//	ls.Label(patchClass.GetType().Name, tooltip: classWithDescription.Description);
			//else
			//	ls.Label(patchClass.GetType().Name);

			//ls.GapLine(2f);
			foreach (var patch in patchClass.Patches)
			{
				if (!patch.ShowSettings)
					continue;

				if (ShouldSkipForScrollView(inRect.height, Text.LineHeight, ls.curY, _scrollPosition.y))
				{
					ls.curY += Text.LineHeight;
					continue;
				}

				var check = patch.Enabled;
				if (patch.Description == null)
					Widgets.DrawHighlightIfMouseover(_scrollRect with { y = ls.curY, height = Text.LineHeight });
				var label = patch.Name;
				if (label.EndsWith("_Patch"))
					label = label.Remove(label.Length - 6);
				ls.CheckboxLabeled(label, ref check, patch.Description);
				if (check != patch.Enabled)
					patch.Enabled = check;
			}

			ls.Gap();
		}

		ContentBelowPatches(ls, inRect);

		ls.End();

		Widgets.EndScrollView();
		_scrollRect = _scrollRect with
		{
			height = ls.curY + 50f, width = inRect.width - GUI.skin.verticalScrollbar.fixedWidth - 5f
		};
	}

	public static void ContentAbovePatches(Listing_Standard ls, Rect inRect)
	{
		if (ShouldSkipForScrollView(inRect.height, Text.LineHeight, ls.curY, _scrollPosition.y))
		{
			ls.curY += Text.LineHeight;
			return;
		}

		var showMainButton = ShowMainButton;
		ls.CheckboxLabeled(SHOW_MAIN_BUTTON, ref showMainButton, SHOW_MAIN_BUTTON_TOOLTIP);
		if (showMainButton != ShowMainButton)
			ShowMainButton = showMainButton;
	}

	public const int COLOR_SIZE = 22;
	public const int COLOR_PADDING = 2;
	public const int COLOR_BOX_SIZE = COLOR_SIZE + (COLOR_PADDING * 3);

	public static void ContentBelowPatches(Listing_Standard ls, Rect inRect)
	{
		ls.Gap();
		if (!FishPatch.Get<Patches.Log_Notify_MessageReceivedThreadedInternal_Disable_Patch>().Enabled
			&& FishPatch.Get<Patches.Log_Notify_MessageReceivedThreadedInternal_Modify_Patch>().Enabled)
		{
			var newLoggingLimit = Slider(ls, _scrollRect, LoggingLimit, LOGGING_LIMIT_SLIDER_LABEL, 0, 10000, 100);
			if (newLoggingLimit != LoggingLimit)
				LoggingLimit = newLoggingLimit;
		}

		ls.Gap();
		var logMessageColor = LogMessageColor;
		if (ColorPicker(ls, "Log Message Color:", ref logMessageColor, inRect))
			LogMessageColor = logMessageColor;

		ls.Gap();
		var logWarningColor = LogWarningColor;
		if (ColorPicker(ls, "Log Warning Color:", ref logWarningColor, inRect))
			LogWarningColor = logWarningColor;

		ls.Gap();
		var logErrorColor = LogErrorColor;
		if (ColorPicker(ls, "Log Error Color:", ref logErrorColor, inRect))
			LogErrorColor = logErrorColor;
	}

	public static bool ColorPicker(Listing_Standard ls, string label, ref Color color, Rect inRect)
	{
		ls.Label(label);
		var totalRows = Mathf.CeilToInt((float)AllColors.Count / (int)(_scrollRect.width / COLOR_BOX_SIZE));
		var colorPickerHeight = (totalRows + 0) * COLOR_BOX_SIZE;
		var colorPickerRect = _scrollRect with { y = ls.curY, height = colorPickerHeight };
		ls.curY += colorPickerHeight;

		return !ShouldSkipForScrollView(inRect.height, colorPickerHeight, ls.curY - colorPickerHeight,
				_scrollPosition.y)
#if V1_3
			&& Widgets.ColorSelector(colorPickerRect, ref color, AllColors, colorSize: COLOR_SIZE,
				colorPadding: COLOR_PADDING);
#else
			&& Widgets.ColorSelector(colorPickerRect, ref color, AllColors, out _, colorSize: COLOR_SIZE,
				colorPadding: COLOR_PADDING);
#endif
	}

	public static int Slider(Listing_Standard ls, Rect inRect, int value, string label, int min, int max,
		int roundToNearest)
	{
		var minimumLoggingLimit = value;
		var previousAlignment = Text.Anchor;
		Text.Anchor = TextAnchor.MiddleRight;
		Widgets.Label(
			inRect with { y = ls.curY, height = Text.CalcHeight(minimumLoggingLimit.ToString(), ls.ColumnWidth) },
			minimumLoggingLimit.ToString());
		Text.Anchor = previousAlignment;
		ls.Label(label);
		var newLoggingLimit
			= GenMath.RoundTo(Convert.ToInt32(ls.Slider(minimumLoggingLimit, min, max)), roundToNearest);
		
		return newLoggingLimit;
	}

	public static bool ShouldSkipForScrollView(float scrollViewSize, float entrySize, float entryPosition,
		float scrollPosition)
		=> entryPosition + entrySize < scrollPosition || entryPosition > scrollPosition + scrollViewSize;

	private static List<Color>? _allColors;
	private static int _loggingLimit = 2000;
	private static bool _showMainButton = true;
	private static Color _logMessageColor = Color.white;
	private static Color _logWarningColor = Color.yellow;
	private static Color _logErrorColor = ColorLibrary.LogError;
	private static Rect _scrollRect = new(0f, 0f, 500f, 9001f);
	private static Vector2 _scrollPosition;
}