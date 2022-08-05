/////////////////////////////////////////////////////////////////////////////////////
//
// Lepracaun - Varies of .NET Synchronization Context.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Lepracaun.Internal;
using System.Collections.Concurrent;
using System.Threading;

namespace Lepracaun;

/// <summary>
/// Custom synchronization context implementation using managed thread.
/// </summary>
public abstract class ManagedThreadSynchronizationContext :
    ThreadBoundSynchronizationContext
{
    /// <summary>
    /// Continuation queue.
    /// </summary>
    private readonly BlockingCollection<ContinuationInformation> queue = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    protected ManagedThreadSynchronizationContext()
    {
    }

    protected override sealed int GetCurrentThreadId() =>
        Thread.CurrentThread.ManagedThreadId;

    protected override sealed void OnPost(
        int targetThreadId, SendOrPostCallback continuation, object? state)
    {
        // Add continuation information into queue.
        this.queue.Add(new ContinuationInformation
            { Continuation = continuation, State = state });
    }

    protected override sealed void OnRun(
        int targetThreadId)
    {
        // Run queue consumer.
        foreach (var continuationInformation in
            this.queue.GetConsumingEnumerable())
        {
            // Invoke continuation.
            this.OnInvoke(
                continuationInformation.Continuation,
                continuationInformation.State);
        }
    }

    protected override sealed void OnShutdown(
        int targetThreadId) =>
        this.queue.CompleteAdding();
}
