/////////////////////////////////////////////////////////////////////////////////////
//
// Lepracaun - Varies of .NET Synchronization Context.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Lepracaun;

public sealed class UnhandledExceptionEventArgs : EventArgs
{
    public readonly Exception Exception;
    public bool Handled;

    public UnhandledExceptionEventArgs(Exception ex) =>
        this.Exception = ex;
}

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
        this.targetThreadId;

    /// <summary>
    /// Occurred unhandled exception event.
    /// </summary>
    public EventHandler<UnhandledExceptionEventArgs>? UnhandledException;

    /// <summary>
    /// Check current context is bound this.
    /// </summary>
    /// <returns>True if bound this.</returns>
    public bool CheckAccess() =>
        this.GetCurrentThreadId() == this.targetThreadId;

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
    /// <param name="onUnhandledException">Occurred unhandled exception handler</param>
    protected abstract void OnRun(
        int targetThreadId, Func<Exception, bool> onUnhandledException);

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
    public override void Send(SendOrPostCallback continuation, object? state)
    {
        // If current thread id is target thread id:
        var currentThreadId = this.GetCurrentThreadId();
        if (currentThreadId == this.targetThreadId)
        {
            // Invoke continuation on current thread.
            continuation(state);

            return;
        }

        using var mre = new ManualResetEventSlim(false);

        // Marshal to.
        this.OnPost(this.targetThreadId, state =>
        {
            try
            {
                continuation(state);
            }
            finally
            {
                mre.Set();
            }
        }, state);

        // Yes, this can easily cause a deadlock on UI threads:
        // * But as long as the Send() method requires blocking, we have no choice.
        // * If this context is bound to the UI thread itself,
        //   it is no problem, as it will be bypassed by the if block above.
        mre.Wait();
    }

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

        this.OnRun(
            this.targetThreadId,
            ex =>
            {
                var e = new UnhandledExceptionEventArgs(ex);
                try
                {
                    this.UnhandledException?.Invoke(this, e);
                }
                catch (Exception ex2)
                {
                    Trace.WriteLine(ex2.ToString());
                }
                return e.Handled;
            });
    }

    /// <summary>
    /// Shutdown running context.
    /// </summary>
    public void Shutdown() =>
        this.OnShutdown(this.targetThreadId);
}
