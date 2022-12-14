// Copyright (c) 2022 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using FisheryLib;
using Verse.Sound;
using CodeInstructions = System.Collections.Generic.IEnumerable<HarmonyLib.CodeInstruction>;
using Log = Verse.Log;

namespace BetterLog;
public class Patches : ClassWithFishPatches
{
	public static readonly float LISTABLE_OPTION_SIZE = new ListableOption(null, null).minHeight; //45f; Should've been a const, but isn't for some reason.

	public class MainMenuDrawer_DoMainMenuControls_Patch : FishPatch
	{
		public override string Name => "Display a log button in the menu";
		public override string? Description => "Somewhere inbetween options and quit.";
		public override Delegate? TargetMethodGroup => MainMenuDrawer.DoMainMenuControls;
		public static void Prefix(ref Rect rect) => rect.height += LISTABLE_OPTION_SIZE;
		public static CodeInstructions Transpiler(CodeInstructions codes, MethodBase method)
			=> codes.ReplaceAt((codes, i)
				=> codes[i] == FishTranspiler.Call(() => new List<ListableOption>().Add(null!))
				&& FindMenuButtonOptionsString(codes, i),
				code => new[]
				{
					code,
					FishTranspiler.FirstLocalVariable(method, typeof(List<ListableOption>)),
					FishTranspiler.Call(AddNewButton)
				});

		private static bool FindMenuButtonOptionsString(List<CodeInstruction> codes, int i)
		{
			for ( ; i > 1; i--)
			{
				if (codes[i].opcode.LoadsString())
					return codes[i] == FishTranspiler.String("MenuButton-Options");
			}
			return false;
		}

		public static void AddNewButton(List<ListableOption> list) => list.Add(new("Open_Log".Translate(), () => Find.UIRoot.debugWindowOpener.ToggleLogWindow()));
	}

	public class MainMenuDrawer_DoMainMenuControls_Extra_Patch : FishPatch
	{
		public override bool ShowSettings => Get<MainMenuDrawer_DoMainMenuControls_Patch>().Enabled;
		public override string Name => "Only show the button while playing";
		public override string? Description => "Having this disabled makes the button show up in the entry screen too.";
		public override Delegate? TargetMethodGroup => MainMenuDrawer_DoMainMenuControls_Patch.AddNewButton;
		public static bool Prefix() => Current.ProgramState == ProgramState.Playing;
	}

	public class MainTabWindow_Menu_Patch : FishPatch
	{
		public override bool ShowSettings => false;
		public override MethodBase TargetMethodInfo => AccessTools.PropertyGetter(typeof(MainTabWindow_Menu), nameof(MainTabWindow_Menu.RequestedTabSize));
		public static Vector2 Postfix(Vector2 __result)
		{
			__result.y += LISTABLE_OPTION_SIZE;
			return __result;
		}
	}

	public class Log_Message_Patch : FishPatch
	{
		public override string Name => "Disable log messages";
		public override string? Description => "Fully prevents new white (or sometimes colored) log messages from being generated. Does not affect anything created before toggling this.";
		public override bool DefaultState => false;
		public override Delegate? TargetMethodGroup => (Action<string>)Log.Message;
		public static bool Prefix() => false;
	}

	public class Log_Warning_Patch : FishPatch
	{
		public override string Name => "Disable log warnings";
		public override string? Description => "Fully prevents new yellow (or sometimes colored) log warnings from being generated. Does not affect anything created before toggling this.";
		public override bool DefaultState => false;
		public override Delegate? TargetMethodGroup => (Action<string>)Log.Warning;
		public static bool Prefix() => false;
	}

	public class Log_Error_Patch : FishPatch
	{
		public override string Name => "Disable log errors";
		public override string? Description => "Fully prevents new red (or sometimes colored) log errors from being generated. Does not affect anything created before toggling this.";
		public override bool DefaultState => false;
		public override Delegate? TargetMethodGroup => (Action<string>)Log.Error;
		public static bool Prefix() => false;
	}

