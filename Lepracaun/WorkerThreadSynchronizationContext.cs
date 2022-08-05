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
public sealed class WorkerThreadedSynchronizationContext :
    ManagedThreadSynchronizationContext, IActiveSynchronizationContext
{
    private readonly Thread thread;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkerThreadedSynchronizationContext()
    {
        this.thread = new(() => base.Run(null!));
        this.thread.IsBackground = true;
        base.SetTargetThreadId(this.thread.ManagedThreadId);
    }

    /// <summary>
    /// Copy this context.
    /// </summary>
    /// <returns>Copied context.</returns>
    public override SynchronizationContext CreateCopy() =>
       new WorkerThreadedSynchronizationContext();

    /// <summary>
    /// Execute message queue on background.
    /// </summary>
    public void Start() =>
        this.thread.Start();

    /// <summary>
    /// Execute message queue.
    /// </summary>
    public void Run() =>
        base.Run(null!);

    /// <summary>
    /// Execute message queue.
    /// </summary>
    /// <param name="task">Completion awaiting task</param>
    public new void Run(Task task) =>
        base.Run(task);
}
