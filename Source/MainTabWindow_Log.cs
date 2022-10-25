// Copyright (c) 2022 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace BetterLog;
public class MainTabWindow_Log : MainTabWindow
{
	public override Vector2 InitialSize => Vector2.zero;
	public override void PostOpen()
	{
		Close(false);
		Find.UIRoot.debugWindowOpener.ToggleLogWindow();
	}

	public override void DoWindowContents(Rect inRect) { }
}