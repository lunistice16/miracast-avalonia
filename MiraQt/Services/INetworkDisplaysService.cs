using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiraQt.Models;

namespace MiraQt.Services;

public interface INetworkDisplaysService : IDisposable
{
    event Action<List<DisplayInfo>>? DisplaysChanged;
    event Action<string>? ConnectionLost;
    bool IsConnected { get; }
    Task ConnectAsync();
    Task RefreshAsync();
    Task<string?> StartStreamAsync(string sinkUuid);
    Task StopStreamAsync(string streamUnitName);
}
