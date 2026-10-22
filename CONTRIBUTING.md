# コントリビューションガイド

## 開発環境

Unity を使う開発と、描画・透過・Win32 の確認は Windows 10/11 で行います。Unity に依存しないロジック（`dotnet/` でテストするもの）は、どの OS でも開発できます。

| 用途 | 必要なもの |
|---|---|
| Unity に依存しないロジック | .NET 8 SDK |
| アプリ本体（`app/`） | Unity 2022.3.62f3 |
| エクスポーターのテスト（`fixtures/ExporterHarness/`） | Unity 2022.3.22f1 |
| 実機での確認 | [docs/windows-test.md](docs/windows-test.md) の機材 |

```powershell
dotnet test dotnet/OpenPerch.Core.Tests/OpenPerch.Core.Tests.csproj
powershell -ExecutionPolicy Bypass -File tools/run-unity-tests.ps1
```

Unity のプロジェクトはパスが長くなるため、Windows では `git config --global core.longpaths true` を設定し、浅いフォルダ（例: `C:\dev\openperch`）に clone してください。

## 出自とライセンス

このプロジェクトは MIT で公開します。取り込むコードと素材の出自を、次の規則で管理します。

- **リポジトリと配布物に入れないもの**: VRChat SDK とそのアセット、lilToon・Poiyomi などのシェーダー、アバター、購入したモーション、Mixamo のファイル、Unity Asset Store のアセット。
- **コードを見てはいけないもの**: GPL / AGPL のプロジェクト（MateEngine など）。
- **考え方の参照だけにとどめるもの**: MPL-2.0（ndmf-vrm-exporter、VRMConverterForVRChat）と Apache-2.0（VRCast）。
- **移植してよいもの**: MIT（Av3Emulator、GestureManager、UniVRM など）と BSD-3。移植する場合は原著作権表示を残し、[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) に追記します。
- **uGUI**（Unity Companion License）: ソースを複製しません。
- **依存の追加**: MIT / Apache-2.0 / BSD / OFL / Unity Companion License に限ります。追加する PR で `THIRD_PARTY_NOTICES.md` も更新してください。

### VRChat SDK の扱い

VRChat SDK License は逆コンパイルを禁止しています。SDK のアセンブリを開くことがあるので、IDE の逆コンパイル表示を無効にしてください。

- **Rider**: Settings → Tools → External Symbols で、逆コンパイルした内容（decompiled sources）へのナビゲーションを無効にする。
- **Visual Studio**: Tools → Options → Text Editor → C# → Advanced の「Enable navigation to decompiled sources」を外す。
- **VS Code（C# Dev Kit）**: `"dotnet.navigation.navigateToDecompiledSources": false` を設定する。

参照してよい情報源は、[VRChat Creator Docs](https://creators.vrchat.com/) と、MIT で公開されている NDMF / Avatar Optimizer のソースです。
runtime パッケージのフィールドや、VRChat コンポーネントからの写像表を変えるときは、根拠（URL）を [docs/provenance.md](docs/provenance.md) に記録してください。

## Developer Certificate of Origin

すべてのコミットに `Signed-off-by` を付けてください（`git commit -s`）。署名は [Developer Certificate of Origin 1.1](https://developercertificate.org/) に同意したことを表します。

## コーディング規約

- **言語**: C# 9 の範囲で書きます（Unity 2022.3 の上限）。`init`、`record`、共変戻り値、file-scoped namespace は使いません。
- **命名**: 名前空間はアセンブリ名と一致させます（アプリは `OpenPerch.App.<フォルダ名>`）。runtime の公開型には接頭辞 `Perch` を付けます。private フィールドは `_camelCase`、凍結対象のフィールドと永続化する型のフィールドは `camelCase` です。1 ファイルに 1 型を置き、ファイル名と型名を一致させます。
- **永続化する型**（設定・ライブラリの索引・ロケール・フィクスチャ・manifest）: `[Serializable]` を付け、データは public フィールドだけにします。プロパティ、readonly フィールド、Dictionary / HashSet、インターフェイス型、ポリモーフィズムは使いません。参照型のフィールドは非 null で初期化します。フィールド名は保存形式の一部なので、改名するときは schemaVersion の移行を伴わせます。
- **JSON**: `OpenPerch.App.Core` は `ITextCodec` を通してだけ JSON を扱います。
- **コメント**: 非自明な理由（WHY）だけを書きます。コードを読めば分かること、変更履歴、Issue 番号は書きません。公開 API には XML ドキュメントコメントを書きます。
- **P/Invoke**: 宣言は `OpenPerch.App.Platform.Windows.Native` に集約します。ウィンドウ情報の取得に `GetWindowText*`、`GetClassName*`、`OpenProcess`、`QueryFullProcessImageName*`、UI Automation は使いません。
- **スレッド**: 専用スレッドから Unity API を呼びません。結果は不変のスナップショットか `ConcurrentQueue` で渡します。

## コミットと PR

- コミットメッセージは [Conventional Commits](https://www.conventionalcommits.org/) に従います。
- PR では、テンプレートの確認項目（出自、DCO、ライセンス）をすべて確認してください。
