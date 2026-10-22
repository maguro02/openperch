# `.opk` 形式の仕様

`.opk` は、エクスポーターが書き出し、アプリが取り込むファイルの形式です。
1 つのファイルに、ヘッダ、manifest（UTF-8 の JSON）、サムネイル（PNG、任意）、Unity の AssetBundle をこの順に格納します。

- 整数はすべてリトルエンディアンです。
- 暗号化はしません。中身は誰でも取り出せます。
- CRC-32 と SHA-256 で検出できるのは破損だけです。改ざんは防げません。
- 読み書きと検証の実装は `packages/com.openperch.runtime/Format`（名前空間 `OpenPerch.Format`）、JSON との変換は `packages/com.openperch.runtime/FormatJson`（`OpenPerch.Format.Json.ManifestJson`）にあります。
- 検証に失敗したときのメッセージコードと対処は [troubleshooting.md](troubleshooting.md) にあります。

## 1. コンテナ

### 1.1 ヘッダ（64 バイト）

| オフセット | 型 | 名前 | 規則 |
|---|---|---|---|
| 0 | byte[8] | magic | `89 4F 50 4B 0D 0A 1A 0A` |
| 8 | u16 | containerVersion | 1 |
| 10 | u16 | flags | 0 |
| 12 | u32 | manifestLength | 1〜262144 |
| 16 | u64 | manifestOffset | 64 |
| 24 | u64 | thumbnailOffset | thumbnailLength と両方 0（サムネイル無し）か、両方 0 以外 |
| 32 | u32 | thumbnailLength | 2 MiB（2097152）以下 |
| 36 | u32 | reserved0 | 0 |
| 40 | u64 | bundleOffset | 4096 の倍数 |
| 48 | u64 | bundleLength | 1 以上 2^31 未満 |
| 56 | u32 | headerCrc32 | バイト 0〜55 の CRC-32（IEEE 802.3、zlib や PNG と同じもの） |
| 60 | u32 | reserved1 | 0 |

### 1.2 配置

```
0        64                64+manifestLength
| header | manifest        | thumbnail（任意） | 0 埋め | bundle |
                                                      ^ bundleOffset（4096 の倍数）。bundle はファイルの末尾で終わる
```

- manifest は 64 バイト目から始まります。
- サムネイルを入れる場合は、manifest より後ろ、バンドルより前に置きます。エクスポーターは manifest の直後に置きます。
- バンドルは 4096 の倍数の位置から始まり、ファイルの末尾で終わります。ファイルの長さは `bundleOffset + bundleLength` と等しくなければなりません。
- 区間の間の隙間は 0 で埋めます。読み手は隙間の中身を見ません。
- 区間は重なってはならず、manifest、サムネイル、バンドルの順に並びます。

### 1.3 ヘッダの検査

ヘッダは次の順に検査し、最初に違反した規則のエラーで拒否します。

| 順 | 検査 | エラー（`PackageError`） |
|---|---|---|
| 1 | 先頭 8 バイトが magic と一致する（ファイルが 8 バイト未満のときも不一致とする） | BadMagic |
| 2 | ファイルが 64 バイト以上ある | LengthMismatch |
| 3 | containerVersion が 1 | UnsupportedContainer |
| 4 | headerCrc32 がバイト 0〜55 の CRC-32 と一致する | BadHeaderCrc |
| 5 | flags が 0 | BadFlags |
| 6 | reserved0 と reserved1 が 0 | BadReserved |
| 7 | manifestOffset が 64 | BadManifestOffset |
| 8 | manifestLength が 0 でない／262144 以下 | OutOfRange／ManifestTooLarge |
| 9 | サムネイルの 2 項目が両方 0 か両方 0 以外で、長さが 2 MiB 以下 | BadThumbnailFields |
| 10 | bundleLength が 1 以上 2^31 未満 | OutOfRange |
| 11 | bundleOffset が 4096 の倍数 | Misaligned |
| 12 | manifest・サムネイル・バンドルがファイルの中にある | OutOfRange |
| 13 | 区間が重ならず、manifest、サムネイル、バンドルの順に並ぶ | Overlap |
| 14 | ファイルの長さが `bundleOffset + bundleLength` と等しい | LengthMismatch |

