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
/// Custom synchronization context implementation using BlockingCollection.
/// </summary>
public sealed class SingleThreadedSynchronizationContext :
    ThreadBoundSynchronizationContext
{
    /// <summary>
    /// Continuation queue.
    /// </summary>
    private readonly BlockingCollection<ContinuationInformation> queue = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    public SingleThreadedSynchronizationContext() :
        base(Thread.CurrentThread.ManagedThreadId)
    {
    }

    private SingleThreadedSynchronizationContext(int targetThreadId) :
        base(targetThreadId)
    {
    }

    protected override int GetCurrentThreadId() =>
        Thread.CurrentThread.ManagedThreadId;

    protected override SynchronizationContext OnCreateCopy(int targetThreadId) =>
        new SingleThreadedSynchronizationContext(targetThreadId);

    protected override void OnPost(
        int targetThreadId, SendOrPostCallback continuation, object? state)
    {
        // Add continuation information into queue.
        this.queue.Add(new ContinuationInformation { Continuation = continuation, State = state });
    }

    protected override void OnRun(
        int targetThreadId)
    {
        // Run queue consumer.
        foreach (var continuationInformation in this.queue.GetConsumingEnumerable())
        {
            // Invoke continuation.
            this.OnInvoke(
                continuationInformation.Continuation,
                continuationInformation.State);
        }
    }

    protected override void OnShutdown(
        int targetThreadId) =>
        this.queue.CompleteAdding();
}
