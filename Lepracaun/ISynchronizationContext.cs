/////////////////////////////////////////////////////////////////////////////////////
//
// Lepracaun - Varies of .NET Synchronization Context.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;

namespace Lepracaun;

/// <summary>
/// Leprecaun central synchronization context.
/// </summary>
public interface ISynchronizationContext
{
    /// <summary>
    /// Get bound thread identity.
    /// </summary>
    int BoundIdentity { get; }

    /// <summary>
    /// Occurred unhandled exception event.
    /// </summary>
    event EventHandler<UnhandledExceptionEventArgs>? UnhandledException;

    /// <summary>
    /// Check current context is bound this.
    /// </summary>
    /// <returns>True if bound this.</returns>
    bool CheckAccess();

    /// <summary>
    /// Send continuation into synchronization context.
    /// </summary>
    /// <param name="continuation">Continuation callback delegate.</param>
    /// <param name="state">Continuation argument.</param>
    void Send(SendOrPostCallback continuation, object? state);

    /// <summary>
    /// Post continuation into synchronization context.
    /// </summary>
    /// <param name="continuation">Continuation callback delegate.</param>
    /// <param name="state">Continuation argument.</param>
    void Post(SendOrPostCallback continuation, object? state);
}
