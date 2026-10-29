# Tritone

**Unity 向けのモジュール型ゲームフレームワーク。ライフサイクルを担う C# カーネルは `UnityEngine` に依存せず、モジュールが取得したリソースの所有者になります。**

必要なサービスだけを組み合わせ、モジュールが自分のリソースを解放します。起動、更新、シーン遷移、終了を分かりやすく保ちます。

[English](README.md) · [简体中文](README.zh-CN.md) · [日本語](README.ja.md)

[![Unity 2022.3+](https://img.shields.io/badge/Unity-2022.3%2B-222c37?logo=unity&logoColor=white)](https://unity.com/releases/editor/whats-new/2022.3.0)
[![UPM](https://img.shields.io/badge/UPM-Git%20URL-496b73)](https://github.com/TristinOrg/Tritone)
[![License](https://img.shields.io/github/license/TristinOrg/Tritone)](LICENSE)
[![Stars](https://img.shields.io/github/stars/TristinOrg/Tritone?style=social)](https://github.com/TristinOrg/Tritone/stargazers)
[![Unity Package workflow](https://github.com/TristinOrg/Tritone/actions/workflows/unity-package.yml/badge.svg?branch=main)](https://github.com/TristinOrg/Tritone/actions/workflows/unity-package.yml)

![Tritone の構成図：Unity 非依存のカーネル、モジュールスコープの機能、Unity アダプター](Documentation~/tritone-architecture.svg)

## Tritone の特長

- **小さく移植しやすいカーネル。** アプリケーションライフサイクル、モジュール、サービス、フロー、モデル、エンティティ、イベント、タイマー、Tween、スレッドディスパッチ、実行時診断は `UnityEngine` に依存しません。
- **明確な所有権。** モジュールスコープは、モジュール停止時に所有するタイマー、イベント購読、アセット、プール済みオブジェクトなどを解放します。
- **必要なサービスを構成。** シーンや UI からネットワーク、コンテンツ配信、ローカライズ、セーブ、テーブルまで、使う機能だけを有効にできます。
- **予測可能な遷移。** 依存順に起動し、逆順に停止します。シーンやフローの有効化に失敗した場合は、以前の状態を復元します。
- **エディターワークフローを同梱。** 型付き UI View、設定テーブルとネットワークメッセージのコード生成、バージョン付きコンテンツビルドに対応します。

Tritone は完成済みのゲームやスタータープロジェクトではなく、既存の Unity プロジェクトに導入する Unity Package Manager パッケージです。機能ごとにゲームコードを整理しやすくします。

## 機能一覧

| 分野 | 主な機能 |
| --- | --- |
| カーネル | モジュール型アプリケーションライフサイクル、依存順序、サービスレジストリ、スコープ所有権、メインスレッドディスパッチ |
| ゲームアーキテクチャ | フロー、共有モデル、軽量エンティティワールド、型付きイベント、タイマー、Tween、実行時診断 |
| Unity ランタイム | Bootstrap、シーン、UI 合成、入力、オーディオ、プール、アセット、セーブ、設定、ローカライズ、テーブル |
| コンテンツパイプライン | Resources、Addressables、AssetBundle、生成マニフェスト、トランザクション型リモート更新 |
| ネットワーク | TCP セッション、再接続、Request/Response、プロトコル互換性ハンドシェイク、メッセージ生成 |
| エディターツール | UI View 生成、テーブル/メッセージ生成、テーブルコンパイル、コンテンツビルド |

## 目次

- [インストールと起動](#インストール)
- [Kernel の基盤](#kernel-の基盤)
- [アーキテクチャとライフサイクル](#モジュールコンテキストと機能)
- [ゲームシステム](#モデルと状態のクイックスタート)
- [Unity 連携](#ui-のクイックスタート)
- [アセットとコンテンツ配信](#アセットのクイックスタート)
- [テーブルとネットワーク](#設定テーブルのクイックスタート)
- [CI](#継続的インテグレーション)
- [パッケージドキュメント](#パッケージドキュメント)

以下では各システムの API、例、ライフサイクルを説明します。UI とテーブルの詳しい作成手順はパッケージガイドを参照してください。

## インストール

Unity Package Manager の **Add package from git URL** を選び、次の URL を入力します。

```text
https://github.com/TristinOrg/Tritone.git
```

本番プロジェクトではリリースタグを指定し、依存バージョンを固定してください。タグなし URL は `main` を追跡したい場合に使用します。必要環境は Unity 2022.3 以降、Addressables 1.21.21（または互換性のある新しいバージョン）です。

本番プロジェクトでは現在のリリースタグを指定してください。

```text
https://github.com/TristinOrg/Tritone.git#v0.2.0
```

## 継続的インテグレーション

GitHub Actions はパッケージメタデータと Unity GUID を検証し、Unity 2022.3 で EditMode と PlayMode のテストを実行します。Unity テストの実行には `UNITY_LICENSE`、`UNITY_EMAIL`、`UNITY_PASSWORD` のリポジトリシークレットが必要です。未設定の場合もパッケージ検証は行われ、Unity テストをスキップした旨が明示されます。

## Kernel の基盤

最初のマイルストーンでは以下を提供します。

- Unity に依存しない C# アプリケーションライフサイクル。
- 明示的なモジュール依存関係とトポロジカル順序での起動。
- 逆順での終了と、起動失敗時の部分ロールバック。
- 小規模なシングルトンサービスレジストリ。
- PreUpdate、Update、LateUpdate、FixedUpdate のアロケーションフリーなディスパッチ。
- Unity Bootstrap と、複数の出力先に対応したフィルタ付き診断ログ。

## Unity クイックスタート

モジュールを作成します。

```csharp
using Tritone.Kernel;

public sealed class HelloModule : ModuleBase, IUpdateSystem
{
    public int Order => 0;
    protected override ELogLevel LogLevel => ELogLevel.Debug;

    protected override void OnStart()
    {
        Logger.Info("Hello module started.");
    }

    public void Update(in FrameTime time) { }

    protected override void OnStop()
    {
        Logger.Info("Hello module stopped.");
    }
}
```

アプリケーションの入口を作成します。

```csharp
using Tritone.Diagnostics;
using Tritone.Kernel;
using Tritone.Unity;

public sealed class GameBootstrap : TritoneBootstrap
{
    protected override void Configure(GameApplicationBuilder builder)
    {
        builder.UseLogging(ELogLevel.Debug, new UnityLogSink());
        builder.AddModule(new HelloModule());
    }
}
```

`GameBootstrap` を起動シーンの GameObject に追加し、Play Mode を開始します。

## モジュールコンテキストと機能

各モジュールには独立した `ModuleContext` が渡されます。コンテキストは機能と共通の `ModuleScope` をまとめます。

```text
ModuleBase
    -> ModuleContext
        -> Assets / Timers / UI / Network / other capabilities
            -> Service contracts
            -> ModuleScope ownership
```

`ModuleBase` は `LoadAsset`、`SetTimer`、`BindMessage` などの互換ファサードを引き続き提供します。実装とリソース所有権は各機能にあります。新しい機能も、共有処理をサービス契約に置き、モジュール向けアダプターを機能に置き、取得したリソースをモジュールスコープで解放する構成にします。

依存関係を明示したい場合、モジュールから機能を直接利用できます。

```csharp
var prefab = await Context.Assets.LoadAsync<GameObject>("Characters/Player");
Context.Events.Bind(player.Events.HealthChanged, OnHealthChanged);
```

利用していない機能は遅延生成され、機能専用のスコープは確保されません。

## メインスレッドディスパッチ

非同期サービスを使うモジュールより前に、上限付きディスパッチャーを設定します。

```csharp
builder.UseMainThreadDispatcher(maxCallbacksPerUpdate: 4096);
```

ワーカースレッドで処理が完了したらメソッドグループを投稿します。モジュールが先に停止した場合は所有権により自動でキャンセルされます。

```csharp
private void OnDownloadCompleted()
{
    PostToMainThread(ApplyDownloadedContent);
}

private void ApplyDownloadedContent()
{
    // Safe to access Unity objects here.
}
```

`Context.MainThread.Post(callback)` は読み取り専用の `DispatchHandle` を返し、明示的なキャンセルや保留状態の確認に使えます。スレッドセーフなキューに小さな構造体を入れ、`PreUpdate` で決定的な FIFO 順にコールバックを実行します。1 フレームあたりの上限でキューの滞留がフレームを占有するのを防ぎ、1 件のコールバックが失敗しても後続を実行します。

## モデルと状態のクイックスタート

ファクトリーと所有期間を明示して共有状態を登録します。

```csharp
builder.AddApplicationModel<PlayerModel>();
builder.AddSceneModel(() => new BattleModel(mBattleRules));
```

モデルのライフサイクルは Unity に依存しません。

```csharp
public sealed class PlayerModel : IModel
{
    public readonly Event<int> LevelChanged = new();
    public int Level { get; private set; }

    public void Initialize() { Level = 1; }
    public void Reset() { Level = 1; LevelChanged.Clear(); }
    public void Dispose() { LevelChanged.Clear(); }
}
```

機能または互換ファサード経由でモデルを取得します。

```csharp
var player = Context.Models.Get<PlayerModel>();
var battle = GetModel<BattleModel>();
```

モデルは初回アクセス時に生成され、登録された具象型ごとに共有されます。アプリケーションモデルはシーンをまたいで存続し、アプリケーション終了時に生成順の逆順で解放されます。シーンモデルはアクティブなシーンモジュールを必要とし、次のシーンモジュール起動前に解放されます。インスタンスを所有するのは利用者ではないため、あるモジュールが停止しても他の利用者の状態は無効になりません。初期化に失敗した場合、未完成のインスタンスを破棄し、後で再試行できるよう登録を残します。

## ゲームフローのクイックスタート

アプリケーションフローを明示的に登録します。

```csharp
builder.AddFlow(() => new LoginFlow(mLoginConfig));
builder.AddFlow<LobbyFlow>();
builder.AddFlow<BattleFlow>();
```

フローはアプリケーションの大きな段階を担当します。

```csharp
public sealed class LoginFlow : IFlow
{
    public async Task EnterAsync(CancellationToken cancellationToken)
    {
        await LoadLoginDataAsync(cancellationToken);
    }

    public void Update(in FrameTime time) { }
    public void Exit() { }
    public void Dispose() { }
}
```

モジュールからフローを切り替えます。

```csharp
await Context.Flows.SwitchAsync<LoginFlow>(cancellationToken);
await SwitchFlowAsync<BattleFlow>(cancellationToken);
```

同時にアクティブなフローは 1 つです。同じ対象への同時リクエストは実行中の遷移を共有し、競合する対象は拒否します。現在のフローを終了してから対象に入りますが、対象の開始が成功するまで旧フローは保持されます。開始が失敗またはキャンセルされた場合、未完成の対象を破棄して元のフローに再入場します。アプリケーション終了時にはアクティブなフローが自動的に終了します。

## エンティティワールドのクイックスタート

ワールドのライフタイムに対して構造体コンポーネントと順序付きシステムを登録します。

```csharp
builder.UseEntities(initialCapacity: 256);
builder.AddApplicationComponent<PlayerIdentity>();
builder.AddSceneComponent<Position>();
builder.AddSceneComponent<Velocity>();
builder.AddSceneEntitySystem<MovementSystem>();
```

コンポーネントデータは通常の値型です。

```csharp
public struct Position : IEntityComponent
{
    public float X;
    public float Y;
}
```

エンティティを作成し、参照経由でコンポーネントを変更します。

```csharp
EntityWorld world = Context.Entities.Scene;
EntityId entity = world.Create();
world.Add(entity, new Position { X = 10.0f, Y = 0.0f });

ref Position position = ref world.Get<Position>(entity);
position.X += 5.0f;
```

クエリは軽量な構造体です。インデックス走査では列挙子を確保せず、コンポーネント参照へ直接アクセスできます。`EntityId` はスロット番号と世代番号を持ち、破棄された ID が再利用スロットの別エンティティにアクセスするのを防ぎます。アプリケーションワールドはシーンをまたいで存続します。シーンワールドはシーンモジュール設定前に作られ、そのモジュール停止後にシステム、エンティティ、コンポーネントを解放します。システムは安定した順序で初期化され、通常のモジュールより前に更新され、逆順で終了します。

## Tween とシーケンス

共有 Tween スケジューラーを一度有効にします。

```csharp
builder.UseTweens();
```

モジュール所有の setter で数値を補間します。

```csharp
Tween(
    0.0f,
    1.0f,
    0.25,
    value => mCanvasGroup.alpha = value,
    ETweenEase.OutCubic);
```

スケジューラーは Unity 型に依存せず、UI、Transform、音量、モデル、シミュレーション状態に使えます。頻繁に Tween を作成する場合、呼び出し側のクロージャー確保を避けるにはメソッドグループデリゲートをキャッシュします。

フレーム更新外で不変シーケンスを構築して再利用します。

```csharp
TweenSequence sequence = new TweenSequenceBuilder()
    .Append(0.0f, 1.0f, 0.2, SetAlpha, ETweenEase.OutQuad)
    .AppendDelay(0.1)
    .AppendCallback(OnShown)
    .Build();

TweenHandle handle = PlayTweenSequence(sequence);
PauseTween(handle);
ResumeTween(handle);
CancelTween(handle);
```

未使用のフレーム時間は次のステップに引き継がれ、有限/無限ループ、スケール/非スケールのクロックを使えます。Tween コールバック内で登録された処理は次のアプリケーション更新から開始し、同一フレームでの再帰実行を防ぎます。setter や完了処理の例外はその Tween だけに影響します。所有するモジュールまたはシーンモジュールの停止時に、実行中の Tween は自動キャンセルされます。

## 実行時診断

状態のサンプリングと固定サイズの最近ログバッファーを設定できます。他の出力先から同じログを Unity Console へ転送できます。

```csharp
builder.UseRuntimeDiagnostics(
    ELogLevel.Debug,
    logCapacity: 256,
    sampleWindow: 0.5,
    new UnityLogSink());
```

コードから最新スナップショットを読み取ります。

```csharp
var diagnostics = application.Services.GetRequired<IRuntimeDiagnosticsService>();
var snapshot = diagnostics.Snapshot;

Logger.Info($"FPS: {snapshot.FramesPerSecond:F1}");
Logger.Info($"Scene entities: {snapshot.SceneEntities}");
```

スナップショットにはアプリケーション状態、アクティブなシーンモジュール/フロー、アプリケーションとシーンのエンティティ数、FPS、フレーム時間の平均/最小/最大値、フレーム番号が含まれます。最近のログは固定サイズのリングバッファーに時系列で保持され、満杯になると古いイベントを上書きします。

任意の IMGUI パネルとして `RuntimeDiagnosticsOverlay` を Bootstrap GameObject に追加できます。既定では F3 キーで表示を切り替えます。診断サービスとは独立しているため、ログやサンプリングを変更せずに独自 UI に置き換えられます。

## タイマー

共有タイマースケジューラーを一度登録します。

```csharp
builder.UseTimers();
builder.AddModule(new GameplayModule());
```

`ModuleBase` では整数キーまたは文字列キーを使えます。

```csharp
protected override void OnStart()
{
    SetTimer("Refresh", 2.0, OnRefresh);
    SetRepeatedTimer(1001, 1.0, OnSecondPassed);
}

private void OnSecondPassed()
{
    Logger.Debug("One second passed.");
}

private void OnRefresh()
{
    CancelTimer(1001);
}
```

モジュールごとに複数のキー付きタイマーを所有できます。同じキーを設定すると以前のタイマーを置き換えます。停止時に所有タイマーとキーキャッシュを自動で解除します。頻繁にスケジュールする処理ではメソッドグループデリゲートをキャッシュしてください。キャプチャ付き lambda はスケジューラーが再利用領域を使っていても確保が発生することがあります。

## イベント

強い型付けイベントを、それを所有するモジュールにまとめます。

```csharp
public sealed class PlayerModule : ModuleBase
{
    public EventsList EventsList { get; } = new();

    public sealed class EventsList
    {
        public readonly Event<int> HealthChanged = new();
        public readonly Event<int, PlayerData> PlayerDied = new();
    }

    private void NotifyHealthChanged(int health)
    {
        EventsList.HealthChanged.Publish(health);
    }
}
```

## UI のクイックスタート

Prefab からモジュールまでの詳細な手順は[UI 作成と合成](Documentation~/ui-authoring.md)を参照してください。

View は Prefab への参照を保持し、ビジネスロジックは持ちません。

```csharp
public sealed class UIShopView : UIView
{
    public Button        BtnClose;
    public Button        BtnRule;
    public RectTransform NodePanels;
}
```

Prefab ベースの作成では、UI Prefab のルートに `UIPrefabRef` を追加します。Inspector で子 GameObject や Component を割り当て、型を選択して次の操作を行います。

1. `Generate UIView Script` で指定 Assets フォルダーに決定的な `// <auto-generated />` View ソースを生成。
2. Unity のコンパイル後に `Bind Generated UIView` を実行し、生成コンポーネントを追加して型付き参照を設定。
3. `Preprocess Sorting Hierarchy` でネストした Canvas/Renderer を制作時の描画順と階層深度で記録。

ウィンドウを開くと UI レイヤー内で最前面になります。Tritone は前処理したノードに連続した実行時ソート値を割り当てます。`EUILayer` ごとにソート値の範囲を分けるため、複数ウィンドウ間でネスト Canvas、Particle、Sprite、Trail が競合しません。Prefab の描画階層や制作時のソート値を変更したら再度前処理します。

専用のバインド段階で Unity コントロールと Tritone イベントを接続します。

```csharp
public sealed class ShopWindow : UIWindow<UIShopView>
{
    protected override void OnBindEvents()
    {
        BindButton(mView.BtnClose, Close);
        BindButton(mView.BtnRule, OnRule);

        var playerModule = GetModule<PlayerModule>();
        BindEvent(playerModule.EventsList.HealthChanged, OnHealthChanged);
    }

    private void OnHealthChanged(int health) { }
    private void OnRule() { }
}
```

`UIElement<TView>` は View を一度だけ解決し、`OnBindEvents` でリスナーを登録します。無効化時には Unity と Tritone の全リスナーを解放します。モジュール停止時には `ModuleBase` の全バインドが解除されます。

ウィンドウごとに再利用アイテムテンプレートと単一インスタンスのパネルを登録できます。アイテム Prefab は遅延ロードされ共有 Prefab プールを利用し、パネルは遅延生成されウィンドウの稼働中に一つのインスタンスを保持します。ウィンドウ終了時に合成インスタンスを返却し、ウィンドウ解放時にテンプレートアセットも解放します。動的子 View は親ウィンドウのソート順を共有します。

Bootstrap でアセット、プール、UI を有効化し、モジュールからウィンドウを登録します。

```csharp
protected override void Configure(GameApplicationBuilder builder)
{
    builder.UseAssets();
    builder.UsePools();
    builder.UseUI(mUIRoot);
    builder.AddModule(new ShopModule());
}
```

```csharp
public sealed class ShopModule : ModuleBase
{
    protected override void OnConfigure(IServiceRegistry services)
    {
        AddWindow<ShopWindow>("UI/Shop/ShopWindow", EUILayer.Normal);
    }

    private void ShowShop() { OpenWindow<ShopWindow>(); }
    private async Task ShowShopAsync() { await OpenWindowAsync<ShopWindow>(); }
}
```

該当する定義を所有するアクティブなモジュールが 1 つ以上あればモジュールウィンドウを使えます。重複登録は同じパス、レイヤー、ライフタイムにする必要があります。最後の所有者が解放されるとウィンドウを閉じ、キャッシュインスタンスと Prefab を破棄して定義を削除します。アプリケーションウィンドウはアプリケーション終了まで有効です。

## シーンモジュールの切り替え

常駐インフラモジュールはアプリケーションとともに起動します。シーンモジュールはファクトリーとして登録し、入場時に生成します。

```csharp
builder.UseTimers();
builder.UseAssets();
builder.UseUI(mUIRoot);
builder.UseScenes();
builder.AddSceneModule<LoginModule>();
builder.AddSceneModule(() => new BattleModule(mBattleConfig));
```

`ModuleBase` または `TritoneComponent` から Unity シーンをロードしてモジュールを有効化します。

```csharp
await SwitchSceneAsync<LoginModule>("Login", OnSceneProgress);
await SwitchSceneAsync<BattleModule>("Battle");
```

対象シーンを Additive でロードしてから以前のモジュールを停止します。次に対象をアクティブ化し、新しいシーンモジュールを生成して旧シーンをアンロードします。同一の同時リクエストは 1 操作にまとめ、競合する対象は拒否します。ロード失敗時は旧シーン/モジュールを維持し、モジュール起動失敗時は旧シーンを復元してモジュールを再生成します。

`TritoneBootstrap` はシーンアンロード後も自動で存続します。シーンモジュール停止時にタイマー、イベント購読、ウィンドウ、プール、アセット、テーブルを解放します。シーン間で保持するデータは永続モデルモジュールに置きます。Unity シーンをロードせず論理モジュールだけを切り替える場合は `SwitchModule<TModule>()` を使用します。

## プールのクイックスタート

型や Prefab を事前登録せず、共有の遅延プールを有効にします。

```csharp
builder.UsePools();
```

`ModuleBase` から通常の C# オブジェクトを借りて返却します。

```csharp
var damageData = Rent<DamageData>();
Return(ref damageData);
```

Unity Component や GameObject Prefab も直接生成/返却できます。`ref` オーバーロードは返却に成功すると呼び出し側の参照をクリアします。初回要求時に該当する型または Prefab のプールを生成します。モジュールや `TritoneComponent` の解放時にアクティブなままのオブジェクトは自動返却されます。`UIElement` も現在有効な期間に生成したオブジェクトを閉じる際に返却します。生成/返却時の初期化処理が必要な場合だけ `IPoolable` を実装します。

## 基本サービス

必要なオプションサービスを有効化します。

```csharp
builder.UseAssets();
builder.UseAudio();
builder.UseSaves();
builder.UseSettings();
builder.UseTables();
builder.UseLocalization("en");
```

Audio はモジュールまたは Tritone Component から使用できます。Save は型付きデータを原子的に保存し、Settings は `Settings.Save()` またはアプリケーション終了までメモリに保持します。

```csharp
PlayMusic("Audio/Music/Login");
var click = PlaySound("Audio/SFX/Click");
StopSound(click);

Save("slot1", playerSave);
if (TryLoadSave<PlayerSave>("slot1", out var loaded))
    ApplySave(loaded);

Settings.SetString("Language", "ja-JP");
```

ローカライズは `Localization/{language}` から言語ごとのホット更新可能な JSON テーブルを読み込みます。`Localize("UI.Login.Title")` で取得し、`SetLanguageAsync("ja-JP")` で切り替えます。

## アセットのクイックスタート

標準の Unity Resources プロバイダーを使います。

```csharp
builder.UseAssets();
var config = LoadAsset<TextAsset>("Configs/GameConfig");
var prefab = await LoadAssetAsync<GameObject>("UI/LoginWindow");
ReleaseAsset(config); // 手動解放は任意
```

同じパスと型のロードはキャッシュを共有し、同時の非同期要求も一つのプロバイダー処理にまとまります。モジュール停止または `TritoneComponent` の破棄時に残った参照を解放します。`IAssetProvider` を実装して `UseAssets(provider)` に渡せば Addressables などに切り替えられます。

Addressables ではリモートカタログ更新や依存コンテンツの事前ダウンロードも利用できます。キャッシュ済みコンテンツはスキップされ、バイト数の進捗を通知し、各操作ハンドルを解放します。

## 設定テーブルのクイックスタート

入力、診断、拡張、CI の詳しい手順は[設定テーブル作成ガイド](Documentation~/table-authoring.md)を参照してください。`Assets/Tritone/Tables.json` を作成し `Tritone/Generate/Tables` を実行すると、定型コードを手書きせず型付き行を生成できます。

設定ディレクトリ以下の CSV/TSV を再帰的に検出します。ファイル名が生成テーブル型、相対パスが JSON パスになります。各ファイルは自己記述形式で、1 行目がフィールド名、2 行目が型、以降がデータです。先頭列は安定した主キーです。

出力確定前にフィールド型、必須セル、重複キー、重複テーブル名、競合するスキーマを検証します。セルが原因のエラーはファイル/行/列を報告します。標準型は `bool`、`int`、`long`、`float`、`double`、`string` です。`TableCompilerBuilder` を使えば、読み込み元、型、検証、コード生成、データ書き込みを個別に拡張できます。

```csharp
builder.UseAssets();
builder.UseTables();

[Serializable]
public sealed class RoleRow : ITableRow<int>
{
    public int Id;
    public string Name;
    public int Key => Id;
}

var roles = LoadTable<int, RoleRow>("Tables/Roles");
var tristin = roles.Get(1001);
```

初回ロードでデシリアライズとインデックス作成を行います。以降の要求はパース済みテーブルを共有し、同時非同期要求は合流します。主キー参照は定数時間、`GetAt(index)` は割り当てなしの元データ順走査をサポートします。重複キーはインデックス作成時に失敗します。`ReleaseTable(ref roles)` で手動解放でき、モジュール終了時には残るテーブルと TextAsset 参照を自動解放します。独自の `ITableDeserializer` を渡せば、ゲーム側のロードコードを変えずに JSON からバイナリ形式へ変更できます。

## ネットワークメッセージの生成

`Assets/Tritone/Network.json` を作成して `Tritone/Generate/Network Messages` を実行します。生成 codec は一度登録します。

```csharp
MessageSerializer serializer = new();
NetworkMessages.Register(serializer);
```

接続/ログインハンドシェイクで生成されたプロトコル記述子を交換し、ゲーム通信の前に互換性を確認します。メジャーバージョンは一致し、双方のマイナーバージョンは相手が宣言した範囲に入る必要があります。同じバージョン番号でも、ワイヤー形式の変更を検出するため SHA-256 のスキーマ指紋が一致しなければなりません。

接続後、ゲームメッセージを許可する前に自動検証できます。

```csharp
var options = new NetworkSessionOptions()
    .UseProtocolHandshake(in NetworkMessages.Protocol, timeoutSeconds: 10.0)
    .UseReconnect();
builder.UseTcpNetwork(serializer, options: options);
```

初回接続および再接続のたびにクライアントが制御フレームを送り、サーバーがプロトコルを受理した後で `ConnectAsync` が完了します。不一致では `NetworkProtocolHandshakeException` を送出して切断します。検証前のアプリケーションメッセージと要求は拒否されます。認証などの独自検証は `UseHandshake(customHandshake)` で組み合わせられます。

生成時にはメッセージ ID、名前、Request/Response 対応、フィールド型を検証します。利用可能なフィールド型は `bool`、`byte[]`、`float`、`int`、`string` です。テーブルとネットワークのスキーマをまとめて更新するには `Tritone/Generate/All` を実行します。

## ローカル AssetBundle プロバイダー

ビルド前に Bundle とアセットの登録を組み立てます。

```csharp
AssetBundleRegistry registry = new();
registry.AddBundle("core", "core.bundle")
        .AddBundle("shared", "shared.bundle", "core")
        .AddBundle("ui", "ui.bundle", "shared")
        .AddAsset("UI/LoginWindow", "ui", "Assets/Game/UI/LoginWindow.prefab");

var bundleRoot = Path.Combine(Application.persistentDataPath, "Bundles");
var source = new FileAssetBundleSource(bundleRoot);
var provider = new AssetBundleAssetProvider(registry, source);
builder.UseAssets(provider);
```

モジュール側の `LoadAssetAsync<GameObject>("UI/LoginWindow")` は変わりません。レジストリは依存先の欠落や循環を検査し、依存先を先に読み込む一意の順序を事前計算します。アセットは Bundle のロードと進行中の要求を共有します。最後のアセットを解放するとルート Bundle と依存 Bundle を逆順でアンロードします。プロバイダー作成後はレジストリを変更できません。ローカル向け `FileAssetBundleSource` に加え、`IAssetBundleSource` を実装すればランタイムモジュールやゲーム側 API を変更せずリモートソースを追加できます。

## コンテンツ更新計画とトランザクション更新

`ContentManifest` はインストール済みまたはリモートのバージョンを Bundle/アセットで表します。`ContentUpdatePlanner.CreatePlan(localManifest, remoteManifest)` はバージョン名だけに頼らず、ファイル名、ハッシュ、サイズを比較します。Bundle 名を変更しても同じファイルを再利用し、不要ファイルを別に報告し、マニフェスト順を保ってダウンロード順を決定的にします。

`UseContentAssets(options)` でリモート更新とローカル AssetBundle ロードをまとめて設定します。この場合、別途 `UseAssets` は呼びません。通常のモジュールから `StartContentUpdate` を呼ぶか、既存の非同期起動処理で `UpdateContentAsync` を await します。モジュール停止時に要求はキャンセルされます。

更新処理は同時チェックを直列化し、ダウンロードを一時ファイルに書き込み、サイズと小文字 SHA-256 を検証してからキャンセル不可のコミットに進みます。`.tritone-update` 配下にバックアップと作成マーカーを保存します。検証失敗なら稼働中ファイルは変わらず、コミット失敗なら置換/削除したファイルと前のマニフェストを復元します。コミット中にプロセスが終了した場合、次回更新時に未完了のトランザクションを復旧してからリモート確認を行います。旧アセットのロード中は更新を拒否し、トランザクション中の新規ロードを止め、全 Bundle 操作の成功後に新しいマニフェストを有効化します。

アプリケーション構築時にインストール済みマニフェストを読み込むため、リモート確認に失敗しても検証済みの既存バージョンを使えます。ディスク上の変更ではメモリにロード済みの AssetBundle/Prefab インスタンスは置き換わらないため、アセットのロード前に更新を行います。

## コンテンツビルド

Unity の Import Settings で AssetBundle 名を割り当て、`PlayerSettings.bundleVersion` を設定して `Tritone > Build > Content for Active Platform` を実行します。`ContentBuilds/<platform>/<version>` に Bundle とランタイム互換の `manifest.json` を出力します。各 Bundle には直接依存、サイズ、小文字 SHA-256 が記録され、既定ではプロジェクト内のアセットパスがアドレスになります。

ビルド自動化では明示的なアドレスと出力先を指定できます。設定したアセットがビルド済み Bundle に含まれない、出力 Bundle がない、生成マニフェストが無効といった場合、公開マニフェストの出力前に失敗します。バージョンディレクトリをそのままアップロードし、`ContentUpdateOptions` の URL を `manifest.json` に設定します。

## パッケージドキュメント

- [UI 作成と合成](Documentation~/ui-authoring.md)
- [設定テーブル作成ガイド](Documentation~/table-authoring.md)
- [パッケージ概要](Documentation~/index.md)
- [変更履歴](CHANGELOG.md)
- [ロードマップ](ROADMAP.md)

## コントリビュートとサポート

質問、バグ報告、機能提案は [GitHub Issues](https://github.com/TristinOrg/Tritone/issues) へお願いします。バグ報告には Unity のバージョンと最小の再現手順を添えてください。

Tritone は Unity Package Manager パッケージとしてメンテナンスしています。ライセンスは [LICENSE](LICENSE) を参照してください。
