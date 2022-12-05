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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

#if !NET35 && !NET40
using System.Runtime.ExceptionServices;
#endif

namespace Lepracaun;

/// <summary>
/// Leprecaun central synchronization context.
/// </summary>
public abstract class ThreadBoundSynchronizationContext :
    SynchronizationContext
{
    /// <summary>
    /// This synchronization context bound thread id.
    /// </summary>
    private int boundThreadId;

    /// <summary>
    /// Number of recursive posts.
    /// </summary>
    private int recursiveCount = 0;

    /// <summary>
    /// Constructor.
    /// </summary>
    protected ThreadBoundSynchronizationContext() =>
        this.boundThreadId = -1;

    protected void SetTargetThreadId(int targetThreadId)
    {
        if (Interlocked.CompareExchange(
            ref this.boundThreadId, targetThreadId, -1) != -1)
        {
            throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Get bound thread identity.
    /// </summary>
    public int BoundIdentity =>
        this.boundThreadId;

    /// <summary>
    /// Occurred unhandled exception event.
    /// </summary>
    public event EventHandler<UnhandledExceptionEventArgs>? UnhandledException;

    /// <summary>
    /// Check current context is bound this.
    /// </summary>
    /// <returns>True if bound this.</returns>
    public bool CheckAccess() =>
        this.GetCurrentThreadId() == this.boundThreadId;

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

    private bool InvokeUnhandledException(Exception ex)
    {
        UnhandledExceptionEventArgs e;
        if (ex is AggregateException aex &&
            aex.InnerExceptions.Count == 1)
        {
            e = new(aex.InnerExceptions[0]);
        }
        else if (ex is TargetInvocationException tex &&
            tex.InnerException != null)
        {
            e = new(tex.InnerException);
        }
        else
        {
            e = new(ex);
        }

        try
        {
            this.UnhandledException?.Invoke(this, e);
        }
        catch (Exception ex2)
        {
            Trace.WriteLine(ex2.ToString());
        }

        return e.Handled;
    }

    protected void OnInvoke(SendOrPostCallback continuation, object? state)
    {
        try
        {
            continuation(state);
        }
        catch (Exception ex)
        {
            if (!this.InvokeUnhandledException(ex))
            {
                throw;
            }
        }
    }

    /// <summary>
    /// Send continuation into synchronization context.
    /// </summary>
    /// <param name="continuation">Continuation callback delegate.</param>
    /// <param name="state">Continuation argument.</param>
    public override void Send(SendOrPostCallback continuation, object? state)
    {
        // If current thread id is target thread id:
        var currentThreadId = this.GetCurrentThreadId();
        if (currentThreadId == this.boundThreadId)
        {
            // Invoke continuation on current thread.
            this.OnInvoke(continuation, state);
            return;
        }

        using var mre = new ManualResetEventSlim(false);
#if NET35 || NET40
        Exception? edi = null;
#else
        ExceptionDispatchInfo? edi = null;
#endif

        // Marshal to.
        this.OnPost(this.boundThreadId, _ =>
        {
            try
            {
                this.OnInvoke(continuation, state);
            }
            catch (Exception ex)
            {
#if NET35 || NET40
                edi = ex;
#else
                edi = ExceptionDispatchInfo.Capture(ex);
#endif
            }
            finally
            {
                mre.Set();
            }
        }, null);

        // Yes, this can easily cause a deadlock on UI threads:
        // * But as long as the Send() method requires blocking, we have no choice.
        // * If this context is bound to the UI thread itself,
        //   it is no problem, as it will be bypassed by the if block above.
        mre.Wait();

        if (edi != null)
        {
#if NET35 || NET40
            throw new TargetInvocationException(edi);
#else
            edi.Throw();
#endif
        }
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
        if (currentThreadId == this.boundThreadId)
        {
            // HACK: If current thread is already target thread, invoke continuation directly.
            //   But if continuation has invokeing Post/Send recursive, cause stack overflow.
            //   We can fix this problem by simple solution: Continuation invoke every post into queue,
            //   but performance will be lost.
            //   This counter uses post for scattering (each 50 times).
            if (recursiveCount < 50)
            {
                recursiveCount++;
                try
                {
                    // Invoke continuation on current thread is better performance.
                    this.OnInvoke(continuation, state);
                }
                finally
                {
                    recursiveCount--;
                }
                return;
            }
        }

        this.OnPost(this.boundThreadId, continuation, state);
    }

    /// <summary>
    /// Execute message queue.
    /// </summary>
    /// <param name="task">Completion awaiting task</param>
    public virtual void Run(Task task)
    {
        // Run only target thread.
        var currentThreadId = this.GetCurrentThreadId();
        if (currentThreadId != this.boundThreadId)
        {
            throw new InvalidOperationException(
                $"Thread mismatch between created and running: Created={this.boundThreadId}, Running={currentThreadId}");
        }

        // Schedule task completion.
        task?.ContinueWith(t =>
        {
            if (t.IsCanceled)
            {
                this.InvokeUnhandledException(t.Exception!);
            }
            else if (t.IsFaulted)
            {
                this.InvokeUnhandledException(t.Exception!);
            }

            this.OnShutdown(this.boundThreadId);
        });

        this.OnRun(this.boundThreadId);
    }

    /// <summary>
    /// Execute message queue.
    /// </summary>
    public virtual void Run() =>
        this.Run(null!);

    /// <summary>
    /// Shutdown running context asynchronously.
    /// </summary>
    public void Shutdown() =>
        this.OnShutdown(this.boundThreadId);
}
