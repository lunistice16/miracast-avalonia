# MiraQt セットアップガイド

Arch Linux (KDE Plasma) で MiraQt を動かして、Linux の画面を Windows PC や Miracast 対応テレビにワイヤレスキャストするまでの完全な手順書です。

---

## 0. 前提条件

| 項目 | 必要なもの |
|------|-----------|
| 送信側 (Linux) | Arch Linux + KDE Plasma, Wi-Fi 搭載 |
| 受信側 | Windows 10/11 PC、または Miracast 対応テレビ |
| ネットワーク | 両方が同じ Wi-Fi に接続している必要はないが、Wi-Fi が有効であること |

> **注意**: Miracast は Wi-Fi Direct (P2P) を使うため、両端末が同じルーターに繋がっている必要はありません。ただし Wi-Fi アダプターが P2P に対応している必要があります。

---

## 1. リポジトリの取得

```bash
git clone https://github.com/lunistice16/miracast-avalonia.git
cd miracast-avalonia
```

---

## 2. 依存パッケージのインストール

### ネットワーク基盤

```bash
sudo pacman -S networkmanager wpa_supplicant
```

- `NetworkManager` — Wi-Fi Direct のピア検出に必要
- `wpa_supplicant` — P2P 接続のハンドリング (`CONFIG_P2P=y` と `CONFIG_WIFI_DISPLAY=y` でビルドされていること。Arch の公式パッケージは対応済み)

### スクリーンキャスト (PipeWire + Portal)

```bash
sudo pacman -S pipewire wireplumber xdg-desktop-portal xdg-desktop-portal-kde
```

KDE 環境では `xdg-desktop-portal-kde` が画面キャプチャの許可ダイアログを出します。これがないと "Screencasting portal is unavailable" エラーになります。

### GStreamer (映像・音声エンコード)

```bash
sudo pacman -S gstreamer gst-plugins-base gst-plugins-good gst-plugins-bad \
               gst-plugins-ugly gst-plugin-pipewire gst-rtsp-server gst-libav x264
```

- **`gst-rtsp-server`** — デーモンが RTSP サーバーを立てるのに必須。これがないとデーモンのビルドが通りません

H.264 エンコーダが最低 1 つ必要です。確認方法：

```bash
gst-inspect-1.0 x264enc    # ← これが見つかれば OK
```

### デーモンのビルド依存

```bash
sudo pacman -S meson ninja gcc pkg-config glib2 \
               gtk4 libadwaita libportal-gtk4 libpulse
```

- `gtk4` + `libadwaita` — デーモンの UI 部分（ビルドに必要だが MiraQt 使用時は非表示）
- `libportal-gtk4` — スクリーンキャストの PipeWire セッション管理
- `libpulse` — PulseAudio 経由の音声キャプチャ

### .NET SDK

```bash
sudo pacman -S dotnet-sdk
```

インストール確認：

```bash
dotnet --version
# 8.0.xxx 以上が出れば OK
```

---

## 3. GNOME Network Displays デーモンのビルド

MiraQt 自体は UI だけなので、実際に Wi-Fi Direct 接続や GStreamer ストリーミングを行う C 製のデーモンが必要です。

### パッチなし（1080p30 固定）の場合

AUR からインストールするだけで OK：

```bash
yay -S gnome-network-displays
```

### パッチあり（最高解像度の自動選択）— おすすめ

upstream は解像度を 1080p@30 にハードコードしています。同梱のパッチで解除できます：

```bash
# 作業用ディレクトリで
git clone https://gitlab.gnome.org/GNOME/gnome-network-displays.git
cd gnome-network-displays

# パッチ適用
patch -p1 < ~/miracast-avalonia/MiraQt/Patches/01-unlock-resolution.patch

# ビルド & インストール
meson setup build
meson compile -C build
sudo meson install -C build
```

インストール先の確認：

```bash
which gnome-network-displays-daemon
# /usr/local/libexec/gnome-network-displays-daemon など
```

