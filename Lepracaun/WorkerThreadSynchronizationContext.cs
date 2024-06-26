/////////////////////////////////////////////////////////////////////////////////////
//
// Lepracaun - Varies of .NET Synchronization Context.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System.Threading;
using System.Threading.Tasks;

namespace Lepracaun;

/// <summary>
/// Custom synchronization context implementation using separated worker thread.
/// </summary>
public sealed class WorkerThreadSynchronizationContext :
    ManagedThreadSynchronizationContext
{
    private readonly Thread thread;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkerThreadSynchronizationContext()
    {
        this.thread = new(() =>
        {
            SetSynchronizationContext(this);
            base.Run(null!);
        });
        this.thread.IsBackground = true;
        base.SetTargetThreadId(this.thread.ManagedThreadId);
    }

    /// <summary>
    /// Copy this context.
    /// </summary>
    /// <returns>Copied context.</returns>
    public override SynchronizationContext CreateCopy() =>
       new WorkerThreadSynchronizationContext();

    /// <summary>
    /// Execute message queue.
    /// </summary>
    /// <param name="task">Completion awaiting task</param>
    public override void Run(Task task)
    {
        this.HookTaskFinalizer(task);
        this.thread.Start();
    }

    /// <summary>
    /// Execute message queue on background.
    /// </summary>
    public override void Run() =>
        this.thread.Start();
}
