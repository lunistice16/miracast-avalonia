# MiraQt

※これらのソースはClaude Opus 4.7を用いて生成されました。個人用のものなので安定性は一切保証できません！ それでもいいよという方は使ってください。gnome以外でMiracastを使いたいっていう方もいると思うので...。


**KDE Plasma で使える Miracast クライアント** — Avalonia (C#) で作った軽量UIが、裏で動く GNOME Network Displays デーモンを D-Bus 経由で操作します。Wi-Fi Direct / RTSP / GStreamer の重い処理は全部 C 側に任せて、このアプリは「見つけて、繋いで、切る」だけ。

> **ソース側（送信）専用です。** Linux の画面を Windows PC や Miracast 対応テレビにキャストします。

## 特徴

- 🖥️ **KDE Plasma ネイティブ感** — Breeze Dark 風のカラーパレット
- ⚡ **軽量** — コード全体が約 800 行。D-Bus クライアントに徹した設計
- 🎯 **MVVM** — CommunityToolkit.Mvvm のソースジェネレータ。リフレクションなし
- 📺 **解像度パッチ同梱** — upstream の 1080p30 固定を解除して最高解像度を自動選択
- 🪟 **Windows でも UI テスト可** — モックサービス内蔵。`dotnet run` するだけ

## アーキテクチャ

```
┌──────────────────────────┐    D-Bus session bus    ┌──────────────────────────────┐
│ MiraQt (Avalonia C#)     │ ◀─────────────────────▶ │ gnome-network-displays-daemon│
│  - ディスプレイ一覧       │ org.gnome.NetworkDisplays│  - NetworkManager / wpa_sup │
│  - Connect / Disconnect  │            .Manager      │  - systemd transient unit   │
└──────────────────────────┘                          └──────────┬───────────────────┘
                                                                 │
                                                                 ▼
                                                ┌──────────────────────────────┐
                                                │ gnome-network-displays-stream│
                                                │  - PipeWire screencast      │
                                                │  - GStreamer + RTSP server   │
                                                └──────────────────────────────┘
```

## クイックスタート (Arch Linux / KDE Plasma)

> 詳しい手順は [SETUP.md](SETUP.md) を参照してください。

### 1. 依存パッケージ

```bash
# ネットワーク・スクリーンキャスト
sudo pacman -S networkmanager wpa_supplicant \
               pipewire wireplumber xdg-desktop-portal xdg-desktop-portal-kde

# GStreamer (gst-rtsp-server はデーモンのビルドに必須)
sudo pacman -S gstreamer gst-plugins-base gst-plugins-good gst-plugins-bad \
               gst-plugins-ugly gst-plugin-pipewire gst-rtsp-server gst-libav x264

# デーモンのビルド依存 + .NET SDK
sudo pacman -S meson ninja gcc pkg-config glib2 \
               gtk4 libadwaita libportal-gtk4 libpulse dotnet-sdk
```

### 2. デーモンのビルド（解像度パッチ付き）

```bash
git clone https://gitlab.gnome.org/GNOME/gnome-network-displays.git
cd gnome-network-displays
patch -p1 < /path/to/MiraQt/Patches/01-unlock-resolution.patch
meson setup build
meson compile -C build
sudo meson install -C build
```

### 3. MiraQt のビルドと実行

```bash
git clone https://github.com/lunistice16/miracast-avalonia.git
cd miracast-avalonia/MiraQt
dotnet restore && dotnet run
```

### 4. 接続

```bash
# 別ターミナルでデーモン起動
gnome-network-displays-daemon &
```

1. Windows 側で **Win+K** → ミラーリング待機モードに
2. MiraQt にディスプレイが表示されたら **Connect** をクリック
3. ステータスが 🟢 **Streaming** になれば成功！

## プロジェクト構成

```
miracast-avalonia/
├── README.md                         ← このファイル
├── SETUP.md                          ← 詳細セットアップガイド
└── MiraQt/
    ├── MiraQt.csproj                 .NET 8.0, Avalonia 11.2, Tmds.DBus
    ├── Program.cs                    エントリポイント
    ├── App.axaml(.cs)                Breeze Dark 風テーマ + DI
    ├── DBus/
    │   └── INetworkDisplaysManager.cs   D-Bus インターフェースミラー
    ├── Services/
    │   ├── INetworkDisplaysService.cs   サービスインターフェース
    │   ├── NetworkDisplaysService.cs    本番: D-Bus 経由の接続
    │   └── MockNetworkDisplaysService.cs  テスト: ダミーデータ (Windows用)
    ├── Models/
    │   └── DisplayInfo.cs               Sink の状態モデル
    ├── ViewModels/
    │   └── MainViewModel.cs             一覧 + コマンド + ステータス
    ├── Views/
    │   ├── MainWindow.axaml(.cs)        メインUI
    │   └── Converters.cs                状態 → 色の変換
    └── Patches/
        └── 01-unlock-resolution.patch   1080p30 固定解除パッチ
```

## 既知の制限

- **送信専用** — Linux → Windows/TV の方向のみ。逆方向（誰かが Linux にキャスト）はできません
- **コーデック自動インストールなし** — エンコーダが足りない場合は手動で `pacman -S` してください
- **デーモン手動起動** — systemd ユーザーユニットで自動化する方法は [SETUP.md](SETUP.md) に記載

## ライセンス

GPL-2.0 — 通信先のデーモンと同じライセンスです。