	public class Log_Message_Message_Patch : FishPatch
	{
		public override bool ShowSettings => !Log_Message_Patch.Enabled;
		public override string Name => "Trigger in-game messages for log messages";
		public override string? Description => "These show up on the top left.";
		public override bool DefaultState => false;
		public override Delegate? TargetMethodGroup => (Action<string>)Log.Message;

		public static void Postfix(string text)
		{
			if (!Log_Message_Patch.Enabled && Current.ProgramState == ProgramState.Playing)
				Messages.Message(new(text.Colorize(Settings.LogMessageColor), DefOfs.LogMessageMessage), false);
		}

		public static Log_Message_Patch Log_Message_Patch => _log_Message_Patch ??= Get<Log_Message_Patch>();
		private static Log_Message_Patch? _log_Message_Patch;
	}

	public class Log_Warning_Message_Patch : FishPatch
	{
		public override bool ShowSettings => !Log_Warning_Patch.Enabled;
		public override string Name => "Trigger in-game messages for log warnings";
		public override string? Description => "These show up on the top left.";
		public override bool DefaultState => false;
		public override Delegate? TargetMethodGroup => (Action<string>)Log.Warning;

		public static void Postfix(string text)
		{
			if (!Log_Warning_Patch.Enabled && Current.ProgramState == ProgramState.Playing)
				Messages.Message(new(text.Colorize(Settings.LogWarningColor), DefOfs.LogWarningMessage), false);
		}

		public static Log_Warning_Patch Log_Warning_Patch => _log_Warning_Patch ??= Get<Log_Warning_Patch>();
		private static Log_Warning_Patch? _log_Warning_Patch;
	}

	public class Log_Error_Message_Patch : FishPatch
	{
		public override bool ShowSettings => !Log_Error_Patch.Enabled;
		public override string Name => "Trigger in-game messages for log errors";
		public override string? Description => "These show up on the top left.";
		public override Delegate? TargetMethodGroup => (Action<string>)Log.Error;

		public static void Postfix(string text)
		{
			try
			{
				if (!Log_Error_Patch.Enabled && Current.ProgramState == ProgramState.Playing)
					Messages.Message(new(text.Colorize(Settings.LogErrorColor), DefOfs.LogErrorMessage), false);
			}
			catch (Exception ex)
			{
				Debug.LogError($"An error occurred while logging an error: {ex}");
			}
		}

		public static Log_Error_Patch Log_Error_Patch => _log_Error_Patch ??= Get<Log_Error_Patch>();
		private static Log_Error_Patch? _log_Error_Patch;
	}

	public class Log_Notify_MessageReceivedThreadedInternal_Disable_Patch : FishPatch
	{
		public override string Name => "Disable Log Limit";
		public override string? Description => "RimWorld normally stops logging after 1000 messages. This makes it not do that.";
		public override bool DefaultState => false;
		public override Delegate? TargetMethodGroup => Log.Notify_MessageReceivedThreadedInternal;
		public static bool Prefix() => false;
	}

	public class Log_Notify_MessageReceivedThreadedInternal_Modify_Patch : FishPatch
	{
		public override bool ShowSettings => !Get<Log_Notify_MessageReceivedThreadedInternal_Disable_Patch>().Enabled;
		public override string Name => "Modify Log Limit";
		public override string? Description => "RimWorld normally stops logging after 1000 messages. This allows changing that number. Use the slider below to set the new value.";
		public override Delegate? TargetMethodGroup => Log.Notify_MessageReceivedThreadedInternal;
		public static CodeInstructions Transpiler(CodeInstructions codes)
			=> codes.Replace(code => code.operand is int number && number == 1000,
				code => FishTranspiler.PropertyGetter(typeof(Settings), nameof(Settings.LoggingLimit)).WithLabelsAndBlocks(code));
	}

	public class FileLog_Log_Patch : FishPatch
	{
		public override string Name => "Disable Harmony FileLog";
		public override string? Description => "This stops Harmony from creating a harmony.log.txt file on the desktop. Requires at least one mod running harmony in debug mode to show up in the first place.";
		public override IEnumerable<Delegate>? TargetMethodGroups => new Delegate[] { FileLog.Log, FileLog.FlushBuffer };
		public static bool Prefix()
		{
			FileLog.Reset();
			Harmony.DEBUG = false;
			return false;
		}
	}

