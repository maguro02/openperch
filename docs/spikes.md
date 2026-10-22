# スパイク

設計の前提のうち、実際に動かさないと判断できないものを検証する作業（スパイク）の一覧と結果です。P1 の縦切りのコードを検証用のハーネスとして使い回します。

## 記録のしかた

- 「状態」は 未実施・実施中・合格・不合格 のいずれかです。
- 実施したら「結論」に、判定と、後の作業に効く事実（採用した値や手順）を短く書きます。計測値やログは `docs/test-runs/<日付>-<Phase>.md` に置き、ここからリンクします。
- 機材は [windows-test.md](windows-test.md) の WIN-D・WIN-I・VM-C を指します。
- ゲート L は VRChat SDK を使う作業についての法務確認、ゲート U はアプリの Unity を 6000.3 LTS に移すかの判定です。

## 一覧

| ID | 時期 | 内容 | 合格基準 | 状態 | 結論 |
|---|---|---|---|---|---|
| SP1 | P1 | 22f1 で書き出したバンドル（lilToon、ParticleSystem、Cloth、Perch*）を 62f3 の Mono プレイヤーで読む | エラーが 0。Perch* のフィールド値が `FixtureDump` の比較で一致する。描画が Editor の参照画像と 2/255 以内。ParticleSystem と Cloth が動く | 未実施 | ― |
| SP2 | P1 | offset を変えて `LoadFromFileAsync` する | 4096 で成功する。64 と奇数の場合の結果も記録する | 未実施 | ― |
| SP3 | P1 | UniWinC の透過（Windows 10 と Windows 11 24H2 / 25H2、OWG と Auto HDR の ON / OFF）、resizable の true / false で `SetWindowPos` の後に Screen が追従するか、2 回目の起動時のウィンドウサイズ | 全構成で透過する（OWG の対処策は文書化する）。2 回目の起動が 480x270 | 未実施 | ― |
| SP4 | P2b | Mac で書き出したバンドルと Windows で書き出したバンドルの描画差 | 差が 2/255 以下なら Mac での書き出しも可とする | 未実施 | ― |
| SP5 | P5 | `CopyAsset` で複製した StateMachineBehaviour が元から独立しているか | 元のアセットのハッシュが変わらない | 未実施 | ― |
| SP6 | P4 | macOS から Windows 向けにビルドしたとき Burst が効くか | `BurstCompiler.IsEnabled` が true で、所要時間の比が 3 以上 | 未実施 | ― |
| SP7 | P1 | PlayerLoop の順序、プレイヤーの DPI awareness | PMv2 で、挿入位置が想定どおり | 未実施 | ― |
| SP8 | P2a | 同名バンドルの衝突、非 ASCII パス | 非 ASCII のユーザー名で読み込める。駄目なら `%LOCALAPPDATA%\OpenPerch` に固定する | 未実施 | ― |
| SP9 | P1 | WS_EX_NOACTIVATE の状態で `PerchUIInputModule` を使って uGUI を操作できるか | ボタン、スライダー、ドラッグがそれぞれ 20/20 回 | 未実施 | ― |
| SP10 | P1 | NonRudeHWND の効果 | 他アプリの QUNS が BUSY にならない。タスクバーが退かない | 未実施 | ― |
| SP11 | P2b | 名前付き GrabPass を、カメラ 2 台 × アバター 2 体で同じフレームに描く | Frame Debugger で、取得がカメラごとに行われているかを確定させる | 未実施 | ― |
| SP12 | P1 | 6000.3 の Mono プレイヤーで SP1 と同じバンドルを読む | SP1 と同じ基準。結果でゲート U を判定する | 未実施 | ― |
| SP13 | P4（ゲート L の後） | Mac（vrc-get / ALCOM）で VRChat SDK、NDMF、Poiyomi が Editor で動くか | 書き出しが通る | 未実施 | ― |
| SP14 | P2a | OS フォントのフォールバック（.ttc の face を含む） | 和文・ハングル・簡体字が表示される | 未実施 | ― |
| SP15 | P2a | Mac の Editor（StandaloneOSX ターゲット）で、Win64 向けの互換性フィクスチャを `LoadFromFile` する | ロードでき、AvatarDefinition と Perch* の `FixtureDump`（反射でフィールドを列挙し、Transform の参照はルートからの相対パスにする）が期待値 JSON と一致する | 未実施 | ― |

## 不合格のときの扱い

| ID | 不合格のとき |
|---|---|
| SP1 | 2022.3.22f1〜2022.3.62f3 を二分探索し、22f1 のバンドルを読める最新の 2022.3 のパッチを採る。2022.3.48f1 より前のパッチでは出荷しない。採ったパッチが 62f2 より前なら、CVE-2025-59489 に限って Unity のパッチ手段を当てる |
| SP2 | 取り込み時にバンドル部分を切り出し、offset 0 で読む |
| SP3 | OWG で透過が壊れる場合は、文書と診断で案内する。AppCompatFlags への書き込みは、同意を得た場合だけ行う |
| SP4 | 書き出しは Windows で行う |
| SP5 | 自前の `ControllerCloner` で複製する |
| SP8 | データの保存先を `%LOCALAPPDATA%\OpenPerch` に固定する |
| SP11 | 取得結果が使い回される場合は、GrabPass を含むアバターで `Cam_B` と `Cam_W` を 1 フレームおきに描き、別のアバターの間でも共有されるなら描くフレームもずらす。それでも破綻する場合は、GrabPass の部分を不透明な暗いガラスとして扱う |
| SP12 | アプリは 2022.3.62f3 のまま進める |
| SP13 | 書き出しは WIN-D で行う |
| SP15 | 互換性フィクスチャの検証を WIN-D のプレイヤー（`--selftest compat <dir>`）で行う。Mac の PlayMode は Editor でビルドしたバンドルだけを扱う |

## 6000.3 での再実施（ゲート U）

SP12 に合格し、アプリを 6000.3 LTS に移すかを判断するときは、次のスパイクを 6000.3 のプレイヤーでもう一度行います。すべて合格したら、P2 以降のアプリを 6000.3 LTS にします。

| ID | 内容 | 状態 | 結論 |
|---|---|---|---|
| SP3 | 透過 | 未実施 | ― |
| SP7 | PlayerLoop の挿入位置と DPI awareness | 未実施 | ― |
| SP9 | ugui 2.x 上の `PerchUIInputModule` | 未実施 | ― |