---

## 4. MiraQt のビルド

```bash
cd ~/miracast-avalonia/MiraQt
dotnet restore
dotnet build
```

ビルドが通れば準備完了です。

---

## 5. 実行

### ターミナル 1: デーモンを起動

```bash
/usr/local/libexec/gnome-network-displays-daemon
```

ログが流れ始めます。エラーなく起動すれば OK。

### ターミナル 2: MiraQt を起動

```bash
cd ~/miracast-avalonia/MiraQt
dotnet run
```

Breeze Dark 風のウィンドウが開きます。

### 受信側 (Windows) の準備

1. **Win + K** を押すか、「設定」→「システム」→「このPCへのプロジェクション」を開く
2. 「すべての場所で使用可能」に設定
3. 「毎回接続を要求する」に設定

### 接続

1. MiraQt のウィンドウに Windows PC の名前が表示されるはず（数秒かかる場合あり）
2. **Connect** ボタンをクリック
3. ステータスが以下の順で遷移：
   - 🟡 `Establishing P2P…`
   - 🟡 `Negotiating link…`
   - 🟡 `Starting stream…`
   - 🟢 `Streaming` ← 成功！
4. 切断するときは **Disconnect** をクリック

---

## 6. デーモンの自動起動（オプション）

毎回手動で `gnome-network-displays-daemon` を起動するのが面倒な場合、systemd ユーザーユニットを作ると楽です：

```bash
mkdir -p ~/.config/systemd/user

cat > ~/.config/systemd/user/gnome-network-displays-daemon.service << 'EOF'
[Unit]
Description=GNOME Network Displays Daemon
After=graphical-session.target

[Service]
Type=simple
ExecStart=/usr/local/libexec/gnome-network-displays-daemon
Restart=on-failure
RestartSec=3

[Install]
WantedBy=graphical-session.target
EOF

systemctl --user daemon-reload
systemctl --user enable --now gnome-network-displays-daemon
```

> `ExecStart` のパスは `which gnome-network-displays-daemon` の結果に合わせてください。

確認：

```bash
systemctl --user status gnome-network-displays-daemon
```

---

## 7. 自己完結型バイナリの作成（オプション）

`dotnet` を入れたくない別マシンに配る場合：

```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
```

`bin/Release/net10.0/linux-x64/publish/MiraQt` に単一バイナリが生成されます。このファイルだけコピーすれば動きます。

---

## トラブルシューティング

### デーモンに接続できない（ステータスが赤）

```bash
# デーモンが D-Bus に登録されているか確認
busctl --user list | grep NetworkDisplays
```

何も出ない場合はデーモンが起動していません。

### ディスプレイが表示されない

```bash
# Wi-Fi が有効か
nmcli radio wifi

# P2P がサポートされているか
iw phy | grep -i p2p
```

`P2P-GO` や `P2P-client` が出ない場合、Wi-Fi アダプターが P2P 非対応です。

### "Screencasting portal is unavailable"

```bash
# ポータルが動いているか
systemctl --user status xdg-desktop-portal-kde
```

動いていなければ：

```bash
systemctl --user restart xdg-desktop-portal xdg-desktop-portal-kde
```

### コーデック不足

デーモンのログに codec 関連のエラーが出る場合：

```bash
# H.264 エンコーダの確認
gst-inspect-1.0 | grep -i "264.*enc"

# VA-API (ハードウェアエンコード) が使えるか
gst-inspect-1.0 vah264enc
```

### デーモンのログを詳しく見る

```bash
G_MESSAGES_DEBUG=all gnome-network-displays-daemon
```

---

## Windows でのUIテスト

Linux 環境がなくても、Windows 上で UI の見た目と動作を確認できます。モックサービスが自動的に使われ、ダミーのディスプレイが表示されます：

```powershell
cd miracast-avalonia\MiraQt
dotnet run
```

「Connect」を押すと接続シーケンスのアニメーション（P2P → Socket → Streaming）が再現されます。
