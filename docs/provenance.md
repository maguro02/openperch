# 根拠の記録（provenance）

runtime パッケージのフィールド、VRChat コンポーネントからの写像表、PhysBone の近似ソルバーの挙動について、何を根拠に決めたかを記録します。フィールドや写像を追加・変更する PR では、この文書の該当する行も更新します。

## 逆コンパイルの禁止

> **VRChat SDK を逆コンパイルしてはいけません。** VRChat SDK License は逆コンパイルを禁止しています。SDK のアセンブリを IDE で開いたときに逆コンパイル結果が表示されないよう、[CONTRIBUTING.md](../CONTRIBUTING.md#vrchat-sdk-の扱い) の手順で設定を無効にしてください。逆コンパイルした内容を見た場合は、その内容に基づく変更をしないでください。

- PhysBone の実装は、公開ドキュメントと観察だけを根拠にします。観察の方法は、人が見る・録画する、または VRChat が公式に提供するインターフェース（OSC）に限ります。
- SDK の出力を正解として合わせ込むことはしません。
- GPL / AGPL のプロジェクト（MateEngine など）のコードは見ません。MPL-2.0 と Apache-2.0 のプロジェクトは考え方の参照だけにとどめます。

## 根拠の種類

| 記号 | 種類 | 記録すること |
|---|---|---|
| DOC | 公式ドキュメント（[VRChat Creator Docs](https://creators.vrchat.com/)） | ページの URL と、参照した日付 |
| SO | SerializedObject の走査（下記） | SDK（`com.vrchat.avatars`）の版、Unity の版、走査した日付 |
| OBS | 観察（目視・録画・OSC） | 手順、使ったアバターの構成、VRChat クライアントの版、録画の保存先（リポジトリには入れない） |
| SRC | MIT で公開されているソース（NDMF、Avatar Optimizer など） | リポジトリの URL、コミットハッシュ、ファイルのパス |

## SerializedObject の走査

VRChat 側のシリアライズ名は、SDK 入りのプロジェクトで、VRChat SDK を使う作業についての法務確認（ゲート L）が済んだ後に、各コンポーネントを `SerializedObject` で走査して集めます。対象は VRCPhysBone、VRCPhysBoneCollider、各 VRC Constraint、VRCContactSender / VRCContactReceiver、アバター記述子です。走査は Unity の公開 API だけで行い、SDK の型を参照するコードは書きません。

次のスクリプトを、SDK 入りのプロジェクトの `Assets/Editor/DumpSerializedProperties.cs` として保存します（UnityEditor の API を使うので、Editor フォルダに置きます）。対象の GameObject を選び、メニューの Tools → Dump Serialized Properties を実行すると、付いているコンポーネントごとのプロパティのパスと型が Console に出ます。

```csharp
using System.Text;
using UnityEditor;
using UnityEngine;

public static class DumpSerializedProperties
{
    [MenuItem("Tools/Dump Serialized Properties")]
    private static void Dump()
    {
        if (Selection.activeGameObject == null) return;
        var text = new StringBuilder();
        foreach (Component component in Selection.activeGameObject.GetComponents<Component>())
        {
            // GetComponents returns null in place of a missing script, which avatars often still carry.
            if (component == null)
            {
                text.AppendLine("## (Missing Script)");
                continue;
            }

            text.AppendLine("## " + component.GetType().FullName);
            using (var serializedObject = new SerializedObject(component))
            {
                SerializedProperty iterator = serializedObject.GetIterator();
                for (bool enter = true; iterator.Next(enter); enter = iterator.propertyType == SerializedPropertyType.Generic)
                {
                    text.AppendLine(iterator.propertyPath + " : " + iterator.propertyType);
                }
            }
        }
        Debug.Log(text.ToString());
    }
}
```

結果は下の表の「VRChat 側」に書き、runtime の型（`PerchPhysBone` など）のフィールドと照合します。

## runtime のフィールド

「状態」は 未確認・確認済み・差異あり のいずれかです。差異があれば、runtime 側を直すか（v1.0 の公開前に限る）、写像で吸収するかを備考に書きます。

### PerchPhysBone

| フィールド | VRChat 側 | 根拠 | 状態 | 備考 |
|---|---|---|---|---|
| `version`、`integrationType` | | | 未確認 | |
| `rootTransform` | | | 未確認 | 空欄はコンポーネント自身の Transform |
| `ignoreTransforms`、`ignoreOtherPhysBones` | | | 未確認 | |
| `endpointPosition`、`multiChildType` | | | 未確認 | |
| `pull`、`pullCurve` | | | 未確認 | |
| `spring`、`springCurve` | | | 未確認 | Advanced では Momentum |
| `stiffness`、`stiffnessCurve` | | | 未確認 | |
| `gravity`、`gravityCurve`、`gravityFalloff`、`gravityFalloffCurve` | | | 未確認 | |
| `immobileType`、`immobile`、`immobileCurve` | | | 未確認 | |
| `limitType`、`maxAngleX`、`maxAngleXCurve`、`maxAngleZ`、`maxAngleZCurve` | | | 未確認 | |
| `limitRotation`、`limitRotationXCurve`、`limitRotationYCurve`、`limitRotationZCurve` | | | 未確認 | |
| `radius`、`radiusCurve` | | | 未確認 | |
| `allowCollision`、`collisionAllowSelf`、`collisionAllowOthers`、`colliders` | | | 未確認 | |
| `stretchMotion`、`stretchMotionCurve`、`maxStretch`、`maxStretchCurve`、`maxSquish`、`maxSquishCurve` | | | 未確認 | |
| `allowGrabbing`、`grabAllowSelf`、`grabAllowOthers`、`grabMovement`、`snapToHand` | | | 未確認 | v1 では保持するだけ |
| `allowPosing`、`poseAllowSelf`、`poseAllowOthers` | | | 未確認 | v1 では保持するだけ |
| `parameter`、`isAnimated`、`resetWhenDisabled` | | | 未確認 | |

### PerchPhysBoneCollider

| フィールド | VRChat 側 | 根拠 | 状態 | 備考 |
|---|---|---|---|---|
| `rootTransform` | | | 未確認 | 空欄はコンポーネント自身の Transform |
| `shapeType`、`radius`、`height`、`position`、`rotation` | | | 未確認 | |
| `insideBounds`、`bonesAsSphere` | | | 未確認 | |
| `isGlobal` | | | 未確認 | SDK の global 指定（3.10.4 以降）と、記述子の手・指のコライダー |

### PerchConstraint

| フィールド | VRChat 側 | 根拠 | 状態 | 備考 |
|---|---|---|---|---|
| `kind` | | | 未確認 | Position / Rotation / Scale / Parent / Aim / LookAt の各コンポーネント |
| `isActive`、`globalWeight`、`locked` | | | 未確認 | |
| `targetTransform` | | | 未確認 | 空欄はコンポーネント自身の Transform |
| `solveInLocalSpace`、`freezeToWorld` | | | 未確認 | |
| `affectsPositionX/Y/Z`、`affectsRotationX/Y/Z`、`affectsScaleX/Y/Z` | | | 未確認 | |
| `positionAtRest`、`positionOffset`、`rotationAtRest`、`rotationOffset`、`scaleAtRest`、`scaleOffset` | | | 未確認 | |
| `aimAxis`、`upAxis`、`worldUpVector`、`worldUpType`、`worldUpTransform`、`roll`、`useUpTransform` | | | 未確認 | |
| `sourceCount`、`source0`〜`source15`（`source`、`weight`、`parentPositionOffset`、`parentRotationOffset`） | | | 未確認 | |

### PerchContactData

| フィールド | VRChat 側 | 根拠 | 状態 | 備考 |
|---|---|---|---|---|
| `isReceiver` | | | 未確認 | Sender と Receiver の区別 |
| `shape`、`rootTransform`、`radius`、`height`、`position`、`size`、`rotation` | | | 未確認 | |
| `collisionTags`、`receiverType`、`parameter` | | | 未確認 | |
| `allowSelf`、`allowOthers`、`localOnly` | | | 未確認 | |

### AvatarDefinition（アバター記述子から）

| フィールド | VRChat 側 | 根拠 | 状態 | 備考 |
|---|---|---|---|---|
| `viewPositionLocal` | | | 未確認 | |
| `eyes`（`EyeSettings` の各フィールド） | | | 未確認 | 瞼のシェイプキーは索引ではなく名前で持つ |
| `parameters`（`ExpressionParameterDef` の各フィールド） | | | 未確認 | |
| `menuPages`（`MenuControlDef` の各フィールド） | | | 未確認 | |
| `fxController` | | | 未確認 | |

### StateMachineBehaviour

| 型 | VRChat 側 | 根拠 | 状態 | 備考 |
|---|---|---|---|---|
| `PerchParameterDriver`（`localOnly`、`DriverEntry` の各フィールド） | | | 未確認 | |
| `PerchTrackingControl`（`eyes`、`mouth`） | | | 未確認 | |
| `PerchLayerControl`（`playable`、`layer`、`goalWeight`、`blendDuration`） | | | 未確認 | |
| `PerchPlayableLayerControl`（`layer`、`goalWeight`、`blendDuration`） | | | 未確認 | |
| `PerchLocomotionControl`（`disableLocomotion`） | | | 未確認 | |

## バインディングの写像

アニメーションクリップのバインディングを VRChat の型から Perch* の型へ付け替える表（`BindingRemap`）の根拠です。写像に無い VRChat の型のカーブは削除し、件数を警告します。

| 元の型 | 元のプロパティ | 先の型 | 先のプロパティ | 根拠 | 状態 |
|---|---|---|---|---|---|
| VRCPhysBone | `m_Enabled` | `PerchPhysBone` | `m_Enabled` | | 未確認 |
| VRCPhysBoneCollider | `m_Enabled` | `PerchPhysBoneCollider` | `m_Enabled` | | 未確認 |
| VRC*Constraint | `m_Enabled` | `PerchConstraint` | `m_Enabled` | | 未確認 |
| VRC*Constraint | IsActive | `PerchConstraint` | `isActive` | | 未確認 |
| VRC*Constraint | GlobalWeight | `PerchConstraint` | `globalWeight` | | 未確認 |
| VRC*Constraint | 各 source の Weight | `PerchConstraint` | `sourceN.weight` | | 未確認 |
| VRC*Constraint | 各 source の ParentPositionOffset / ParentRotationOffset | `PerchConstraint` | `sourceN.parentPositionOffset` / `sourceN.parentRotationOffset` | | 未確認 |
| VRC*Constraint | FreezeToWorld | `PerchConstraint` | `freezeToWorld` | | 未確認 |
| VRCContactSender / VRCContactReceiver | `m_Enabled` | `PerchContactData` | `m_Enabled` | | 未確認 |

## PhysBone の挙動

近似ソルバーで再現する挙動ごとに、根拠になった記述や観察を記録します。

| 挙動 | 根拠 | 確かめ方 | 状態 |
|---|---|---|---|
| Root Transform が空欄のとき、コンポーネントの GameObject から始まる | | | 未確認 |
| Pull・Spring（Momentum）・Stiffness の効き方 | | | 未確認 |
| Gravity と Gravity Falloff（1.1 の休止方向からの回転） | | | 未確認 |
| Immobile の AllMotion と World | | | 未確認 |
| Limit の Angle・Hinge・Polar | | | 未確認 |
| Stretch Motion・Max Stretch・Max Squish | | | 未確認 |
| Multi Child Type と Endpoint Position | | | 未確認 |
| コライダーの Inside Bounds と Bones As Sphere | | | 未確認 |
| global のコライダーと Allow Collision（Other のときの Self / Others） | | | 未確認 |
| 同じパスに同じ型が複数あるとき、`m_Enabled` のカーブが先頭の 1 個にだけ効く | | | 未確認 |
