// Copyright (c) 2022 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using JetBrains.Annotations;
using Mono.Cecil;
using Prepatcher;

namespace BetterLog;

public static class FreePatchEntry
{
	/// <summary>
	/// This fixes prepatcher compatibility
	/// </summary>
	[FreePatch]
	[UsedImplicitly]
	public static void Start(ModuleDefinition module)
		=> Application.logMessageReceivedThreaded -= Log.Notify_MessageReceivedThreadedInternal;
}