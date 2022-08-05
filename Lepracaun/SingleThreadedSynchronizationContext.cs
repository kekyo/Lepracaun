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
/// Custom synchronization context implementation running on current thread.
/// </summary>
public sealed class SingleThreadedSynchronizationContext :
    ManagedThreadSynchronizationContext, ISynchronizationContext
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public SingleThreadedSynchronizationContext() =>
        this.SetTargetThreadId(Thread.CurrentThread.ManagedThreadId);

    /// <summary>
    /// Copy this context.
    /// </summary>
    /// <returns>Copied context.</returns>
    public override SynchronizationContext CreateCopy() =>
       new SingleThreadedSynchronizationContext();

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
