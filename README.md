# Lepracaun

![Lepracaun](Images/Lepracaun.100.png)

Lepracaun - Varies of .NET Synchronization Context.

[![Project Status: WIP – Initial development is in progress, but there has not yet been a stable, usable release suitable for the public.](https://www.repostatus.org/badges/latest/wip.svg)](https://www.repostatus.org/#wip)

## NuGet

| Package  | NuGet                                                                                                                |
|:---------|:---------------------------------------------------------------------------------------------------------------------|
| Lepracaun | [![NuGet Lepracaun](https://img.shields.io/nuget/v/Lepracaun.svg?style=flat)](https://www.nuget.org/packages/Lepracaun) |

## CI

| main                                                                                                                                                                 | develop                                                                                                                                                                       |
|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [![Lepracaun CI build (main)](https://github.com/kekyo/Lepracaun/workflows/.NET/badge.svg?branch=main)](https://github.com/kekyo/Lepracaun/actions?query=branch%3Amain) | [![Lepracaun CI build (develop)](https://github.com/kekyo/Lepracaun/workflows/.NET/badge.svg?branch=develop)](https://github.com/kekyo/Lepracaun/actions?query=branch%3Adevelop) |

----

## What is this?

Lepracaun is a library that collection of .NET Synchronization Context. It is a successor of [SynchContextSample library](https://github.com/kekyo/SynchContextSample).

|Class|Detail|
|:----|:----|
|`SingleThreadedSynchronizationContext`|That constrains the execution context to a single thread using simple queue.|
|`Win32MessagingSynchronizationContext`|That constrains the execution context to a single thread using the Win32 message pumps.|
|`WorkerThreadSynchronizationContext`|Create a worker thread and running on it.|
|TODO:|TODO:|
|`Application`|Pseudo (WPF/WinForms like) the Application class. Will use `SingleThreadedSynchronizationContext` defaulted.|

### Operating Environment

The following platforms are supported by the package.

* NET 7, 6, 5
* NET Core 3.1, 3.0, 2.2, 2.1, 2.0
* NET Standard 2.1, 2.0, 1.6, 1.3
* NET Framework 4.8, 4.6.1, 4.5, 4.0, 3.5

----

## Basic usage

### Single threaded

```csharp
// Allocate and assigned:
using var sc = new SingleThreadedSynchronizationContext();
SynchronizationContext.SetSynchronizationContext(sc);

// Use it directly (Post is asynchronously, so will delay execution.)
var origin = Thread.CurrentThread.ManagedThreadId;
sc.Post(_ =>
{
    var current = Thread.CurrentThread.ManagedThreadId;
    Console.WriteLine($"{origin} ==> {current}");
}, null);

// Run it:
sc.Run();
```

### Worker thread

```csharp
// Allocate and assigned:
using var sc = new WorkerThreadSynchronizationContext();
SynchronizationContext.SetSynchronizationContext(sc);

// Use it directly (Post is asynchronously, so will delay execution.)
var origin = Thread.CurrentThread.ManagedThreadId;
sc.Post(_ =>
{
    var current = Thread.CurrentThread.ManagedThreadId;
    Console.WriteLine($"{origin} ==> {current}");
}, null);

// Run it (Worker thread will run background.)
sc.Run();

// (Wait until consume.)
Thread.Sleep(1000);
```

----

## Realistic samples

### Main thread bound asynchronous operation

```csharp
public static void Main(string[] args)
{
    using var app = new Application();

    //using var app = new Application(
    //    new SingleThreadedSynchronizationContext());

    app.Run(async () =>
    {
        // Your asynchronous operations
        // into only main thread (Will not use any worker threads)

        using var rs = new FileStream(...);
        using var ws = new FileStream(...);

        var buffer = new byte[1024];
        var read = await rs.ReadAsync(buffer, 0, buffer.Length);

        // (Rebound to main thread)
        await ws.WriteAsync(buffer, 0, read);

        // (Rebound to main thread)
        await ws.FlushAsync();
   
        // (Rebound to main thread)

        // ...
    });    
}
```

### Bound temporary Win32 UI thread on any arbitrary thread context

```csharp
public void MarshalInToUIThread()
{
    using var app = new Application(
        new Win32MessagingSynchronizationContext());

    // Run on worker thread.
    Task.Run(() =>
    {
        // Some longer operations...

        // Manipulate Win32 UI from worker thread context.
        app.BeginInvoke(() => Win32NativeMethods.ShowWindow(this.window));
    });

    app.Run();
}
```

----

## License

Apache-v2.

----

## History

* 0.4.0:
  * Supported .NET 7.0.
  * Fixed unmarking event field on `Application` class.
  * Fixed did not handle exception when runner bootstrap.
* 0.3.0:
  * Added feature of worker thread bound.
* 0.2.0:
  * Added CheckAccess method.
  * Fixed returns invalid identity.
* 0.1.0:
  * Initial stable release.
  * Added pseudo `Application` class.
  * Fixed a problem that `SynchContext.Send()` completes even though the target continuation is not completed.
* 0.0.2:
  * Initial release.
