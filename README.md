# OpenPerch（仮称）

自分で書き出した VRChat アバターを、Windows のデスクトップに常駐させるための OSS です。
アバター・シェーダー・モーションは同梱しません。ユーザーが自分の Unity プロジェクトから書き出したデータを取り込んで動かす、土台だけを提供します。

> **開発中です。** 利用できるリリースはまだありません。名称は仮称です。

**English:** OpenPerch (provisional name) is an open-source platform that keeps VRChat avatars you export yourself on your Windows desktop. It ships no avatars, shaders or motions. Work in progress — there is no usable release yet.

## 構成

| ディレクトリ | 内容 |
|---|---|
| `packages/com.openperch.runtime` | エクスポーターとアプリが共有する直列化型と `.opk` 形式（UPM / VPM） |
| `packages/com.openperch.exporter` | VRChat アバター用の Unity プロジェクトに入れるエクスポーター（VPM） |
| `app/` | デスクトップアプリ本体（Unity 2022.3、Windows 10/11 x64） |
| `dotnet/` | Unity に依存しないロジックのテストと、Windows ネイティブ部分の確認ツール |
| `fixtures/` | テスト用の自作データ（CC0 / MIT） |
| `docs/` | 仕様と手順 |

## 利用にあたっての注意

- 個人利用に限ってください。
- 取り込むアバターやモーションの利用規約（VN3 ライセンスなど）で、VRChat 以外のアプリでの使用と形式の変換が許可されているか確認してください。
- 書き出した `.opk` ファイルを他人と共有しないでください。`.opk` は暗号化していないため、中身を取り出せます。
- 読み込むファイルは自分で書き出したものだけにしてください。Unity のランタイムやグラフィックスドライバの脆弱性は、アプリ側では防げません。
- このプロジェクトは VRChat Inc. とは関係ありません。

## ライセンス

[MIT License](LICENSE)。サードパーティ製品の表記は [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) にあります。

## 開発に参加する

[CONTRIBUTING.md](CONTRIBUTING.md) を参照してください。脆弱性の報告方法は [SECURITY.md](SECURITY.md) にあります。
