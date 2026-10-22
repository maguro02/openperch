# テスト用フィクスチャ

OpenPerch のテストと実機確認に使うデータ・プロジェクト・手順をまとめます。

## 一覧と公開の範囲

| フィクスチャ | 場所 | ライセンス | 公開 | 用途 |
|---|---|---|---|---|
| TestDummy | `fixtures/models/TestDummy/` | CC0 1.0 | する | 人型の検証アバター。揺れもの・表情・衣装の切り替え・Constraint の確認と、性能計測の A1 |
| OpenPerchTestToon | `fixtures/shaders/OpenPerchTestToon/` | MIT | する | 公開フィクスチャ用のトゥーンシェーダー |
| ExporterHarness | `fixtures/ExporterHarness/` | MIT | する | VRChat SDK を入れずにエクスポーターをテストする Unity プロジェクト |
| テストモーション | ExporterHarness の `Assets/TestMotions/`（生成物） | MIT | する（生成手順を公開） | `MotionRole` の全役割のヒューマノイドモーション |
| 互換性フィクスチャ | GitHub リリース `fixtures-v<schema>` のアセット | CC0 1.0（TestDummy）と MIT | する | 書き出したファイルをアプリが読めることの継続的な確認 |
| ExporterTestProject | `fixtures/ExporterTestProject/`（手順書だけをコミットする） | ― | 手順書だけ | VRChat SDK 入りのプロジェクトでの書き出し確認。VRChat SDK を使う作業についての法務確認（ゲート L）の後に作る |
| lilToon・Poiyomi を使うフィクスチャ、参照アバター（A2） | 各開発者の手元 | 各製品の規約 | しない | 描画の確認（W-RND）と性能計測 |

- 公開するフィクスチャは、自作の OpenPerchTestToon と TestDummy から、SDK なしの ExporterHarness で作ります。
- lilToon・Poiyomi を使うフィクスチャはローカル専用です。リポジトリにも、リリースのアセットにも入れません。
- リポジトリと配布物に入れないもの: VRChat SDK とそのアセット、lilToon・Poiyomi、アバター、購入したモーション、Mixamo のファイル、Unity Asset Store のアセット。
- 実アバターでの確認は手元だけで行い、結果は `docs/test-runs/` のチェックリストに記録します。購入したアバターや衣装のスクリーンショットは載せません。

## TestDummy

生成手順、メッシュ・ボーン・シェイプキーの一覧は [fixtures/models/TestDummy/README.md](../fixtures/models/TestDummy/README.md) にあります。

| 項目 | 状態 |
|---|---|
| `TestDummy.fbx` と `TestDummy.blend` の生成とコミット | 未実施（Blender 4.x のある環境で `build_testdummy.py` を実行する） |
| Unity の Humanoid として全ボーンが自動で割り当てられることの確認 | 未実施 |

## OpenPerchTestToon

Built-in レンダーパイプライン用のトゥーンシェーダー `OpenPerch/TestToon` です。

| パス | LightMode | 内容 |
|---|---|---|
| `FORWARD` | ForwardBase | 主光源（Directional）を段階的な影にし、環境光（SH）と頂点ライトを足す。`multi_compile_fwdbase`（ライトマップの変種は除く） |
| `OUTLINE` | ForwardBase | 法線方向に押し出した裏面でアウトラインを描く |
| `SHADOWCASTER` | ShadowCaster | 影を落とす。`multi_compile_shadowcaster` |

| プロパティ | 内容 |
|---|---|
| `_MainTex`、`_Color` | 基本色 |
| `_ShadeColor` | 影の部分で主光源に掛ける色 |
| `_ShadeThreshold`、`_ShadeSoftness` | 影の境界の位置（N·L）と幅 |
| `_AlphaTest`（キーワード `_ALPHATEST_ON`）、`_Cutoff` | Cutout。全パスで `_MainTex` の alpha × `_Color` の alpha を `_Cutoff` で切り抜く |
| `_OutlineWidth`、`_OutlineColor` | アウトラインの幅（メートル）と色 |

- 表示する画素の alpha は常に 1 を書きます。アプリは描画先の alpha から透過を求めるため、半透明の表現は扱いません。
- Cutout を有効にしたマテリアルは、Render Queue を AlphaTest（2450）にします。

## ExporterHarness

