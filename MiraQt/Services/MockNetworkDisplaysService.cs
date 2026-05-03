using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using MiraQt.Models;

namespace MiraQt.Services;

public class MockNetworkDisplaysService : INetworkDisplaysService
{
    public event Action<List<DisplayInfo>>? DisplaysChanged;
    public event Action<string>? ConnectionLost;

    public bool IsConnected { get; private set; }

    private readonly List<DisplayInfo> _displays = new();

    public async Task ConnectAsync()
    {
        await Task.Delay(500); // Simulate connection delay
        IsConnected = true;
        
        _displays.Add(new DisplayInfo
        {
            Uuid = "mock-uuid-1",
            DisplayName = "Living Room TV (Mock)",
            Priority = 10,
            State = SinkState.Disconnected,
            Protocol = SinkProtocol.WfdP2P
        });
        
        _displays.Add(new DisplayInfo
        {
            Uuid = "mock-uuid-2",
            DisplayName = "Bedroom Display (Mock)",
            Priority = 5,
            State = SinkState.Disconnected,
            Protocol = SinkProtocol.Chromecast
        });

        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (!IsConnected) return;
        await Task.Delay(100);
        // Dispatch copy of list
        var copy = new List<DisplayInfo>();
        foreach(var d in _displays) {
            copy.Add(new DisplayInfo {
                Uuid = d.Uuid,
                DisplayName = d.DisplayName,
                Priority = d.Priority,
                State = d.State,
                Protocol = d.Protocol
            });
        }
        DisplaysChanged?.Invoke(copy);
    }

    public async Task<string?> StartStreamAsync(string sinkUuid)
    {
        var display = _displays.Find(d => d.Uuid == sinkUuid);
        if (display == null) return null;

        // Simulate connection sequence in background
        _ = Task.Run(async () =>
        {
            display.State = SinkState.WaitP2P;
            await RefreshAsync();
            await Task.Delay(800);
            
            display.State = SinkState.WaitSocket;
            await RefreshAsync();
            await Task.Delay(800);
            
            display.State = SinkState.WaitStreaming;
            await RefreshAsync();
            await Task.Delay(800);
            
            display.State = SinkState.Streaming;
            await RefreshAsync();
        });

        return "mock-stream-unit-" + sinkUuid;
    }

    public async Task StopStreamAsync(string streamUnitName)
    {
        var display = _displays.Find(d => d.State == SinkState.Streaming);
        if (display != null)
        {
            display.State = SinkState.Disconnected;
            await RefreshAsync();
        }
    }

    public void Dispose() { }
}
