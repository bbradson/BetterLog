// Copyright (c) 2022 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace BetterLog;

[DefOf]
public static class DefOfs
{
	public static MessageTypeDef LogMessageMessage;
	public static MessageTypeDef LogWarningMessage;
	public static MessageTypeDef LogErrorMessage;

#pragma warning disable CS8618
	static DefOfs() => DefOfHelper.EnsureInitializedInCtor(typeof(DefOfs));
#pragma warning restore CS8618
}