VRChat SDK を入れない Unity 2022.3.22f1 のプロジェクトです。エクスポーターの EditMode テスト（T-EXP、T-EXP-ORIG）と、公開フィクスチャの作成に使います。SDK はこのプロジェクトに入れません。

| ファイル | 内容 |
|---|---|
| `Packages/manifest.json` | `com.openperch.runtime` と `com.openperch.exporter` を `file:` で参照し、`testables` に入れる。`com.unity.test-framework` と、エクスポーターが使う組み込みモジュールを含む |
| `ProjectSettings/ProjectVersion.txt` | 2022.3.22f1（リビジョン 887be4894c44） |
| `Assets/Editor/TestSupportDefine.cs` | 読み込みのたびに、Standalone のスクリプト定義に `OPENPERCH_TEST_SUPPORT` が無ければ足す。これで `OpenPerch.Runtime.TestSupport`（`FixtureDump`）がコンパイルされる |
| `Assets/Editor/GenerateTestMotions.cs` | メニュー「OpenPerch/Generate Test Motions」 |

### 初回の準備

1. Unity Hub で 2022.3.22f1 と Windows Build Support (Mono) を入れます。
2. `fixtures/ExporterHarness` を開きます。初回はスクリプト定義の追加による再コンパイルが一度走ります。バッチモードでテストを流す前に、一度エディタで開いて再コンパイルを済ませてください。
3. エクスポーターの前提（ユーザーのプロジェクトと同じ条件）に合わせます。
   - Player Settings: Color Space を Linear にし、Windows の Graphics APIs に Direct3D11 を含めます。
   - Build Settings: 書き出しまで通す場合は Windows（x86_64）に Switch Platform します。
