# TestDummy

OpenPerch のテストに使う人型モデルです。`build_testdummy.py` が Blender 上でメッシュ・ボーン・シェイプキー・マテリアルをすべて数値から生成し、FBX と .blend を出力します。外部のアセットは読み込みません。

## ライセンス

このディレクトリのスクリプトと、スクリプトが生成するモデル（`TestDummy.fbx`、`TestDummy.blend`）は [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/deed.ja) です。著作権を放棄しており、表記なしで自由に使えます。

## 生成のしかた

Blender 4.x が必要です。リポジトリのルートで次を実行します。

```sh
blender --background --factory-startup --python fixtures/models/TestDummy/build_testdummy.py
```

`--` の後ろにスクリプトの引数を渡せます。

| 引数 | 内容 |
|---|---|
| `--output-dir <dir>` | 出力先。既定はこのディレクトリ |
| `--no-blend` | `TestDummy.blend` を保存しない |

```sh
blender --background --factory-startup --python fixtures/models/TestDummy/build_testdummy.py -- --output-dir /tmp/testdummy --no-blend
```

終了時に、メッシュごとの三角形数（合計 約 20,000）とボーン数（48）が標準出力に出ます。生成した `TestDummy.fbx` と `TestDummy.blend` はこのディレクトリにコミットします。

## 構成

- T ポーズ。原点は足元で、頭頂は約 1.53 m です。Blender では -Y を向き、Unity に取り込むと +Z を向きます。
- 単位はメートルです。FBX は Forward -Z / Up Y、Apply Scalings = FBX All、リーフボーンなしで書き出します。

### メッシュ

すべて `Armature` でスキニングされ、Unity では SkinnedMeshRenderer になります。

| オブジェクト | 内容 | マテリアル |
|---|---|---|
| `Body` | 頭、胴、腕、脚、目、眉、口。シェイプキーを持つ | `TestDummy_Skin`、`TestDummy_Cloth`、`TestDummy_Eye`、`TestDummy_Detail` |
| `Hair` | 頭髪と髪 3 本 | `TestDummy_Hair` |
| `Skirt` | スカート | `TestDummy_Cloth` |
| `Tail` | 尻尾 | `TestDummy_Hair` |
| `Accessory_Hat` | 帽子（小物 1） | `TestDummy_Accessory` |
| `Accessory_Charm` | チャーム（小物 2） | `TestDummy_Accessory` |

マテリアルは単色で、テクスチャはありません。

### ボーン

| 種類 | ボーン |
|---|---|
| Humanoid | `Hips`、`Spine`、`Chest`、`UpperChest`、`Neck`、`Head`、`LeftEye`/`RightEye`、`LeftShoulder`/`RightShoulder`、`LeftUpperArm`/`RightUpperArm`、`LeftLowerArm`/`RightLowerArm`、`LeftHand`/`RightHand`、`LeftUpperLeg`/`RightUpperLeg`、`LeftLowerLeg`/`RightLowerLeg`、`LeftFoot`/`RightFoot`、`LeftToes`/`RightToes` |
| 髪 | `Hair_Side_L_0..2`、`Hair_Side_R_0..2`（`Head` の子）、`Hair_Back_0..3`（ポニーテール。`Head` の子） |
| スカート | `Skirt_Root`（`Hips` の子）と、その子の `Skirt_Front_0..1`、`Skirt_Back_0..1`、`Skirt_Side_L_0..1`、`Skirt_Side_R_0..1` |
| 尻尾 | `Tail_0..3`（`Hips` の子） |
| 小物 | `Charm`（`Armature` の直下。体のボーンの子ではない） |

Humanoid のボーン名は Unity の `HumanBodyBones` と同じなので、Avatar の自動割り当てで全ボーンが対応付けられます。指のボーンはありません。

揺れもの・コライダー・Constraint の付け方（`Hair_Back_0` に Root Transform を空欄にした PhysBone を直接付ける、`Charm` を Parent Constraint で体に追従させる、など）は [docs/fixtures.md](../../../docs/fixtures.md) にあります。

### シェイプキー（`Body`）

| 名前 | 内容 |
|---|---|
| `Blink` | 上まぶたを閉じる |
| `LookUp` | 上まぶたを少し上げる（上を見るとき） |
| `LookDown` | 上まぶたを少し下げる（下を見るとき） |
| `Smile` | 口角を上げる |
| `Angry` | 眉の内側を下げ、口角を下げる |
| `Surprised` | 眉を上げ、口を開け、目を見開く |

## Unity への取り込み

1. `TestDummy.fbx` を Unity プロジェクトの `Assets/` 以下にコピーします。
2. Rig タブで Animation Type を Humanoid、Avatar Definition を Create From This Model にして Apply します。Configure を開き、Humanoid のボーンがすべて割り当てられていることを確認します。
3. Materials タブでマテリアルを取り出し（Extract Materials）、シェーダーを `OpenPerch/TestToon`（`fixtures/shaders/OpenPerchTestToon/`）に替えます。色は `_Color` に引き継がれます。