	public static bool ShowErrors { get; set; } = true;
	public static bool ShowWarnings { get; set; } = true;
	public static bool ShowMessages { get; set; } = true;

	public class EditWindow_Log_DoMessagesListing_Patch : FishPatch
	{
		public override bool ShowSettings => false;
		public override Expression<Action>? TargetMethod => () => default(EditWindow_Log)!.DoMessagesListing(default);
		public override int TranspilerMethodPriority => Priority.LowerThanNormal;
		public static CodeInstructions Transpiler(CodeInstructions codes)
			=> codes.InsertAfter(c => c == FishTranspiler.PropertyGetter(typeof(Log), nameof(Log.Messages)),
				 FishTranspiler.Call(FilteredMessages));
		public static IEnumerable<LogMessage> FilteredMessages(IEnumerable<LogMessage> messages) => messages.Where(message => IsAllowedType(message.type));
		public static bool IsAllowedType(LogMessageType type) => type switch
		{
			LogMessageType.Error => ShowErrors,
			LogMessageType.Warning => ShowWarnings,
			LogMessageType.Message => ShowMessages,
			_ => true
		};
	}

	public class EditWindow_Log_DoWindowContents_Patch : FishPatch
	{
		public override bool ShowSettings => false;
		public override MethodBase TargetMethodInfo => AccessTools.Method(typeof(EditWindow_Log), nameof(EditWindow_Log.DoWindowContents));
		public static CodeInstructions Transpiler(CodeInstructions instructions)
			=> instructions
			.ReplaceAt((codes, i)
				=> codes[i] == _widget_ButtonText
				  && i - 8 > 0 && codes[i - 8] == FishTranspiler.String("Trace big"),
				code => FishTranspiler.Call(ShowMessagesButton).WithLabelsAndBlocks(code))

			.ReplaceAt((codes, i)
				=> codes[i] == _widget_ButtonText
				  && i - 8 > 0 && codes[i - 8] == FishTranspiler.String("Trace medium"),
				code => FishTranspiler.Call(ShowWarningsButton).WithLabelsAndBlocks(code))

			.ReplaceAt((codes, i)
				=> codes[i] == _widget_ButtonText
				  && i - 8 > 0 && codes[i - 8] == FishTranspiler.String("Trace small"),
				code => FishTranspiler.Call(ShowErrorsButton).WithLabelsAndBlocks(code))
			
			.InsertAfter(c => c.opcode == OpCodes.Ldstr,
					FishTranspiler.Call(Translator.TranslateSimple))
			
			.Replace(c => c.opcode == OpCodes.Ldstr && c.operand is string s && s != string.Empty,
				code => code.With(operand: ((string)code.operand).Replace(' ', '_')));

		public static bool ShowMessagesButton(WidgetRow widgetRow, string label, string tooltip, bool drawBackground, bool doMouseoverSound, bool active, float? fixedWidth)
		{
			ShowMessages = ToggleableButtonText(widgetRow, "Show_Messages".Translate(), ShowMessages, "Toggle_showing_white__or_sometimes_colored__log_messages.".Translate());
			return false;
		}
		public static bool ShowWarningsButton(WidgetRow widgetRow, string label, string tooltip, bool drawBackground, bool doMouseoverSound, bool active, float? fixedWidth)
		{
			ShowWarnings = ToggleableButtonText(widgetRow, "Show_Warnings".Translate(), ShowWarnings, "Toggle_showing_yellow_log_warnings.".Translate());
			return false;
		}
		public static bool ShowErrorsButton(WidgetRow widgetRow, string label, string tooltip, bool drawBackground, bool doMouseoverSound, bool active, float? fixedWidth)
		{
			ShowErrors = ToggleableButtonText(widgetRow, "Show_Errors".Translate(), ShowErrors, "Toggle_showing_red_log_errors.".Translate());
			return false;
		}

