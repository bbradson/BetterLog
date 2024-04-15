// Copyright (c) 2022 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Diagnostics;
using System.Threading;
using JetBrains.Annotations;

namespace BetterLog;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class DebugActions
{
	[DebugOutput("Better Log", true, name = "Toggle Rainbow Errors")]
	public static void SpamErrors()
	{
		lock (_debugLock)
		{
			if (_errorSpamEnabled)
			{
				_errorSpamEnabled = false;
				Application.onBeforeRender -= _errorAction;
				return;
			}

			EnsureInitialized();
			_errorSpamEnabled = true;
			Application.onBeforeRender += _errorAction;
		}
	}

	private static void EnsureInitialized()
	{
		if (_random is not null)
			return;

		_random = new();
		_random.NextBytes(_colorBytes = new byte[3000]);
	}

	[DebugOutput("Better Log", true, name = "Log Single Error")]
	public static void LogSingleError()
	{
		lock (_debugLock)
		{
			EnsureInitialized();
			_errorAction();
		}
	}

	private static readonly object _debugLock = new();
	private static System.Random? _random;
	private static bool _errorSpamEnabled;
	private static byte[]? _colorBytes;
	private static volatile int _logErrorCount = -1;
	private static UnityEngine.Events.UnityAction _errorAction = () =>
	{
		var i = Interlocked.Increment(ref _logErrorCount);
		if (i >= 1000)
		{
			_random!.NextBytes(_colorBytes!);
			_logErrorCount = 0;
			i = 0;
		}

		Log.Error(Patches.Truncate_Log_Message_Message($"ERROR!\n{new StackTrace(true)}", 5).Colorize(
			new ColorInt(_colorBytes![i], _colorBytes[i + 1000], _colorBytes[i + 2000]).ToColor));
	};
}