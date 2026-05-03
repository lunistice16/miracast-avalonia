using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiraQt.DBus;
using MiraQt.Models;
using System.Diagnostics;
using Tmds.DBus;

namespace MiraQt.Services;

/// <summary>
/// Talks to the C daemon (gnome-network-displays-daemon) over the user's
/// session bus. The daemon owns "org.gnome.NetworkDisplays.Manager" at
/// /org/gnome/NetworkDisplays/Manager.
///
/// The daemon does ALL the heavy lifting (NetworkManager / wpa_supplicant /
/// GStreamer / RTSP). We only drive it.
/// </summary>
public sealed class NetworkDisplaysService : INetworkDisplaysService
{
    private const string BusName    = "org.gnome.NetworkDisplays.Manager";
    private static readonly ObjectPath ObjectPath = new("/org/gnome/NetworkDisplays/Manager");

    private Connection? _connection;
    private INetworkDisplaysManager? _proxy;
    private IDisposable? _propertyWatcher;

    public event Action<List<DisplayInfo>>? DisplaysChanged;
    public event Action<string>? ConnectionLost;

    public bool IsConnected => _proxy is not null;

    public async Task ConnectAsync()
    {
        int retries = 5;
        Exception? lastException = null;

        while (retries > 0)
        {
            try
            {
                _connection = Connection.Session;
                _proxy = _connection.CreateProxy<INetworkDisplaysManager>(BusName, ObjectPath);

                // This will fail if the daemon isn't running yet.
                _propertyWatcher = await _proxy.WatchPropertiesAsync(OnPropertiesChanged);
                
                // Push the initial state.
                await RefreshAsync();
                return; // Successfully connected!
            }
            catch (Exception ex)
            {
                lastException = ex;
                _proxy = null;
                _propertyWatcher?.Dispose();
                _propertyWatcher = null;
                
                if (retries == 5)
                {
                    // First failure: Try to launch the daemon automatically
                    EnsureDaemonRunning();
                }

                await Task.Delay(1000); // Wait 1s and retry
                retries--;
            }
        }

        ConnectionLost?.Invoke($"Could not connect to daemon after multiple attempts: {lastException?.Message}");
    }

    private void EnsureDaemonRunning()
    {
        try
        {
            // List of possible daemon locations, prioritizing the manually built one.
            var paths = new[] 
            {
                "/usr/local/libexec/gnome-network-displays-daemon",
                "/usr/libexec/gnome-network-displays-daemon",
                "/usr/bin/gnome-network-displays-daemon"
            };

            string? validPath = paths.FirstOrDefault(System.IO.File.Exists);

            if (validPath is not null)
            {
                Console.WriteLine($"[MiraQt] Spawning daemon: {validPath}");
                var psi = new ProcessStartInfo
                {
                    FileName = validPath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MiraQt] Failed to spawn daemon: {ex.Message}");
        }
    }

    public async Task RefreshAsync()
    {
        if (_proxy is null) return;

        try
        {
            var props = await _proxy.GetAllAsync();
            DisplaysChanged?.Invoke(ParseDisplays(props.Displays));
        }
        catch (Exception ex)
        {
            ConnectionLost?.Invoke($"Lost daemon: {ex.Message}");
        }
    }

    public async Task<string?> StartStreamAsync(string sinkUuid)
    {
        if (_proxy is null) return null;

        // Automatically clean up any crashed stream units to prevent UnitExists errors.
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "systemctl",
                Arguments = "--user reset-failed",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi)?.WaitForExit();
        }
        catch { }

        return await _proxy.StartStreamAsync(sinkUuid);
    }

    public async Task StopStreamAsync(string streamUnitName)
    {
        if (_proxy is null) return;
        await _proxy.StopStreamAsync(streamUnitName);
    }

    private void OnPropertiesChanged(PropertyChanges changes)
    {
        // Whenever any tracked property changes the daemon emits the whole
        // Displays array — re-parse.
        foreach (var kv in changes.Changed)
        {
            if (kv.Key == "Displays" && kv.Value is IDictionary<string, object>[] raw)
            {
                DisplaysChanged?.Invoke(ParseDisplays(raw));
                return;
            }
        }

        // Fall back to a refresh in case the property arrived in Invalidated.
        if (changes.Invalidated.Length > 0)
            _ = RefreshAsync();
    }

    private static List<DisplayInfo> ParseDisplays(IDictionary<string, object>[] raw)
    {
        var list = new List<DisplayInfo>(raw.Length);
        foreach (var dict in raw)
        {
            list.Add(new DisplayInfo
            {
                Uuid        = GetString(dict, "uuid"),
                DisplayName = GetString(dict, "display-name", "Unknown display"),
                Priority    = GetUInt(dict, "priority"),
                State       = (SinkState)GetUInt(dict, "state"),
                Protocol    = (SinkProtocol)GetUInt(dict, "protocol"),
            });
        }
        return list.OrderByDescending(d => d.Priority).ToList();
    }

    private static string GetString(IDictionary<string, object> d, string k, string fallback = "")
        => d.TryGetValue(k, out var v) && v is string s ? s : fallback;

    private static uint GetUInt(IDictionary<string, object> d, string k)
        => d.TryGetValue(k, out var v) && v is uint u ? u : 0u;

    public void Dispose()
    {
        _propertyWatcher?.Dispose();
        // Tmds.DBus's Connection.Session is process-shared; do not dispose it.
        _propertyWatcher = null;
        _proxy = null;
    }
}
