/////////////////////////////////////////////////////////////////////////////////////
//
// Lepracaun - Varies of .NET Synchronization Context.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Lepracaun.Internal;
using System.Collections.Generic;
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
    private readonly Queue<ContinuationInformation> queue = new();

    /// <summary>
    /// Flag for continuation availability.
    /// </summary>
    private readonly ManualResetEventSlim available = new();

    /// <summary>
    /// Flag for finalized.
    /// </summary>
    private bool completeAdding;

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
        var entry = new ContinuationInformation
            { Continuation = continuation, State = state };
        lock (this.queue)
        {
            this.queue.Enqueue(entry);
            if (this.queue.Count == 1)
            {
                this.available.Set();
            }
        }
    }

    protected override sealed void OnRun(
        int targetThreadId)
    {
        // Run queue consumer.
        while (true)
        {
            this.available.Wait();

            ContinuationInformation entry;
            lock (this.queue)
            {
                if (this.queue.Count == 0)
                {
                    this.available.Reset();
                    if (this.completeAdding)
                    {
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
                entry = this.queue.Dequeue();
            }

            // Invoke continuation.
            this.OnInvoke(
                entry.Continuation,
                entry.State);
        }
    }

    protected override sealed void OnShutdown(
        int targetThreadId)
    {
        this.completeAdding = true;
        this.available.Set();
    }
}