		public static bool ToggleableButtonText(WidgetRow widgetRow, string label, bool state, string? tooltip = null)
		{
			var rect = widgetRow.ButtonRect(label);
			var toggled = Widgets.ButtonText(rect with { width = rect.width + rect.height }, label, true, true, true);
			if (!tooltip.NullOrEmpty())
				TooltipHandler.TipRegion(rect, tooltip);
			var rectHeightDividedByEight = rect.height / 8f;
			GUI.DrawTexture(new Rect(rect.xMax + rectHeightDividedByEight, rect.y + rectHeightDividedByEight, rectHeightDividedByEight * 6f, rectHeightDividedByEight * 6f), state ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex);
			widgetRow.IncrementPosition(rect.height);
			//if (mouseoverSound != null)
			//	MouseoverSounds.DoRegion(rect, mouseoverSound);
			if (toggled)
			{
				if (!state)
					SoundDefOf.Tick_High.PlayOneShotOnCamera();
				else
					SoundDefOf.Tick_Low.PlayOneShotOnCamera();
				return !state;
			}
			return state;
		}

		private static FishTranspiler.Container _widget_ButtonText = FishTranspiler.Call(typeof(WidgetRow), nameof(WidgetRow.ButtonText));
	}

	public class LogMessage_Color_Patch : FishPatch
	{
		public override bool ShowSettings => false;
		public override MethodBase TargetMethodInfo => AccessTools.PropertyGetter(typeof(LogMessage), nameof(LogMessage.Color));
		public static Color Postfix(Color __result, LogMessage __instance) => __instance.type switch
		{
			LogMessageType.Message => Settings.LogMessageColor,
			LogMessageType.Warning => Settings.LogWarningColor,
			LogMessageType.Error => Settings.LogErrorColor,
			_ => Settings.LogMessageColor
		};
	}

	public class Decolorize_Patch : FishPatch
	{
		public override string Name => "Disallow custom colors for mods";
		public override string? Description => "Enabling this makes all log messages show up in default colors, or those specified through this mod, overwriting any values other mods may have set.";
		public override IEnumerable<MethodBase> TargetMethodInfos => new[] { ((Action<string>)Log.Message).Method, ((Action<string>)Log.Warning).Method, ((Action<string>)Log.Error).Method };
		public static void Prefix(ref string text) => text = ColorRegex.Replace(text, string.Empty);
		public static void Prepare()
		{
			if (RocketManLogPatchMethod != null)
				Harmony.Patch(RocketManLogPatchMethod, new(((Delegate)DisableRocketmanLogPatch).Method));
		}
		public static void Cleanup()
		{
			if (RocketManLogPatchMethod != null)
				Harmony.Unpatch(RocketManLogPatchMethod, ((Delegate)DisableRocketmanLogPatch).Method);
		}
		public static bool DisableRocketmanLogPatch(ref CodeInstructions __result, CodeInstructions instructions)
		{
			__result = instructions;
			return false;
		}
		public static Regex ColorRegex { get; } = new("<\\/*color[^>]*>");
		public static MethodInfo RocketManLogPatchMethod
			=> _rocketManLogPatchMethod.HasValue && _rocketManLogPatchMethod.Value.IsSet ? _rocketManLogPatchMethod.Value.Method
			: (_rocketManLogPatchMethod = (AccessTools.Method("RocketMan.EditWindow_Log_DoMessagesListing_Patch:Transpiler"), true)).Value.Method;
		private static (MethodInfo Method, bool IsSet)? _rocketManLogPatchMethod;
	}

	public class Translator_PseudoTranslate_Patch : FishPatch
	{
		public override string Name => "Disable cursed text in dev mode";
		public override bool DefaultState => false;
		public override string? Description => "RimWorld normally shows any text that is missing translations in zalgo when dev mode is enabled. This disables that behaviour.";
		public override Delegate? TargetMethodGroup => Translator.PseudoTranslated;
		public static CodeInstructions Transpiler(CodeInstructions codes, MethodBase method)
		{
			yield return FishTranspiler.FirstArgument(method, typeof(string));
			yield return FishTranspiler.Return;
		}
	}
}