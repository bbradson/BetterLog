// Copyright (c) 2022 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

global using System;
global using System.Collections.Generic;
global using System.Reflection;
#if !V1_3 && !V1_4
global using LudeonTK;
#endif
global using HarmonyLib;
global using RimWorld;
global using Verse;
global using UnityEngine;
using JetBrains.Annotations;

namespace BetterLog;
[PublicAPI]
public class BetterLog : Mod
{
	public BetterLog(ModContentPack content) : base(content)
	{
		Settings = GetSettings<Settings>();
		try
		{
			FishPatch.PatchAll();
		}
		catch (Exception ex)
		{
			Log.Error($"{FishPatch.MOD_NAME} encountered an exception while patching:\n{ex}");
		}
	}

	public override void DoSettingsWindowContents(Rect inRect) => Settings.DoSettingsWindowContents(inRect);

	public override string SettingsCategory() => FishPatch.MOD_NAME;

	public Settings Settings { get; private set; }
}