- containerVersion を CRC より先に調べるのは、別の containerVersion ではヘッダの構造（CRC の位置を含む）が違いうるためです。
- u64 の値は符号なしのまま扱います。区間がファイルの中にあるかは `off <= fileLength && len <= fileLength - off` の形で、加算ではなく引き算で判定し、桁あふれで判定をすり抜けられないようにします。
- 読み込みの途中でファイルが終わった場合も LengthMismatch とします。

### 1.4 サムネイル

- PNG です。エクスポーターは 256x256 で出力します。
- アプリは画像を展開する前に IHDR チャンクを読み、次をすべて満たすものだけを使います。
  - 幅と高さが 1〜512
  - ビット深度が 8
  - カラータイプが 2（RGB）か 6（RGBA）
  - インターレースなし
- IHDR の読み取りでは、PNG のシグネチャ、チャンクの長さ（13）、チャンクの型、チャンクの CRC も確かめます。
- 条件を満たさないサムネイルは、無いものとして扱います。パッケージは拒否せず、警告 `W_THUMB_REJECTED` を出します。
- 検証の結果（`VerifyResult`）の `ThumbnailOk` は、サムネイルの無いパッケージ（thumbnailLength が 0）でも false です。`W_THUMB_REJECTED` を出すのは、サムネイルの検査まで進み、`Header.ThumbnailLength != 0 && !ThumbnailOk` のときだけです（`MessageCodes.ForThumbnail`）。サムネイルの無いパッケージには出しません。

### 1.5 バンドルの読み込み

アプリは `AssetBundle.LoadFromFileAsync(path, crc, bundleOffset)` で、ファイルの中のバンドルを直接読みます。展開やコピーはしません。`crc` には manifest の `bundle.crc` を渡します。

## 2. manifest

### 2.1 例

```json
{
  "formatVersion": 1,
  "kind": "avatar",
  "exportId": "3f2c9a0d6b7e4c1fa2b3c4d5e6f70812",
  "lineageId": "9a1b3c5d7e9f4a2b8c6d0e1f2a3b4c5d",
  "displayName": "MyAvatar",
  "createdAtUtc": "2026-10-22T10:00:00Z",
  "exporter": {
    "version": "1.0.0",
    "unityVersion": "2022.3.22f1",
    "buildTarget": "StandaloneWindows64",
    "colorSpace": "Linear",
    "vrcSdkVersion": "3.10.5",
    "tools": ["nadena.dev.ndmf@1.15.0"]
  },
  "runtimeSchemaVersion": 1,
  "bundle": {
    "name": "op-3f2c9a0d6b7e4c1fa2b3c4d5e6f70812",
    "crc": 2864434397,
    "sha256": "<64 桁の小文字の 16 進数>",
    "length": 123456789,
    "rootAsset": "definition"
  },
  "features": { "fx": true, "menuPages": 5, "physBones": 14, "constraints": 3, "contacts": 2, "eyeLook": true, "blink": true },
  "compositing": { "requiresPrecise": true, "reasons": ["additive:1", "grabpass:1"] },
  "stats": {
    "triangles": 69000, "materials": 18, "skinnedMeshes": 6, "physBoneTransforms": 220,
    "estimatedTextureBytes": 412000000, "estimatedMeshBytes": 38000000
  },
  "warnings": [{ "code": "W_REMOVED_COMPONENT", "detail": "VRCSpatialAudioSource x2" }],
  "motions": []
}
```

### 2.2 項目

「必須」の項目は、値が 0 や空の文字列のときも欠落（`MissingField`）として扱います。JsonUtility は JSON に無いキーを 0 や空の文字列で埋めるため、両者を区別できないからです。

