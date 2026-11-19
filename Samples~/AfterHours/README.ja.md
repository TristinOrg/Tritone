# Tritone Feature Playground

### Tritone を試す Unity 向けの小さな独立サンプル

![After Hours — a night route through the city](Art/after-hours-banner.svg)

**機能を選び、操作を 1 回試して、結果を確認します。**

この Sample は Tritone の機能を個別に確認できる独立シーン集です。ローカルのデモはゲームを遊ばなくても理解できるよう、一つの機能に絞っています。Sample Hub から開始し、各シーンで操作して Hub に戻れます。

墨色のネイビー、温かみのある紙色、路線図の幾何学模様、控えめな青緑を使ったビジュアルです。バナーは Sample に含まれる SVG です。

**言語：** [English](README.md) · [简体中文](README.zh-CN.md) · 日本語

---

## Unity で始める

1. Unity 2022.3 LTS に Tritone をインストールします。
2. パッケージの **Samples** から **Tritone Feature Playground** をインポートします。
3. **Tritone → After Hours → Open Sample Hub** を選び、**Play** を押します。サンプルが `Assets/AfterHours` に用意され、シーンが Build Settings に登録されます。
4. 機能を選び、画面の案内に沿って操作します。**サンプル一覧へ戻る**から別の機能を開けます。

`Assets/AfterHours/Scenes` から各シーンを直接開くこともできます。操作と結果が画面に表示され、配信や通信のデモにはローカル設定手順も表示されます。

## シーンを選ぶ

| シーン | 機能 | 操作と結果 |
| --- | --- | --- |
| `ApplicationLab` | アプリのライフサイクル | アプリが動作していることと、クリックでモジュールのカウントが増えることを確認します。 |
| `FlowLab` | フロー遷移 | 2 回押して Running から Ready へ切り替えます。古いフローは終了します。 |
| `ModelLab` | 共有モデル | 2 回加算してからリセットし、カウントが初期値に戻ることを確認します。 |
| `ScopeLab` | モジュールスコープ | 一度発行し、次に購読を解除して発行します。リセットで再購読します。 |
| `DiagnosticsLab` | ランタイム診断 | ログを記録し、バッファ件数とフレーム番号を確認します。 |
| `DispatcherLab` | メインスレッド配送 | コールバックを登録し、次の更新で実行されたスレッドの ID を確認します。 |
| `EventLab` | 型付きイベント | 数回発行し、連番付きの信号を購読側が受け取ることを確認します。 |
| `TweenLab` | トゥイーン | 再生するとバーが 0% から 100% へ移動します。リセットで中断します。 |
| `TimerLab` | タイマー | 一度だけ実行するコールバックを予約します。 |
| `SceneModuleLab` | シーンモジュール | シーンを再読込せずに Lobby と Round を切り替えます。 |
| `InputLab` | 名前付き入力 | クリックして Interact を送り、カウントを確認します。旧入力が有効なら E キーも使えます。 |
| `AudioLab` | 効果音の再生 | Game ビューのミュートを解除して再生します。リセットで停止します。 |
| `SaveLab` | セーブとロード | 保存して読み戻します。再入場で続きから再開し、リセットで消去します。 |
| `SettingsLab` | 型付き設定 | 0〜3 の値を順に選び、変更した設定を読み戻します。 |
| `LocalizationLab` | ローカライズ | 言語を切り替え、同じ office.open キーから別の文字列を取得します。 |
| `EcsLab` | ECS — Entity の生成と Query | Spawn Marker で Entity を追加し、Query 数を確認して World をリセットします。 |
| `UiLab` | UI — Tritone Window の開閉 | Popup Layer に生成 Window を開き、閉じて再び開きます。 |
| `PoolLab` | オブジェクトプール | 3 回クリックして貸出、返却、再貸出を順に試します。ハイライトと、同じインスタンスが再利用されたかを確認します。 |
| `DataWorkshop` | Tables — 型付きデータの読込 | 生成済み ClueRow Table を読み込み、行数と最初のデータを確認します。 |
| `ResourcesLab` | Resources 読込 | スコープでテキストを読み込み、文字数を表示して参照を解放します。 |
| `ContentWorkshop` | コンテンツ更新とトランザクション | オフラインのコンテンツ更新、ステージング、コミット、ロールバックを確認します。 |
| `AddressablesDesk` | Addressables Provider プロファイル | 任意の Addressables 構成で同じ Sample Asset を読み込みます。 |
| `BundleDesk` | AssetBundle Provider プロファイル | ローカル Bundle、Manifest、専用 Provider 構成でコンテンツを読み込みます。 |
| `NetworkDesk` | Network — ローカル Request を送信 | 独立 Network サンプルを開き、任意のローカル Relay 設定を確認します。 |
| `ProtocolLab` | ハンドシェイクと再接続 | Node.js で LocalServices/relay.js を起動して接続し、停止・再起動で接続状態を確認します。 |

