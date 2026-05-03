# MiraQt — Developer Notes

内部向けの技術メモ。ユーザー向けの情報はリポジトリルートの [README.md](../README.md) と [SETUP.md](../SETUP.md) を参照。

## D-Bus Interface

| 項目 | 値 |
|------|-----|
| Bus name | `org.gnome.NetworkDisplays.Manager` |
| Object path | `/org/gnome/NetworkDisplays/Manager` |
| Displays property | `aa{sv}` — 配列 of dict (`uuid`, `display-name`, `priority`, `state`, `protocol`) |
| `StartStream(uuid)` | → `string` (systemd transient unit 名) |
| `StopStream(unitName)` | → void |

元ソース: [nd-manager.c](https://gitlab.gnome.org/GNOME/gnome-network-displays/-/blob/master/src/nd-manager.c)

## SinkState enum (from nd-sink.h)

| 値 | 意味 |
|----|------|
| `0x0` | Disconnected |
| `0x50` | EnsureFirewall |
| `0x100` | WaitP2P |
| `0x110` | WaitSocket |
| `0x120` | WaitStreaming |
| `0x1000` | Streaming |
| `0x10000` | Error |

## Design decisions

- **`INetworkDisplaysService` インターフェース** — Windows ではモック、Linux では D-Bus 本番実装に切り替え
- **`ObservableCollection` のマージロジック** — `Clear()` を使わず in-place 更新して選択状態を保持
- **`[NotifyPropertyChangedFor]`** — CommunityToolkit.Mvvm 8.2+ の宣言的な依存プロパティ通知
- **デーモンの `Connection.Session` は Dispose しない** — Tmds.DBus のプロセス共有インスタンスのため

## Resolution patch

`Patches/01-unlock-resolution.patch` は `wfd-client.c` の `#if 0` ブロックを有効化し、
`wfd_video_codec_get_resolutions()` から最高解像度を選択するように変更。
ネイティブ解像度 (`codec->native`) は意図的に無視（実機でゴミ値を返すため）。
