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

TODO:

### Main thread bound asynchronous operation

```csharp
private static async Task MainAsync(string[] args)
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
}

public static void Main(string[] args)
{
    using var app = new Application();

    app.Run(MainAsync(args));    
}
```

### Bound temporary Win32 UI thread on any arbitrary thread context

```csharp
public void MarshalInToUIThread()
{
    using var app = new Application(
        new Win32MessagingSynchronizationContext());

    Task.Run(() =>
    {
        // Some longer operations...

        // Manipulate UI from worker thread context.
        app.BeginInvoke(() => NativeMethods.ShowWindow(this.window));
    });

    app.Run();
}
```

----

## License

Apache-v2.

----

## History

TODO:
