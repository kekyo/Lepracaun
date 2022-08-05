/////////////////////////////////////////////////////////////////////////////////////
//
// Lepracaun - Varies of .NET Synchronization Context.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

using static NUnit.Framework.Assert;

namespace Lepracaun;

[TestFixture]
public sealed class ThreadBoundTests
{
    private static async Task TestBodyAsync(int threadId)
    {
        AreEqual(threadId, Application.Current.BoundIdentity);

        await Task.Delay(100);

        AreEqual(threadId, Application.Current.BoundIdentity);
    }

    [Test]
    public void ThreadBoundTest()
    {
        IsNull(SynchronizationContext.Current);

        var app = new Application();

        app.Run(TestBodyAsync(app.BoundIdentity));
    }

    [Test]
    public void Win32ThreadBoundTest()
    {
        IsNull(SynchronizationContext.Current);

        var app = new Application(
            new Win32MessagingSynchronizationContext());

        app.Run(TestBodyAsync(app.BoundIdentity));
    }
}
