using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace MiraQt.DBus;

/// <summary>
/// org.gnome.NetworkDisplays.Manager interface.
/// Maps directly to:
/// gnome-network-displays/src/org.gnome.NetworkDisplays.Manager.xml
/// </summary>
[DBusInterface("org.gnome.NetworkDisplays.Manager")]
public interface INetworkDisplaysManager : IDBusObject
{
    Task<string> StartStreamAsync(string sinkUuid);
    Task StopStreamAsync(string streamUnitName);

    Task<object> GetAsync(string prop);
    Task<NetworkDisplaysManagerProperties> GetAllAsync();
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(System.Action<PropertyChanges> handler);
}

[Dictionary]
public class NetworkDisplaysManagerProperties
{
    /// <summary>
    /// aa{sv} - array of dicts. Each dict has:
    ///   display-name : string
    ///   priority     : uint32
    ///   state        : uint32 (NdSinkState)
    ///   protocol     : uint32
    /// The key of the outer dict (which Tmds.DBus exposes as IDictionary&lt;string, object&gt;)
    /// is the sink UUID — this is what we feed into StartStream.
    /// </summary>
    public IDictionary<string, object>[] Displays { get; set; } = System.Array.Empty<IDictionary<string, object>>();
}
