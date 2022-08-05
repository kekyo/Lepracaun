/////////////////////////////////////////////////////////////////////////////////////
//
// Lepracaun - Varies of .NET Synchronization Context.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Lepracaun.Internal;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Lepracaun;

/// <summary>
/// Custom synchronization context implementation using Windows message queue (Win32)
/// </summary>
public sealed class Win32MessagingSynchronizationContext :
    ThreadBoundSynchronizationContext, IPassiveSynchronizationContext
{
    /// <summary>
    /// Internal uses Windows message number (Win32).
    /// </summary>
    private static readonly int WM_SC;

    /// <summary>
    /// Type initializer.
    /// </summary>
    static Win32MessagingSynchronizationContext() =>
        // Allocate Windows message number.
        // Using guid because type loaded into multiple AppDomain, type initializer called multiple.
        WM_SC = Win32NativeMethods.RegisterWindowMessageW(
            "Win32MessagingSynchronizationContext_" + Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Constructor.
    /// </summary>
    public Win32MessagingSynchronizationContext() =>
        this.SetTargetThreadId(Win32NativeMethods.GetCurrentThreadId());

    protected override int GetCurrentThreadId() =>
        Win32NativeMethods.GetCurrentThreadId();

    /// <summary>
    /// Copy this context.
    /// </summary>
    /// <returns>Copied context.</returns>
    public override SynchronizationContext CreateCopy() =>
       new Win32MessagingSynchronizationContext();

    protected override void OnPost(
        int targetThreadId, SendOrPostCallback continuation, object? state)
    {
        // Get continuation and state cookie.
        // Because these values turn to unmanaged value (IntPtr),
        // so GC cannot track instances and maybe collects...
        var continuationCookie = GCHandle.ToIntPtr(GCHandle.Alloc(continuation));
        var stateCookie = GCHandle.ToIntPtr(GCHandle.Alloc(state));

        // Post continuation information into UI queue.
        Win32NativeMethods.PostThreadMessage(
            targetThreadId, WM_SC, continuationCookie, stateCookie);
    }

    protected override void OnRun(
        int targetThreadId)
    {
        // Run message loop (very legacy knowledge...)
        while (true)
        {
            // Get front of queue (or waiting).
            Win32NativeMethods.MSG msg;
            var result = Win32NativeMethods.GetMessage(out msg, IntPtr.Zero, 0, 0);

            // If message number is WM_QUIT (Cause PostQuitMessage API):
            if (result == 0)
            {
                // Exit.
                break;
            }

            // If cause error:
            if (result == -1)
            {
                // Throw.
                var hr = Marshal.GetHRForLastWin32Error();
                Marshal.ThrowExceptionForHR(hr);
            }

            // If message is WM_SC:
            if (msg.msg == WM_SC)
            {
                // Retreive GCHandles from cookies.
                var continuationHandle = GCHandle.FromIntPtr(msg.wParam);
                var stateHandle = GCHandle.FromIntPtr(msg.lParam);

                // Retreive continuation and state.
                var continuation = (SendOrPostCallback)continuationHandle.Target!;
                var state = stateHandle.Target;

                // Release handle (Recollectable by GC)
                continuationHandle.Free();
                stateHandle.Free();

                // Invoke continuation.
                this.OnInvoke(continuation, state);

                // Consumed message.
                continue;
            }

            // Translate accelerator (require UI stability)
            Win32NativeMethods.TranslateMessage(ref msg);

            // Send to assigned window procedure.
            Win32NativeMethods.DispatchMessage(ref msg);
        }
    }

    protected override void OnShutdown(
        int targetThreadId) =>
        Win32NativeMethods.PostThreadMessage(
            targetThreadId, Win32NativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);

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
