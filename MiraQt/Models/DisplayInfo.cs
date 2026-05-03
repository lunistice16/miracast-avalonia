using CommunityToolkit.Mvvm.ComponentModel;

namespace MiraQt.Models;

public enum SinkState : uint
{
    Disconnected     = 0x0,
    EnsureFirewall   = 0x50,
    WaitP2P          = 0x100,
    WaitSocket       = 0x110,
    WaitStreaming    = 0x120,
    Streaming        = 0x1000,
    Error            = 0x10000,
}

public enum SinkProtocol : uint
{
    Unknown   = 0,
    WfdP2P    = 1,
    WfdMice   = 2,
    Chromecast = 3,
}

/// <summary>
/// Single discovered display. Maps the {sv} dict that the daemon emits.
/// </summary>
public partial class DisplayInfo : ObservableObject
{
    [ObservableProperty]
    private string _uuid = "";

    [ObservableProperty]
    private string _displayName = "";

    [ObservableProperty]
    private uint _priority;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StateLabel))]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    private SinkState _state = SinkState.Disconnected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProtocolLabel))]
    private SinkProtocol _protocol = SinkProtocol.Unknown;

    /// <summary>Set after StartStream returns; needed for StopStream.</summary>
    [ObservableProperty]
    private string? _activeStreamUnit;

    public string StateLabel => State switch
    {
        SinkState.Disconnected    => "Idle",
        SinkState.EnsureFirewall  => "Configuring firewall…",
        SinkState.WaitP2P         => "Establishing P2P…",
        SinkState.WaitSocket      => "Negotiating link…",
        SinkState.WaitStreaming   => "Starting stream…",
        SinkState.Streaming       => "Streaming",
        SinkState.Error           => "Error",
        _                         => $"Unknown ({(uint)State:X})",
    };

    public string ProtocolLabel => Protocol switch
    {
        SinkProtocol.WfdP2P     => "Wi-Fi Direct",
        SinkProtocol.WfdMice    => "Miracast / MICE",
        SinkProtocol.Chromecast => "Chromecast",
        _                       => "Unknown",
    };

    public bool IsBusy => State is not SinkState.Disconnected
                                and not SinkState.Streaming
                                and not SinkState.Error;

    public bool IsConnected => State == SinkState.Streaming;
}
