/////////////////////////////////////////////////////////////////////////////////////
//
// Lepracaun - Varies of .NET Synchronization Context.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System.Threading.Tasks;

namespace Lepracaun;

/// <summary>
/// Leprecaun central synchronization context.
/// </summary>
public interface IActiveSynchronizationContext :
    ISynchronizationContext
{
    /// <summary>
    /// Execute message queue on background.
    /// </summary>
    void Start();
}
