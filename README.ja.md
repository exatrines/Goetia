# Goetia

[English](README.md)

![パーティリスト上の Attack / Bind / Stop ハイライト](docs/screenshots/party-highlight-1280x720.png)

Goetia は、手動マーカー付与を補助する Dalamud プラグインです。パーティリスト順（`<1>`–`<8>`）に合わせ、Attack / Bind / Stop のホットバースロットをハイライトします。

Goetia 自体は `/mk` を発行しません。`/mk` マクロは割り当てたホットバーに自分で配置する必要があります。設定でホットバーとスロットを対応づけ、使うモジュールを有効にすれば、戦闘中に対応するスロットがハイライトされます。

## インストール

1. `/xlsettings` を実行し、**試験的機能**タブを開く
2. **カスタムプラグインリポジトリ** に次の URL を追加する:

```
https://raw.githubusercontent.com/exatrines/DalamudPlugins/refs/heads/main/pluginmaster.json
```

3. `/xlplugins` を実行し、**Goetia** をインストールする

## 機能

- **ホットバー設定** — `/goetia settings` で Attack / Bind / Stop のホットバーと、`<1>` の基準スロットを対応づけます。3 本のホットバーを割り当て、連続する 8 スロットをパーティリスト順 `<1>`–`<8>` にします。例: ホットバー 6 / 7 / 8 のスロット 5〜12 が `<1>`〜`<8>`。
- **Preview オーバーレイ** — 必要なら表示します。パーティの席とホットバーの対応、どのモジュールがハイライトしているかを確認できます（メインの Eye、または設定の Preview で開き、× でオフ）。

## モジュール

`/goetia` でモジュール画面を開き、使うモジュールをオンにします。

### TOP（絶オメガ検証戦）

各モジュールと初期設定は以下の通りです。

#### Run Dynamis Delta (コードデュナミス・デルタ)

1. `ハローワールド・ニア/ファー` の対象2名に `Stop`

#### Run Dynamis Sigma (コードデュナミス・シグマ)

1. `ハローワールド・ニア/ファー` の対象2名に `Stop`
2. `デュナミスの高揚` 1スタックの対象（最大2名）に `Bind`
3. 残りの4名に `Attack`

#### Run Dynamis Omega (コードデュナミス・オメガ) 前半

1. `ファーストターゲット` の対象2名に `Stop`
2. `セカンドターゲット` かつ `デュナミスの高揚` 2スタックの対象に `Bind`
3. 手順2と合わせて2名になるように、`デュナミスの高揚` 2スタックの対象に `Bind`
4. 残りの4名に `Attack`

#### Run Dynamis Omega (コードデュナミス・オメガ) 後半

1. `ハローワールド・ニア/ファー` の対象2名に `Stop`
2. `デュナミスの高揚` 3スタックの対象2名に `Bind`
3. 残りの4名に `Attack`

### DSR（絶竜詩戦争）

各モジュールと初期設定は以下の通りです。

#### Wrath of the Heavens（至天の陣：風槍）

1. `サンダーウィング` の対象2名に `Stop`

#### Wroth Flames（邪念の炎）

1. `復讐の炎` の対象4名に `Attack`
2. `道連れの炎` の対象2名に `Bind`
3. 残りの2名に `Stop`

## コマンド

| コマンド | 説明 |
| --- | --- |
| `/goetia` | モジュール画面の表示切替 |
| `/goetia settings` | プラグイン設定画面の表示切替（`config` / `s` も可） |

## 開発者向け

1. ビルド: `dotnet build Goetia.sln -c Release -p:Platform=x64`
2. Dalamud の **dev plugin** パスを `Goetia/bin/Release/` に向ける
3. プラグインインストーラ（dev）で **Goetia** を有効化

共有 UI キットとして [MirageUI](https://github.com/exatrines/MirageUI) を git サブモジュールで同梱しています。

## ライセンス

[AGPL-3.0-or-later](LICENSE)