## Tritone の機能範囲

- **Application と Module：** Bootstrap 構成、Module 順序、Application/Scene Lifetime、Context、Scope、Scene Module、Scene 切り替え。
- **Runtime Service：** 診断と構造化ログ、メインスレッド Dispatch、型付き Event、Timer、Tween、Object Pool、Input、Audio、Model、Save、Settings、Localization。
- **Entity：** Entity World、Component 登録、Query、順序付き System。
- **UI：** `UIWindow`、生成された `UIView` 参照、Prefab Binding、Popup Sorting、Window Lifetime。
- **Table：** Schema に基づく型付き Row、CSV/JSON 入力、コード生成、Runtime 読込、サンプル Validator。
- **Content：** Resources、Addressables、AssetBundle Provider、Manifest、Update Plan、Integrity Check、Transaction Recovery。
- **Network：** 生成 Message、TCP Session、Protocol Compatibility、Heartbeat、Reconnect。
- **Editor Workflow：** Provider 設定、Scene 作成、UI Binding、Table/Network 生成、Content Build。

Editor 専用コマンドは対応する Sample と `Tritone/After Hours/…` メニューに記載します。Runtime の操作ボタンとは区別しています。

## 任意のローカル Profile

After Hours のシーンで Play を押すと、不足している Addressables と AssetBundle コンテンツが自動生成されます。Unity の PATH に Node.js がある場合、ネットワークとコンテンツ更新のローカルサービスも起動します。次のメニューは手動で再構築する場合に使います。

Addressables、AssetBundle、Content Update、Network は別々の Bootstrap を使います。試したい Profile だけを実行してください。ローカルの機能デモには統合されません。

- **Content Update：** **Tritone → After Hours → Prepare Provider Labs**、続けて **Build Local Bundle and Update Manifest** を実行します。`node "<Import した Sample フォルダー>/LocalServices/content-host.js"` で同梱の Loopback Content Host を起動します。
- **Addressables：** Host Project に Unity Addressables を導入または有効化し、**Prepare Provider Labs** と **Build Addressables Lab Content** を実行します。
- **AssetBundle：** **Prepare Provider Labs** の後に **Build Local Bundle and Update Manifest** を実行します。
- **Network：** Terminal で `node "<Import した Sample フォルダー>/LocalServices/relay.js"` を起動して **Network Desk** を開きます。Relay は `127.0.0.1` のみで待ち受けます。

任意の依存関係を使えなくても、Hub とオフラインデモは利用できます。

## Authoring コマンド

Sample を Import した後、Unity の **Tritone → After Hours** を開きます。

- **Generate Sample Scenes** — Hub と独立デモ Scene を作り、Build Settings に追加します。
- **Regenerate Table Outputs** — `Schemas/Tables.json` と CSV から型付き Row と JSON を生成します。Sample の `ITableValidator` が入力データを検査します。
- **Regenerate Network Messages** — `Schemas/Network.json` から Message 型、Codec、Registry、Schema Fingerprint を生成します。
- **Generate Systems Window View** / **Bind Generated Systems Window View** — 型付き UI View を生成して Binding します。
- **Prepare Provider Labs** — Provider ごとの Sample Content を準備します。
- **Build Addressables Lab Content** / **Build Local Bundle and Update Manifest** — 任意の Asset Profile を構築します。
- **Tritone → Build → Content for Active Platform** — 選択中の Unity Target で共通 Content Build を実行します。

## Package 構成

```text
AfterHours/
├── Art/                 同梱 SVG バナー
├── Data/                CSV Sample データ
├── Editor/              Scene、Table、UI、Network、Content の Authoring Tool
├── LocalServices/       任意の Loopback Relay と Content Host
├── Runtime/             独立デモ Bootstrap、UI、Data、生成コード
├── Schemas/             Table と Network の Schema
├── Scenes/              Hub と独立機能/Profile Scene
└── Tests/EditMode/      Sample データ、Catalog、Rule のテスト
```

## 検証

Tritone Repository の Root から実行します。

```powershell
.\.ci\Validate-Package.ps1
```

Unity Test Runner で `AfterHours.Sample.Tests` EditMode Assembly を実行すると、Catalog Route、Sample Data、Demo Rule を確認できます。

---

**[English](README.md)** · **[简体中文](README.zh-CN.md)**

