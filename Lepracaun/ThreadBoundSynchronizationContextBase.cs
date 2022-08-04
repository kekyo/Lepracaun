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
using System.Threading.Tasks;

namespace Lepracaun;

/// <summary>
/// Custom synchronization context implementation using Windows message queue (Win32)
/// </summary>
public abstract class ThreadBoundSynchronizationContextBase :
    SynchronizationContext
{
    /// <summary>
    /// This synchronization context bound thread id.
    /// </summary>
    private readonly int targetThreadId;

    /// <summary>
    /// Number of recursive posts.
    /// </summary>
    private int recursiveCount = 0;

    /// <summary>
    /// Constructor.
    /// </summary>
    protected ThreadBoundSynchronizationContextBase() =>
        this.targetThreadId = this.GetCurrentThreadId();

    /// <summary>
    /// Constructor.
    /// </summary>
    protected ThreadBoundSynchronizationContextBase(int targetThreadId) =>
        this.targetThreadId = targetThreadId;

    /// <summary>
    /// Get bound thread identity.
    /// </summary>
    public int BoundIdentity =>
        this.GetCurrentThreadId();

    /// <summary>
    /// Copy this context.
    /// </summary>
    /// <param name="targetThreadId">Target thread identity</param>
    /// <returns>Copied context.</returns>
    protected abstract SynchronizationContext OnCreateCopy(
        int targetThreadId);

    /// <summary>
    /// Copy this context.
    /// </summary>
    /// <returns>Copied context.</returns>
    public override SynchronizationContext CreateCopy() =>
        this.OnCreateCopy(this.targetThreadId);

    /// <summary>
    /// Get current thread identity.
    /// </summary>
    /// <returns>Thread identity</returns>
    protected abstract int GetCurrentThreadId();

    /// <summary>
    /// Post continuation into synchronization context.
    /// </summary>
    /// <param name="targetThreadId">Target thread identity.</param>
    /// <param name="continuation">Continuation callback delegate.</param>
    /// <param name="state">Continuation argument.</param>
    protected abstract void OnPost(
        int targetThreadId, SendOrPostCallback continuation, object? state);

    /// <summary>
    /// Execute message queue.
    /// </summary>
    /// <param name="targetThreadId">Target thread identity.</param>
    protected abstract void OnRun(
        int targetThreadId);

    /// <summary>
    /// Shutdown requested.
    /// </summary>
    /// <param name="targetThreadId">Target thread identity.</param>
    protected abstract void OnShutdown(
        int targetThreadId);

    /// <summary>
    /// Send continuation into synchronization context.
    /// </summary>
    /// <param name="continuation">Continuation callback delegate.</param>
    /// <param name="state">Continuation argument.</param>
    public override void Send(SendOrPostCallback continuation, object? state) =>
        this.Post(continuation, state);

    /// <summary>
    /// Post continuation into synchronization context.
    /// </summary>
    /// <param name="continuation">Continuation callback delegate.</param>
    /// <param name="state">Continuation argument.</param>
    public override void Post(SendOrPostCallback continuation, object? state)
    {
        // If current thread id is target thread id:
        var currentThreadId = this.GetCurrentThreadId();
        if (currentThreadId == this.targetThreadId)
        {
            // HACK: If current thread is already target thread, invoke continuation directly.
            //   But if continuation has invokeing Post/Send recursive, cause stack overflow.
            //   We can fix this problem by simple solution: Continuation invoke every post into queue,
            //   but performance will be lost.
            //   This counter uses post for scattering (each 50 times).
            if (recursiveCount < 50)
            {
                recursiveCount++;

                // Invoke continuation on current thread is better performance.
                continuation(state);

                recursiveCount--;
                return;
            }
        }

        this.OnPost(this.targetThreadId, continuation, state);
    }

    /// <summary>
    /// Execute message queue.
    /// </summary>
    public void Run() =>
        this.Run(null!);

    /// <summary>
    /// Execute message queue.
    /// </summary>
    /// <param name="task">Completion awaiting task</param>
    public void Run(Task task)
    {
        // Run only target thread.
        var currentThreadId = this.GetCurrentThreadId();
        if (currentThreadId != this.targetThreadId)
        {
            throw new InvalidOperationException(
                $"Thread mismatch between created and running: Created={this.targetThreadId}, Running={currentThreadId}");
        }

        // Schedule task completion.
        task?.ContinueWith(_ => this.OnShutdown(this.targetThreadId));

        this.OnRun(this.targetThreadId);
    }

    /// <summary>
    /// Shutdown running context.
    /// </summary>
    public void Shutdown() =>
        this.OnShutdown(this.targetThreadId);
}