4. 生成した `TestDummy.fbx` と `fixtures/shaders/OpenPerchTestToon/OpenPerchTestToon.shader` を `Assets/Fixtures/` にコピーします。`Assets/Fixtures/` と `Assets/TestMotions/` はハーネスの `.gitignore` で除外しているので、コミットされません。
5. TestDummy を [README の手順](../fixtures/models/TestDummy/README.md#unity-への取り込み)で Humanoid として取り込み、マテリアルを `OpenPerch/TestToon` にします。

### テストモーションの生成

メニュー「OpenPerch/Generate Test Motions」を実行すると、`Assets/TestMotions/TestMotion_<役割>.anim` を役割ごとに 1 つ作ります。バッチモードでは `-executeMethod OpenPerch.Fixtures.ExporterHarness.GenerateTestMotions.Generate` で実行できます。既存のクリップは GUID を保ったまま上書きします。

クリップは Humanoid のマッスルカーブ（指・目・顎を除く体の全マッスル）と RootT / RootQ だけで作ります。

| 役割 | クリップ | 長さ | ループ | 内容 |
|---|---|---|---|---|
| Idle | `TestMotion_Idle` | 4.0 秒 | する | 立ち姿勢で呼吸に合わせて胸と肩が動く |
| IdleVariant | `TestMotion_IdleVariant` | 3.0 秒 | しない | 右手を挙げて振り、立ち姿勢に戻る |
| Walk | `TestMotion_Walk` | 1.0 秒 | する | 歩行。前進（1 周期で 0.6 × human scale）をルートモーションとして持つ |
| SitOnEdge | `TestMotion_SitOnEdge` | 3.0 秒 | する | 縁に腰掛け、脚を交互に揺らす。太ももは根元から椅子の座面ほどの高さ |
| Dragged | `TestMotion_Dragged` | 1.6 秒 | する | 腕を上げ、脚がぶら下がって揺れる |
| Fall | `TestMotion_Fall` | 0.8 秒 | する | 腕を上げてばたつかせ、膝を曲げる |
| Land | `TestMotion_Land` | 0.6 秒 | しない | 着地でしゃがみ、立ち姿勢に戻る |
| SitOnFloor | `TestMotion_SitOnFloor` | 4.0 秒 | する | 床に脚を伸ばして座り、後ろに手をつく |

| 項目 | 状態 |
|---|---|
| 8 役割のクリップが生成され、`humanMotion` が true になる | 未確認 |
| TestDummy に当てて、各役割の姿勢が上の表どおりに見える | 未確認 |

### テストの実行

`tools/run-unity-tests.sh` で、ハーネスの EditMode テスト（`testables` に入れた runtime と exporter のテスト）を流します。エディタの Test Runner からも実行できます。

## 検証アバターの構成手順

TestDummy に次のコンポーネントとアニメーションを設定して、検証アバターにします。

- ExporterHarness（SDK なし）では、揺れもの・コライダー・Constraint・Contact を Perch* のコンポーネント（`PerchPhysBone`、`PerchPhysBoneCollider`、`PerchConstraint`、`PerchContactData`）で手で付けます。
- SDK 入りのプロジェクト（ExporterTestProject。ゲート L の後）では、同じ配置で VRChat のコンポーネントを付け、アバター記述子・Expressions・FX を設定して、エクスポーターで書き出します。

### 揺れもの・コライダー・Constraint・Contact

`Dynamics` は、アバターのルートの直下に作る空の GameObject です。1 つの GameObject に PhysBone 3 個とコライダー 2 個が付く構成を兼ねます。

| ID | コンポーネント | 付ける GameObject | Root / Target Transform | 主な設定 | 確かめること |
|---|---|---|---|---|---|
| PB1 | PhysBone（ポニーテール） | `Hair_Back_0` | 空欄 | Pull 0.2、Spring 0.3、Gravity 0.3、Radius 0.02、Colliders = C1 | 空欄の Root Transform は、付けた GameObject から始まる |
| PB2 | PhysBone（横髪・左） | `Dynamics` | `Hair_Side_L_0` | Pull 0.2、Gravity 0.2、Colliders = C1 | 1 つの GameObject に複数の PhysBone |
| PB3 | PhysBone（横髪・右） | `Dynamics` | `Hair_Side_R_0` | PB2 と同じ | 同上 |
| PB4 | PhysBone（スカート） | `Skirt_Root` | 空欄 | Multi Child Type = Ignore、Colliders = C2、C3 | 子が複数あるチェーン |
| PB5 | PhysBone（尻尾） | `Dynamics` | `Tail_0` | Limit Type = Angle（45°）、Gravity 0.2 | 角度の制限 |
| C1 | Collider（Sphere） | `Head` | 空欄 | 頭全体を覆う大きさと位置 | ボーンに直接付けたコライダー |
| C2 | Collider（Capsule） | `Dynamics` | `LeftUpperLeg` | 太ももを覆う半径と高さ | 1 つの GameObject に複数のコライダー |
| C3 | Collider（Capsule） | `Dynamics` | `RightUpperLeg` | C2 と同じ | 同上 |
| CN1 | Parent Constraint | `Charm` | 空欄 | Source 0 = `Hips`（Weight 1）、Source 1 = `RightHand`（Weight 0）。Offset は現在の位置を保つ値にする | 空欄の Target Transform は自分に作用する。Source の Weight のカーブで追従先が切り替わる |
| CT1 | Contact Receiver（Sphere） | `Head` | 空欄 | Radius 0.1、Parameter = `HeadPat`、Collision Tags = `Hand` | データだけ残り、`HeadPat` は既定値のまま |

`Accessory_Hat` と `Accessory_Charm` の GameObject は、初期状態で非アクティブにします。

### アバター記述子（SDK 入りのプロジェクト）

| 項目 | 設定 |
|---|---|
| View Position | 両目の間 |
| Eye Look | 有効。Left Eye = `LeftEye`、Right Eye = `RightEye` |
| Eyelids | Blendshapes。Mesh = `Body`、Blink = `Blink`、Looking Up = `LookUp`、Looking Down = `LookDown` |
| Lip Sync | 使わない |
| Playable Layers | FX だけをカスタムにし、ほかは既定のまま |

### Expression Parameters

| 名前 | 型 | 既定値 | Saved | Synced | 用途 |
|---|---|---|---|---|---|
| `Outfit` | Int | 0 | する | する | 衣装セレクタ（0: なし、1: 帽子、2: チャーム、3: 両方） |
| `HatSize` | Float | 0.5 | する | する | 帽子の大きさ（Radial） |
| `Face` | Int | 0 | しない | する | 表情（0: 通常、1: 笑顔、2: 怒り、3: 驚き） |
| `CharmInHand` | Bool | false | する | する | チャームを右手に持つ |
| `Party` | Bool | false | しない | する | Driver の連鎖の起点（Button） |
| `PartyStep` | Int | 0 | しない | しない | Driver の連鎖の中間値 |

### Expressions Menu

ルートのページ:

| ID | 名前 | 種類 | パラメータ | 値 | アイコン |
|---|---|---|---|---|---|
| M1 | 帽子 | Toggle | `Outfit` | 1 | SDK に同梱のアイコン |
| M2 | チャーム | Toggle | `Outfit` | 2 | なし |
| M3 | 両方 | Toggle | `Outfit` | 3 | なし |
| M4 | 帽子の大きさ | Radial Puppet | サブパラメータ `HatSize` | ― | SDK に同梱のアイコン |
| M5 | 表情 | SubMenu | なし | ― | なし |
| M6 | チャームを手に | Toggle | `CharmInHand` | 1 | なし |
| M7 | パーティー | Button | `Party` | 1 | なし |

M5 のページ:

| ID | 名前 | 種類 | パラメータ | 値 |
|---|---|---|---|---|
| M8 | 笑顔 | Toggle | `Face` | 1 |
| M9 | 怒り | Toggle | `Face` | 2 |
| M10 | 驚き | Toggle | `Face` | 3 |

M1〜M3 が 1 つの Int を共有する衣装セレクタです。

### FX

Write Defaults は全ステートで OFF に揃えます。

| ID | レイヤー | マスク | 内容 |
|---|---|---|---|
| L1 | Outfit | なし | `Outfit` の値（0〜3）ごとのステート。各クリップで `Accessory_Hat` と `Accessory_Charm` の `m_IsActive` を設定する |
| L2 | HatSize | なし | 1D BlendTree（`HatSize` = 0 で帽子の Scale 0.5、1 で 1.5） |
| L3 | Face | なし | `Face` の値ごとのステート。各クリップで `Body` のシェイプキー（`Smile`、`Angry`、`Surprised`、`Blink`）を設定する。`Face` = 3 のステートに TrackingControl（Eyes = Animation）を付け、クリップで `Blink` を 0 にする。ほかのステートには TrackingControl（Eyes = Tracking）を付ける |
| L4 | Charm | なし | `CharmInHand` で、CN1 の Source 0 と Source 1 の Weight を（1, 0）と（0, 1）の間で切り替える |
| L5 | Party | なし | `Party` が true になったら、ParameterDriver（Set `Outfit` = 3、Set `Face` = 1、Set `PartyStep` = 1）を持つステートへ移る。`Party` が false で戻る |
| L6 | PartyChain | なし | `PartyStep` = 1 になったら、ParameterDriver（Add `HatSize` +0.25、Set `PartyStep` = 0）を持つステートへ移る。`PartyStep` = 0 で戻る |
| L7 | GestureFace | SDK に同梱の AvatarMask | `GestureLeft` = 1（Fist）のステートで `Angry` を 100 にする。それ以外のステートは空のクリップ |

L5 の Driver が L6 を動かすことで、Driver の連鎖になります。

### Modular Avatar での衣装の統合

1. `TestDummy.fbx` をもう 1 体シーンに置いて Unpack し、`Skirt` 以外のメッシュを削除して、衣装用の Prefab にします。
2. 衣装用の Prefab をアバターのルートの子に置き、その `Armature` に MA Merge Armature を付けます。統合先はアバターの `Armature` です。
3. 衣装側の `Skirt_Root` に PB4 と同じ設定の PhysBone を付け、アバター本体の `Skirt` は非アクティブにします。

## 期待結果

### 操作 → パラメータ値 → 見た目

| ID | 操作 | パラメータ値 | 見た目 | 状態 |
|---|---|---|---|---|
| E1 | 初めて配置する（saved の値なし） | `Outfit` = 0、`HatSize` = 0.5、`Face` = 0、`CharmInHand` = false | 帽子とチャームが非表示。通常の表情で、2〜6 秒ごとにまばたきする | 未確認 |
| E2 | M1「帽子」を ON | `Outfit` = 1 | 帽子だけを表示。M1 が ON、M2・M3 が OFF の表示 | 未確認 |
| E3 | M2「チャーム」を ON | `Outfit` = 2 | チャームだけを表示（右の腰）。M1 が OFF の表示に変わる | 未確認 |
| E4 | M3「両方」を ON | `Outfit` = 3 | 帽子とチャームを表示 | 未確認 |
| E5 | ON の M3 をもう一度押す | `Outfit` = 0 | 帽子とチャームが非表示 | 未確認 |
| E6 | M4「帽子の大きさ」を 0、0.5、1 にする | `HatSize` = 0、0.5、1 | 帽子の大きさが 0.5 倍、1 倍、1.5 倍 | 未確認 |
| E7 | M5「表情」→ M8「笑顔」 | `Face` = 1 | 口角が上がる。まばたきは続く | 未確認 |
| E8 | M9「怒り」 | `Face` = 2 | 眉の内側と口角が下がる | 未確認 |
| E9 | M10「驚き」 | `Face` = 3 | 眉が上がって口が開き、目を見開く。まばたきが止まる | 未確認 |
| E10 | M10 を OFF | `Face` = 0 | 通常の表情に戻り、まばたきが再開する | 未確認 |
| E11 | M6「チャームを手に」を ON、OFF | `CharmInHand` = true、false | チャームが右手に移り、OFF で右の腰に戻る | 未確認 |
| E12 | M7「パーティー」を押す | `Party` = 1（0.6 秒後に 0）。続けて `Outfit` = 3、`Face` = 1、`PartyStep` = 1。さらに `HatSize` が 0.25 増え（上限 1）、`PartyStep` = 0 | 帽子とチャームを表示し、笑顔になり、帽子が一段大きくなる | 未確認 |
| E13 | ジェスチャーで左手を Fist にする | `GestureLeft` = 1、`GestureWeight` = 1 | 眉の内側と口角が下がる | 未確認 |
| E14 | アプリを再起動する | `Outfit`、`HatSize`、`CharmInHand` が終了前の値。`Face` = 0 | saved の値どおりの衣装と帽子の大きさ | 未確認 |
| E15 | アバターをドラッグして振る | ― | PB1〜PB5 が揺れ、ポニーテールと横髪が頭（C1）を、スカートが太もも（C2、C3）を突き抜けない。尻尾は 45° を超えて曲がらない | 未確認 |

### 書き出し時（SDK 入りのプロジェクト）

| ID | 対象 | 期待する結果 | 状態 |
|---|---|---|---|
| X1 | SDK のアイコンを使う M1、M4 | アイコンが null になり、警告にコントロール名が出る | 未確認 |
| X2 | SDK のマスクを使う L7 | `tempAssetDir` に作った AvatarMask に差し替わり、書き出したコントローラーが SDK のアセットを参照しない。E13 の見た目は変わらない | 未確認 |
| X3 | ParameterDriver（L5、L6）と TrackingControl（L3） | Perch の StateMachineBehaviour に置き換わる | 未確認 |
| X4 | L4 のクリップ | Source の Weight のカーブが `PerchConstraint` の `sourceN.weight` に付け替わる | 未確認 |
| X5 | PB1、PB4、C1、CN1、CT1（空欄の Root / Target Transform） | 書き出し後、自分の Transform が明示的に入っている | 未確認 |
| X6 | MA で統合した衣装 | 統合後のスカートと PhysBone が含まれ、レポートにエラーが無い | 未確認 |
| X7 | CT1 | `PerchContactData` として無効の状態で残る | 未確認 |

## 互換性フィクスチャ

アプリが過去のエクスポーターの出力を読めることを、公開したファイルで確かめます。

- **作り方**: ExporterHarness で、TestDummy（OpenPerchTestToon のマテリアルと Perch* のコンポーネント付き）とテストモーションを書き出します。同時に `FixtureDump`（`OpenPerch.Runtime.TestSupport`）で、AvatarDefinition と Perch* のフィールドを期待値 JSON に書き出します。Transform の参照はルートからの相対パスで表します。
- **公開**: GitHub リリース `fixtures-v<schema>`（`<schema>` は `RuntimeSchema.Current`）のアセットとして、`.opk` と期待値 JSON を添付します。
- **検証**: SP15 に合格した場合は、Mac の PlayMode テストで `gh release download` したファイルをロードし、`FixtureDump` の結果を期待値 JSON と比べます。Windows 向けのシェーダーはピンクのままでよく、シェーダーのエラーは `LogAssert` で無視します。SP15 が不合格なら、WIN-D のプレイヤーで `--selftest compat <dir>` を実行し、結果の JSON を `docs/test-runs/` に記録します。

| 項目 | 状態 |
|---|---|
| `fixtures-v1` の作成と公開 | 未実施 |
| 検証の経路（Mac の PlayMode か WIN-D の `--selftest`） | SP15 の結論待ち（[docs/spikes.md](spikes.md)） |
