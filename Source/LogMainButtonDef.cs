// Copyright (c) 2022 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace BetterLog;

[StaticConstructorOnStartup]
public static class LogMainButtonDef
{
	public static MainButtonDef DebugLog => _debugLog ??= DefDatabase<MainButtonDef>.GetNamed(nameof(DebugLog));
	private static MainButtonDef? _debugLog;

	static LogMainButtonDef()
	{
		Settings.ButtonDefLoaded = true;
		DebugLog.buttonVisible = Settings.ShowMainButton;
	}
}