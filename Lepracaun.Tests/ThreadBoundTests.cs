/////////////////////////////////////////////////////////////////////////////////////
//
// Lepracaun - Varies of .NET Synchronization Context.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Lepracaun.Internal;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

using static NUnit.Framework.Assert;

namespace Lepracaun;

[TestFixture]
public sealed class ThreadBoundTests
{
    private static async Task TestBodyAsync(int threadId, Func<int> getter)
    {
        AreEqual(threadId, getter());

        await Task.Delay(100);

        AreEqual(threadId, getter());

        await Task.Delay(100);

        AreEqual(threadId, getter());
    }

    [Test]
    public void ThreadBoundTest()
    {
        IsNull(SynchronizationContext.Current);

        using var app = new Application();

        app.Run(TestBodyAsync(app.BoundIdentity, () => Thread.CurrentThread.ManagedThreadId));
    }

    [Test]
    public void Win32ThreadBoundTest()
    {
        IsNull(SynchronizationContext.Current);

        using var app = new Application(
            new Win32MessagingSynchronizationContext());

        app.Run(TestBodyAsync(app.BoundIdentity, () => Win32NativeMethods.GetCurrentThreadId()));
    }

    private static async Task WorkerTestBodyAsync(int threadId)
    {
        AreNotEqual(threadId, Thread.CurrentThread.ManagedThreadId);

        await Task.Delay(100);

        AreEqual(threadId, Thread.CurrentThread.ManagedThreadId);

        await Task.Delay(100);

        AreEqual(threadId, Thread.CurrentThread.ManagedThreadId);
    }

    [Test]
    public async Task WorkerThreadBoundTest()
    {
        IsNull(SynchronizationContext.Current);

        using var app = new Application(
            new WorkerThreadSynchronizationContext());

        var task = WorkerTestBodyAsync(
            app.BoundIdentity);

        app.Run(task);

        await task;
    }
}
