/////////////////////////////////////////////////////////////////////////////////////
//
// Lepracaun - Varies of .NET Synchronization Context.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Lepracaun;

/// <summary>
/// Custom synchronization context implementation using BlockingCollection.
/// </summary>
public sealed class ThreadBoundSynchronizationContext :
    ThreadBoundSynchronizationContextBase
{
    private struct ContinuationInformation
    {
        public SendOrPostCallback Continuation;
        public object? State;
    }

    /// <summary>
    /// Continuation queue.
    /// </summary>
    private readonly BlockingCollection<ContinuationInformation> queue = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    public ThreadBoundSynchronizationContext()
    {
    }

    private ThreadBoundSynchronizationContext(int targetThreadId) :
        base(targetThreadId)
    {
    }

    protected override int GetCurrentThreadId() =>
        Thread.CurrentThread.ManagedThreadId;

    protected override SynchronizationContext OnCreateCopy(int targetThreadId) =>
        new ThreadBoundSynchronizationContext(targetThreadId);

    protected override void OnPost(
        int targetThreadId, SendOrPostCallback continuation, object? state)
    {
        // Add continuation information into queue.
        this.queue.Add(new ContinuationInformation { Continuation = continuation, State = state });
    }

    protected override void OnRun(
        int targetThreadId, Func<Exception, bool> onUnhandledException)
    {
        // Run queue consumer.
        foreach (var continuationInformation in this.queue.GetConsumingEnumerable())
        {
            try
            {
                // Invoke continuation.
                continuationInformation.Continuation(continuationInformation.State);
            }
            catch (Exception ex)
            {
                if (!onUnhandledException(ex))
                {
                    throw;
                }
            }
        }
    }

    protected override void OnShutdown(
        int targetThreadId) =>
        this.queue.CompleteAdding();
}