| キー | 型 | 必須 | 規則 |
|---|---|---|---|
| `formatVersion` | int | 必須 | 1 以上。形式に依らない項目 |
| `runtimeSchemaVersion` | int | 必須 | 1 以上。形式に依らない項目 |
| `exporter.unityVersion` | string | 必須 | Unity のバージョンとして解釈できる（3.3 節）。形式に依らない項目 |
| `exporter.version` | string | 必須 | SemVer（3.4 節）。形式に依らない項目 |
| `exporter.buildTarget` | string | 必須 | 空でない。形式に依らない項目 |
| `exporter.colorSpace` | string | 必須 | 空でない。形式に依らない項目 |
| `exporter.vrcSdkVersion` | string | 任意 | VRChat SDK を通さない書き出しでは空 |
| `exporter.tools` | string[] | 任意 | 書き出しに関わったパッケージ（`id@version`） |
| `kind` | string | 必須 | `avatar` か `motion` |
| `exportId` | string | 必須 | `^[0-9a-f]{32}$`。書き出しごとの GUID |
| `lineageId` | string | 必須 | `^[0-9a-f]{32}$`。同じアバターの書き出しで共通の GUID |
| `displayName` | string | 任意 | 128 文字以下 |
| `createdAtUtc` | string | 任意 | ISO 8601 の UTC 時刻 |
| `bundle.name` | string | 必須 | `"op-" + exportId` |
| `bundle.crc` | uint | 必須 | Unity が報告したバンドルの CRC。0 は不可（Unity は 0 を「検査しない」と解釈するため） |
| `bundle.sha256` | string | 必須 | `^[0-9a-f]{64}$`。バンドルの区間だけの SHA-256 |
| `bundle.length` | long | 必須 | ヘッダの bundleLength と等しい |
| `bundle.rootAsset` | string | 必須 | `kind` が `avatar` なら `definition`、`motion` なら `motionpack` |
| `features.*` | bool / int | 任意 | 数値は 0 以上 |
| `compositing.requiresPrecise` | bool | 任意 | |
| `compositing.reasons` | string[] | 任意 | `理由:件数` |
| `stats.*` | int / long | 任意 | 0 以上 |
| `warnings` | array | 任意 | 128 件以下。各要素は `code`（メッセージコード）と `detail`（256 文字以下） |
| `motions` | array | 任意 | モーションパックのクリップ。各要素は `id`、`role`（`MotionRole` の名前）、`sourceName`、`length`（秒、0 以上）、`loop`、`hasRootMotion` |

- 文字数は UTF-16 のコード単位で数えます。
- JSON の先頭の UTF-8 BOM は読み飛ばします。未知のキーは無視します。
- JSON として読めない場合や、全体がオブジェクトでない場合は `JsonInvalid` です。
- `features`、`stats`、`compositing` は、表示と既定値の決定にだけ使います。メモリを確保する量や、受け付けるかどうかの判定には使いません。

### 2.3 形式に依らない項目

次の 6 項目は、**今後のどの formatVersion でも、名前と意味を変えません。**

- `formatVersion`
- `runtimeSchemaVersion`
- `exporter.unityVersion`
- `exporter.version`
- `exporter.buildTarget`
- `exporter.colorSpace`

manifest の検査は 2 段で行います。

1. 形式に依らない項目を検査します。`formatVersion` が 1 以外なら、ここで検査を終えます。新しい形式のファイルを「項目の不正」として拒否せず、互換性の判定で「アプリより新しい形式」（`FormatTooNew`）と伝えるためです。
2. `formatVersion` が 1 なら、2.2 節の残りの項目を検査します。

## 3. バージョンと互換性

### 3.1 番号を上げる条件

| 番号 | 上げる条件 |
|---|---|
| containerVersion | ヘッダのレイアウトを変えたとき |
| formatVersion | manifest の意味を破壊的に変えたとき |
| runtimeSchemaVersion | runtime パッケージの型を追加・移行したとき（`RuntimeSchema.Current`） |

containerVersion が 1 でないファイルは、ヘッダの検査（1.3 節）で拒否します。

### 3.2 アプリでの判定

アプリは次の順に評価し、最初に拒否となった理由を結果にします（`CompatibilityChecker`）。拒否が無ければ受け付け、警告を評価の順に添えます。

| 順 | 拒否・警告 | 条件 |
|---|---|---|
| 1 | FormatTooNew | `formatVersion` がアプリの対応する形式に無い |
| 2 | SchemaTooNew | `runtimeSchemaVersion` がアプリの値より大きい |
| 3 | WrongTarget | `exporter.buildTarget` が `StandaloneWindows64` でない |
| 4 | WrongColorSpace | `exporter.colorSpace` が `Linear` でない |
| 5 | DifferentStream | `exporter.unityVersion` がアプリの Unity と同じ系統でない |
| 5 | LegacyStreamAccepted（警告） | Unity 6 版のアプリで、2022.3 からの読み込みを許可している場合の 2022.3 のファイル |
| 6 | NewerThanApp | 同じ系統で、アプリの Unity より新しい |
| 7 | KnownBadExporter | `exporter.version` が、不具合が知られている範囲にある。範囲ごとに拒否か警告かを決める |

