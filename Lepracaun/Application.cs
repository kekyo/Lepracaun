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
    private readonly ThreadBoundSynchronizationContextBase context;

    public Application() :
        this(new ThreadBoundSynchronizationContext())
    {
    }

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
    /// Get current Application instance.
    /// </summary>
    public static Application? Current { get; private set; }

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
    public void Run(Task task) =>
        this.context.Run(task);

    /// <summary>
    /// Request shutdown.
    /// </summary>
    public void Shutdown() =>
        this.context.Shutdown();
}

/// <summary>
/// Pseudo Application class.
/// </summary>
public sealed class Application<TSynchronizationContext> : Application
    where TSynchronizationContext : ThreadBoundSynchronizationContextBase, new()
{
    public Application() :
        base(new TSynchronizationContext())
    {
    }
}
