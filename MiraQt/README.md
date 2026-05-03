# MiraQt

A Qt/Avalonia frontend for the GNOME Network Displays daemon, built for
KDE Plasma. The C# UI talks to the existing C daemon over D-Bus, so all the
hard stuff (Wi-Fi Direct, RTSP, GStreamer, codec negotiation) is left to
the upstream implementation. This tool is the thin, native-feeling client.

## Architecture

```
┌──────────────────────────┐    D-Bus session bus    ┌──────────────────────────────┐
│ MiraQt (Avalonia C#)     │ ◀─────────────────────▶ │ gnome-network-displays-daemon│
│  - List displays         │ org.gnome.NetworkDisplays│  - NetworkManager / wpa_sup │
│  - Connect / Disconnect  │            .Manager      │  - Spawns stream units      │
└──────────────────────────┘                          └──────────┬───────────────────┘
                                                                 │ systemd transient unit
                                                                 ▼
                                                ┌──────────────────────────────┐
                                                │ gnome-network-displays-stream│
                                                │  - libportal screencast      │
                                                │  - GStreamer + RTSP server   │
                                                └──────────────────────────────┘
```

## What you'll need on the Linux machine

These come from the upstream daemon — MiraQt does not change them:

- NetworkManager ≥ 1.15
- wpa_supplicant compiled with `CONFIG_P2P=y` and `CONFIG_WIFI_DISPLAY=y`
- PipeWire + xdg-desktop-portal-kde (for screen capture; KDE Plasma's portal
  is what makes this work outside GNOME)
- GStreamer with one of: openh264enc / x264enc / vah264enc / vaapih264enc
- For audio: fdkaacenc / faac / avenc_aac

Install on Arch:

```bash
sudo pacman -S networkmanager wpa_supplicant pipewire xdg-desktop-portal-kde \
               gst-plugins-base gst-plugins-good gst-plugins-bad gst-plugins-ugly \
               gst-plugin-pipewire gst-libav x264
yay -S gnome-network-displays    # or build from source with the patch below
```

## Build the daemon with the resolution patch (optional but recommended)

The upstream daemon hardcodes 1080p@30. Apply the patch in `Patches/` to
let it pick the highest negotiated resolution (up to 4K@60 if your codec
and the sink both support it):

```bash
git clone https://gitlab.gnome.org/GNOME/gnome-network-displays.git
cd gnome-network-displays
patch -p1 < /path/to/MiraQt/Patches/01-unlock-resolution.patch
meson setup build
meson compile -C build
sudo meson install -C build
```

## Build MiraQt (Windows or Linux)

You need .NET SDK 9.0+:

```powershell
# Windows
winget install Microsoft.DotNet.SDK.9
```

```bash
# Linux
sudo pacman -S dotnet-sdk
```

Then:

```bash
cd MiraQt
dotnet restore
dotnet run
```

To produce a self-contained Linux binary you can drop on the Surface:

```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
# output: bin/Release/net9.0/linux-x64/publish/MiraQt
```

## Running

1. Start the daemon (one-shot, runs in the background until logout):
   ```bash
   gnome-network-displays-daemon &
   ```
2. Put your Windows machine into "Connect" / wireless-display mode (Win+K).
3. Launch MiraQt. The display should appear within a few seconds.
4. Click **Connect**. Watch the status pip turn green.

If the daemon is unreachable, the bottom-left pip turns red and the status
bar tells you what's wrong.

## Project layout

```
MiraQt/
├── App.axaml(.cs)            -- App shell, theme, DI of services into the VM
├── Program.cs                -- Avalonia entry point
├── DBus/
│   └── INetworkDisplaysManager.cs   -- Tmds.DBus interface mirror
├── Services/
│   └── NetworkDisplaysService.cs    -- Connect, watch, Start/StopStream
├── Models/
│   └── DisplayInfo.cs               -- Single display, observable state
├── ViewModels/
│   └── MainViewModel.cs             -- List + commands + status
├── Views/
│   ├── MainWindow.axaml(.cs)
│   └── Converters.cs                -- bool/state -> color converters
└── Patches/
    └── 01-unlock-resolution.patch   -- Daemon-side resolution cap fix
```

## Known limitations

- This is a **source-side** implementation only — your Linux machine streams
  to a Windows / TV sink. It does not turn the Surface into a display sink
  for someone else's casting device.
- No codec installer dialog yet (upstream `nd-codec-install.c` is GNOME-Software
  specific). If a codec is missing, you'll get a daemon-side error and have
  to install it manually with pacman.
- No D-Bus auto-start. The daemon needs to be running before MiraQt connects.
  Adding a systemd user unit for this is the obvious next step.

## License

GPL-2.0, matching the daemon it talks to.
