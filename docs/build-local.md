# ローカルビルド

アプリは、開発機（Windows。[windows-test.md](windows-test.md) の WIN-D）でビルドします。v1 の正式なリリースもこの手順で作ります。

## 必要なもの

| 用途 | 必要なもの |
|---|---|
| ビルド | Unity 2022.3.62f3（Windows の Editor は Mono での Windows ビルドを標準で含む） |
| 署名 | Windows SDK の `signtool` と、署名手段（ゲート S で調達するコード署名証明書） |
| 梱包・公開 | PowerShell（`Compress-Archive`、`Get-FileHash`）、GitHub CLI（`gh`） |

アプリの Unity は 2022.3.62f3 です（プロファイル `U2022`）。アプリを Unity 6000.3 LTS に移すかの判定（ゲート U。[spikes.md](spikes.md) の SP12 など）に合格した場合は、6000.3 LTS（プロファイル `U6000`）に切り替えます。エクスポーターと ExporterHarness は、VRChat の指定に合わせて 2022.3.22f1 を使います。

## BuildScript.BuildWindowsCli

`OpenPerch.App.Editor.BuildScript.BuildWindowsCli` をバッチモードで呼びます。リポジトリのルートで、PowerShell から次を実行します。

```powershell
$unity = "$env:ProgramFiles\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe"
& $unity -batchmode -quit `
  -projectPath app `
  -buildTarget Win64 `
  -executeMethod OpenPerch.App.Editor.BuildScript.BuildWindowsCli `
  -opOut "$PWD\out\OpenPerch-1.0.0" `
  -opVersion 1.0.0 `
  -logFile - | Out-Host
$LASTEXITCODE
```

Unity.exe は GUI のプログラムなので、出力をパイプで受けて終了を待ちます。

| 引数 | 内容 |
|---|---|
| `-opOut <dir>` | 出力先のフォルダ。絶対パスで渡す（例では `$PWD` を付けている）。リポジトリのルートの `out/` は `.gitignore` で除外している |
| `-opVersion <semver>` | アプリのバージョン。`PlayerSettings.bundleVersion` と `AppInfo.Version` に埋め込む |
| `-opDev` | 開発ビルドにする。スクリプト定義に `OPENPERCH_TEST_SUPPORT` を足し、DevTools（`--open <path>`、`--selftest compat <dir>`、`--dev-spawn-two`）を含める |
| `-opProfile <U2022\|U6000>` | Unity のプロファイル。省略すると `U2022` |

ビルドに失敗すると、Unity は 0 以外の終了コードで終わります（`$LASTEXITCODE`）。ログは `-logFile -` で標準出力に出ます。

### BuildScript が毎回そろえる設定

ビルドのたびに次の値を強制します。プロジェクトの設定を手で変えても、ビルドには反映されません。

| 区分 | 値 |
|---|---|
| Player | Win64、Mono、Managed Stripping Level = Disabled、API Compatibility Level = .NET Standard 2.1、Active Input Handling = Input Manager (Old) |
| 描画 | Color Space = Linear、Graphics APIs = Direct3D11 だけ（自動選択は無効）、Flip Model Swapchain = 無効、GPU Skinning = 有効、Frame Timing Stats = 有効 |
| ウィンドウ | Fullscreen Mode = Windowed、既定の画面 480x270、Resizable Window = 有効、Allow Fullscreen Switch = 無効、Run In Background = 有効、Force Single Instance = 無効 |
| 送信 | Analytics、Performance Reporting、Crash Reporting は無効。クラウドプロジェクトにはリンクしない |
| Quality | レベルは 1 つ。Skin Weights = 4 Bones、Anti Aliasing = 無効、Global Texture Mipmap Limit = 0、Soft Particles = 無効、Pixel Light Count = 2、Shadow Distance = 10、VSync = 無効 |
| Graphics | HDR Mode = FP16。Always Included Shaders = VRChat のプロジェクトテンプレートと同じ 6 件と `OpenPerch/Composite`、`OpenPerch/UIPremul`。Lights Use Linear Intensity と Lights Use Color Temperature を有効 |

`U6000` では、上に加えてスプラッシュを無効にし、6000.3 に対応する Burst と TextMeshPro（ugui 2.x）を使い、`AppProfile.AcceptLegacy2022` を true にします。Direct3D11 の固定は同じです。

### ビルドを失敗させる検査

