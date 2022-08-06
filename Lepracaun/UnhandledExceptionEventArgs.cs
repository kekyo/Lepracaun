/////////////////////////////////////////////////////////////////////////////////////
//
// Lepracaun - Varies of .NET Synchronization Context.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;

namespace Lepracaun;

public sealed class UnhandledExceptionEventArgs : EventArgs
{
    public readonly Exception Exception;
    public bool Handled;

    public UnhandledExceptionEventArgs(Exception ex) =>
        this.Exception = ex;
}