- Unity の系統: 2022 までは major と minor が同じもの、6000 以降は major が同じもの（6000.0 と 6000.3 は同じ系統）。
- 文字列の比較は大文字と小文字を区別します。

### 3.3 Unity のバージョンの書式

`<major>.<minor>.<patch><kind><build>` の後に、中国版の接尾辞 `c<数字>` が付くことがあります（例: `2022.3.22f1`、`2022.3.22f1c1`）。

- kind は `a`（alpha）< `b`（beta）< `f`（final）< `p`（patch）の順です。
- 比較は major、minor、patch、kind、build の順に行います。接尾辞 `c` は比較に使いません。

### 3.4 SemVer と範囲

- `exporter.version` は Semantic Versioning 2.0.0 の書式です。先頭の `v` や前後の空白は認めません。
- 不具合の範囲は、比較子を空白で区切って並べたもので、すべてを満たすバージョンが範囲に入ります（例: `>=1.0.0 <1.0.3`）。比較子は `>=`、`>`、`<=`、`<`、`=` のいずれかとバージョンを空白なしでつなげたもので、演算子を省くと `=` です。
- プレリリースは SemVer の優先順位のとおりに比べます。たとえば `1.0.3-beta` は `<1.0.3` に入ります。

### 3.5 エクスポーターが動く Unity

エクスポーターは、書き出しを始める前に自分が動いている Unity を判定します（`ExporterUnityPolicy`）。

| Unity | 扱い |
|---|---|
| 2022.3.22f1 以上 2022.3.62f3 以下 | 書き出す |
| 6000.x | 「Unity 6 版のアプリでのみ読めます」と警告して書き出す（`W_EXPORT_UNITY6_APP_ONLY`） |
| それ以外 | 中止する（`E_EXPORT_UNSUPPORTED_UNITY`） |

上限の 2022.3.62f3 は、2022.3 版のアプリが使う Unity です。これより新しいパッチで書き出したファイルは、アプリが `NewerThanApp` で拒否します。

## 4. 取り込み時の検証

アプリは、取り込むファイルをライブラリにコピーしてから、コピーに対して次の順に検証します（`PackageVerifier`）。最初に失敗した段階で止めます。ただし、サムネイルの不合格だけはサムネイル無しとして続けます。

1. ヘッダ（1.3 節）
2. manifest の JSON の読み取り
3. manifest の検査（2.2 節、2.3 節）
4. 互換性（3.2 節）
5. サムネイルの IHDR（1.4 節）
6. バンドルの区間の SHA-256 と `bundle.sha256` の照合

| 失敗した段階 | 取り込みのエラー | 詳細コード |
|---|---|---|
| 1 | `E_IMPORT_HEADER` | `E_PKG_` で始まるコード |
| 2、3 | `E_IMPORT_MANIFEST` | `E_MANIFEST_` で始まるコード |
| 4 | `E_IMPORT_INCOMPATIBLE` | `E_COMPAT_` で始まるコード |
| 6 | `E_IMPORT_SHA256_MISMATCH` | ― |

- サムネイルの不合格（5）は取り込みのエラーにしません。警告 `W_THUMB_REJECTED` を添えるのは `Header.ThumbnailLength != 0 && !ThumbnailOk` のときだけで、サムネイルの無いパッケージには添えません（1.4 節）。

## 5. メッセージコード

エクスポーターとアプリは、共通のメッセージカタログ（`MessageCatalog`）を使います。各コードには日本語と英語の文と、[troubleshooting.md](troubleshooting.md) の同じ名前の節へのアンカーがあります。

| 接頭辞 | 意味 |
|---|---|
| `E_` / `W_` / `I_` | エラー／警告／お知らせ |
| `E_PKG_` | ヘッダの検査（`PackageError`） |
| `E_MANIFEST_` | manifest の検査（`ManifestErrorCode`） |
| `E_COMPAT_` / `W_COMPAT_` | 互換性の判定（`CompatCode`） |
| `E_IMPORT_` / `I_IMPORT_` | 取り込みの失敗 |
| `E_LOAD_` | ロードの失敗 |
| `W_EXPORT_` / `E_EXPORT_` | エクスポーターが動く Unity の判定 |