| 検査 | 内容 |
|---|---|
| 組み込みモジュール | `app/Packages/manifest.json` に animation、assetbundle、cloth、particlesystem、physics、audio、imageconversion、jsonserialize、ui、director、unitywebrequest の各モジュールが無ければ失敗する |
| 素材のライセンス | `Assets/**/Resources/**`、`Packages/**/Resources/**`（埋め込み・ローカル・キャッシュ済みのパッケージを含む）、`Assets/StreamingAssets/**` のファイルを [asset-licenses.json](asset-licenses.json) と照合し、申告の無いファイルがあれば失敗する。出力には `Licenses/` フォルダを入れる |

`asset-licenses.json` は `{ "path", "license", "source" }` の配列です。素材を追加するときは、同じ PR でこのファイルに追記します。

| キー | 書き方 |
|---|---|
| `path` | アプリのプロジェクト（`app/`）での Unity のアセットパス。プロジェクトのファイルは `Assets/...`（例: `Assets/StreamingAssets/i18n/ja.json`）、パッケージのファイルは `Packages/<パッケージ名>/...` と書く。パッケージは、埋め込み・ローカル・キャッシュ済みのどれでも、実体の場所や版に依らず同じ書き方になる。`*`（フォルダ名の中の任意の文字列）、`**`（フォルダをまたぐ任意のパス）、`?`（任意の 1 文字）を使える |
| `license` | SPDX の識別子 |
| `source` | 素材の出どころ（自作、配布元の URL など） |

## リリースまでの流れ

すべて開発機で行います。`<ver>` はアプリのバージョンです。

1. **ビルド**: 上の `BuildWindowsCli` で `out\OpenPerch-<ver>\` に出力します。
2. **署名**: 署名手段を調達できた場合（ゲート S）だけ行います。`OpenPerch.exe` と同梱の DLL のうち署名の無いもの（`signtool verify /pa` が失敗するもの。自前でビルドした `LibUniWinC.dll` を含む）に署名します。
   ```powershell
   signtool sign /fd SHA256 /tr <タイムスタンプサーバーの URL> /td SHA256 /n "<証明書のサブジェクト名>" OpenPerch.exe LibUniWinC.dll
   signtool verify /pa /v OpenPerch.exe
   ```
3. **梱包**: 出力フォルダの直下に、展開してから起動するよう案内する `README-FIRST.txt` を置き、zip とその SHA-256 を作ります。
   ```powershell
   Compress-Archive -Path out\OpenPerch-<ver>\* -DestinationPath out\OpenPerch-<ver>.zip
   (Get-FileHash -Algorithm SHA256 out\OpenPerch-<ver>.zip).Hash.ToLower() | Set-Content -NoNewline out\OpenPerch-<ver>.zip.sha256
   ```
4. **VPM**: runtime と exporter のパッケージを、それぞれ VPM 用の zip にします（`Tests/Runtime` は除く）。runtime と exporter は同じ SemVer にします。
5. **公開**: ドラフトのリリースを作り、3 と 4 のファイルを添付します。
   ```powershell
   gh release create v<ver> --draft --title "OpenPerch <ver>" out\OpenPerch-<ver>.zip out\OpenPerch-<ver>.zip.sha256
   ```

署名は必ず zip にする前に行います。zip にした後で署名すると、zip と `.sha256` を作り直すことになります。署名手段が無い場合は未署名のまま出し、README に Smart App Control での制限を書きます。

## 確認用の Windows 機への渡し方

開発機（WIN-D）でビルドした zip を、確認用の機材（WIN-I、VM-C）に USB メモリやネットワークの共有で渡します。

1. zip と `.sha256` を確認用の機材のローカルのディスクにコピーします。共有フォルダ上や zip の中からは起動しません。
2. PowerShell でハッシュを確かめます。
   ```powershell
   (Get-FileHash -Algorithm SHA256 .\OpenPerch-<ver>.zip).Hash.ToLower()
   Get-Content .\OpenPerch-<ver>.zip.sha256
   ```
3. 展開して、展開先の `OpenPerch.exe` を起動します。
   ```powershell
   Expand-Archive .\OpenPerch-<ver>.zip -DestinationPath .\OpenPerch-<ver>
   ```
4. テストの記録（`docs/test-runs/`）に、ビルドのバージョンとコミットを書きます。

SmartScreen と Smart App Control の確認（[windows-test.md](windows-test.md) の W-SYS-07）では、Mark-of-the-Web が確実に付くよう、共有ではなく、ドラフトのリリースからブラウザでダウンロードした zip を使います。
