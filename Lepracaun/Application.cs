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
/// Pseudo Application class.
/// </summary>
public class Application : IDisposable
{
    private static readonly object locker = new();
    private static Application? current;

    /// <summary>
    /// Get current Application instance.
    /// </summary>
    public static Application Current
    {
        get
        {
            lock (locker)
            {
                if (current == null)
                {
                    current = new Application();
                }
            }
            return current;
        }
        private set
        {
            lock (locker)
            {
                if (!object.ReferenceEquals(value, current))
                {
                    if (current != null)
                    {
                        current.Dispose();
                    }
                    current = value;
                }
            }
        }
    }

    private readonly ThreadBoundSynchronizationContextBase context;

    /// <summary>
    /// Constructor.
    /// </summary>
    public Application() :
        this(new ThreadBoundSynchronizationContext())
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="context">Applicated synchronization context</param>
    public Application(ThreadBoundSynchronizationContextBase context)
    {
        this.context = context;
        this.context.UnhandledException += this.OnUnhandledException!;

        Current = this;
        SynchronizationContext.SetSynchronizationContext(this.context);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) =>
        this.UnhandledException?.Invoke(this, e);

    /// <summary>
    /// Dispose.
    /// </summary>
    public void Dispose()
    {
        this.context.Shutdown();
        this.context.UnhandledException -= this.OnUnhandledException!;
    }

    /// <summary>
    /// Get bound thread identity.
    /// </summary>
    public int BoundIdentity =>
        this.context.BoundIdentity;

    /// <summary>
    /// Occurred unhandled exception event.
    /// </summary>
    public EventHandler<UnhandledExceptionEventArgs>? UnhandledException;

    /// <summary>
    /// Run the application.
    /// </summary>
    public void Run() =>
        this.context.Run();

    /// <summary>
    /// Run the application with first time task.
    /// </summary>
    /// <param name="mainTask">Bound main task</param>
    public void Run(Task mainTask) =>
        this.context.Run(mainTask);

    /// <summary>
    /// Run the application with first time task.
    /// </summary>
    /// <param name="mainAction">Bound main action</param>
    public void Run(Func<Task> mainAction) =>
        this.context.Run(mainAction());

    /// <summary>
    /// Request shutdown.
    /// </summary>
    public void Shutdown() =>
        this.context.Shutdown();

    /// <summary>
    /// Synchronized invoking a delegate on the application context.
    /// </summary>
    /// <param name="action">Action delegate</param>
    public void Invoke(Action action) =>
        this.context.Send(_ => action(), null);

    /// <summary>
    /// Synchronized invoking a delegate on the application context.
    /// </summary>
    /// <typeparam name="TResult">Return value type</typeparam>
    /// <param name="action">Action delegate</param>
    /// <returns>Return value</returns>
    public TResult Invoke<TResult>(Func<TResult> action)
    {
        TResult result = default!;
        this.context.Send(_ => result = action(), null);
        return result;
    }

    /// <summary>
    /// Asynchronized invoking a delegate on the application context.
    /// </summary>
    /// <param name="action">Action delegate</param>
    public void BeginInvoke(Action action) =>
        this.context.Post(_ => action(), null);

    /// <summary>
    /// Asynchronized invoking a delegate on the application context.
    /// </summary>
    /// <param name="action">Action delegate</param>
    public Task InvokeAsync(Action action)
    {
        var tcs = new TaskCompletionSource<int>();
        this.context.Post(_ =>
        {
            try
            {
                action();
                tcs.TrySetResult(0);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, null);
        return tcs.Task;
    }

    /// <summary>
    /// Asynchronized invoking a delegate on the application context.
    /// </summary>
    /// <typeparam name="TResult">Return value type</typeparam>
    /// <param name="action">Action delegate</param>
    /// <returns>Return value</returns>
    public Task<TResult> InvokeAsync<TResult>(Func<TResult> action)
    {
        var tcs = new TaskCompletionSource<TResult>();
        this.context.Post(_ =>
        {
            try
            {
                tcs.TrySetResult(action());
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, null);
        return tcs.Task;
    }

    /// <summary>
    /// Asynchronized invoking a delegate on the application context.
    /// </summary>
    /// <param name="action">Asynchronous action delegate</param>
    public Task InvokeAsync(Func<Task> action)
    {
        var tcs = new TaskCompletionSource<int>();
        this.context.Post(async _ =>
        {
            try
            {
                await action().ConfigureAwait(false);
                tcs.TrySetResult(0);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, null);
        return tcs.Task;
    }

    /// <summary>
    /// Asynchronized invoking a delegate on the application context.
    /// </summary>
    /// <typeparam name="TResult">Return value type</typeparam>
    /// <param name="action">Asynchronous action delegate</param>
    /// <returns>Return value</returns>
    public Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> action)
    {
        var tcs = new TaskCompletionSource<TResult>();
        this.context.Post(async _ =>
        {
            try
            {
                tcs.TrySetResult(await action().ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, null);
        return tcs.Task;
    }
}

/// <summary>
/// Pseudo Application class.
/// </summary>
/// <typeparam name="TSynchronizationContext">Applicated synchronization context type</typeparam>
public sealed class Application<TSynchronizationContext> : Application
    where TSynchronizationContext : ThreadBoundSynchronizationContextBase, new()
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public Application() :
        base(new TSynchronizationContext())
    {
    }
}
