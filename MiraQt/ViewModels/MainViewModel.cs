using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiraQt.Models;
using MiraQt.Services;

namespace MiraQt.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INetworkDisplaysService _service;

    public ObservableCollection<DisplayInfo> Displays { get; } = new();

    [ObservableProperty]
    private DisplayInfo? _selectedDisplay;

    [ObservableProperty]
    private string _statusMessage = "Connecting to daemon…";

    [ObservableProperty]
    private bool _isDaemonConnected;

    public bool HasDisplays => Displays.Count > 0;

    public MainViewModel(INetworkDisplaysService service)
    {
        _service = service;
        _service.DisplaysChanged += OnDisplaysChanged;
        _service.ConnectionLost += OnConnectionLost;

        // Fire-and-forget connect; failures surface through ConnectionLost.
        _ = ConnectAsync();
    }

    private async Task ConnectAsync()
    {
        try
        {
            await _service.ConnectAsync();
            IsDaemonConnected = true;
            StatusMessage = "Searching for displays…";
        }
        catch (Exception ex)
        {
            IsDaemonConnected = false;
            StatusMessage = $"Daemon unreachable — is gnome-network-displays-daemon running? ({ex.Message})";
        }
    }

    private void OnDisplaysChanged(List<DisplayInfo> incoming)
    {
        // Marshal back to the UI thread; D-Bus signals come from a worker.
        Dispatcher.UIThread.Post(() =>
        {
            // Sync collection without Clear() to avoid UI flickering and lost focus
            for (int i = 0; i < incoming.Count; i++)
            {
                var fresh = incoming[i];
                var existingIdx = -1;
                for (int j = 0; j < Displays.Count; j++)
                {
                    if (Displays[j].Uuid == fresh.Uuid)
                    {
                        existingIdx = j;
                        break;
                    }
                }

                if (existingIdx >= 0)
                {
                    // Update existing
                    var current = Displays[existingIdx];
                    current.DisplayName = fresh.DisplayName;
                    current.Priority    = fresh.Priority;
                    current.State       = fresh.State;
                    current.Protocol    = fresh.Protocol;
                    
                    // Move if order changed
                    if (existingIdx != i)
                    {
                        Displays.Move(existingIdx, i);
                    }
                }
                else
                {
                    // Add new
                    Displays.Insert(i, fresh);
                }
            }

            // Remove items no longer present
            while (Displays.Count > incoming.Count)
            {
                Displays.RemoveAt(Displays.Count - 1);
            }

            OnPropertyChanged(nameof(HasDisplays));

            if (Displays.Count == 0)
                StatusMessage = "No displays found yet — make sure your TV is in cast mode.";
            else
                StatusMessage = $"{Displays.Count} display(s) found.";
        });
    }

    private void OnConnectionLost(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsDaemonConnected = false;
            StatusMessage = message;
        });
    }

    [RelayCommand]
    private async Task ConnectDisplayAsync(DisplayInfo? display)
    {
        if (display is null) return;
        StatusMessage = $"Connecting to {display.DisplayName}…";
        try
        {
            display.ActiveStreamUnit = await _service.StartStreamAsync(display.Uuid);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Connect failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DisconnectDisplayAsync(DisplayInfo? display)
    {
        if (display?.ActiveStreamUnit is null) return;
        StatusMessage = $"Disconnecting from {display.DisplayName}…";
        try
        {
            await _service.StopStreamAsync(display.ActiveStreamUnit);
            display.ActiveStreamUnit = null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Disconnect failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!IsDaemonConnected)
        {
            await ConnectAsync();
            return;
        }
        await _service.RefreshAsync();
    }
}
