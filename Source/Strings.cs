// Copyright (c) 2022 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

// ReSharper disable UnassignedReadonlyField
namespace BetterLog;

#pragma warning disable CS8618
public static class Strings
{
	public static class Translated
	{
		public static readonly string
			Open_Log,
			Show_Messages,
			Show_Warnings,
			Show_Errors,
			Toggle_showing_messages = "Toggle_showing_white__or_sometimes_colored__log_messages.",
			Toggle_showing_warnings = "Toggle_showing_yellow_log_warnings.",
			Toggle_showing_errors = "Toggle_showing_red_log_errors.";
		
		static Translated() => AssignAllStringFields(typeof(Translated), Translator.TranslateSimple);
	}
	
	private static void AssignAllStringFields(Type type, Func<string, string>? func = null)
	{
		foreach (var field in type.GetFields(AccessTools.allDeclared))
		{
			if (field.FieldType != typeof(string))
				continue;

			var text = field.GetValue(null) as string ?? field.Name;
			field.SetValue(null, func != null ? func.Invoke(text) : text);
		}
	}
}
#pragma warning restore CS8618