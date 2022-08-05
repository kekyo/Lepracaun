﻿/////////////////////////////////////////////////////////////////////////////////////
//
// Lepracaun - Varies of .NET Synchronization Context.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

using static NUnit.Framework.Assert;

namespace Lepracaun;

[TestFixture]
public sealed class InvokingTests
{
    private static async Task TestBodyAsync()
    {
        await Application.Current.InvokeAsync(() =>
        {
            Thread.Sleep(100);
        });

        var result1 = await Application.Current.InvokeAsync(() =>
        {
            Thread.Sleep(100);
            return 123;
        });

        AreEqual(123, result1);

        await Application.Current.InvokeAsync(async () =>
        {
            await Task.Delay(100);
        });

        var result2 = await Application.Current.InvokeAsync(async () =>
        {
            await Task.Delay(100);
            return 456;
        });

        AreEqual(456, result2);
    }

    [Test]
    public void RunTest1()
    {
        IsNull(SynchronizationContext.Current);

        var app = new Application();

        app.Run(TestBodyAsync());
    }

    [Test]
    public void RunTest2()
    {
        IsNull(SynchronizationContext.Current);

        var app = new Application();

        app.Run(() => TestBodyAsync());
    }

    [Test]
    public void RunManyInvoking()
    {
        IsNull(SynchronizationContext.Current);

        var app = new Application();
        var id = app.BoundIdentity;

        app.Run(async () =>
        {
            for (var index = 0; index < 1000000; index++)
            {
                await Task.Yield();

                AreEqual(id, Thread.CurrentThread.ManagedThreadId);
            }
        });
    }
}
