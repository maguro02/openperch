# セキュリティポリシー / Security Policy

## 報告方法 / Reporting

脆弱性は公開の Issue ではなく、GitHub の非公開の脆弱性報告（Security タブの「Report a vulnerability」）から報告してください。

Please report vulnerabilities privately through GitHub's "Report a vulnerability" button on the Security tab. Do not open public issues for them.

## 対象 / Scope

- 対象: アプリ本体、エクスポーター、`.opk` 形式の読み込み処理。
- 対象外: Unity のランタイムやグラフィックスドライバそのものの脆弱性。これらは Unity Technologies や各ベンダーに報告してください。アプリ側で緩和できるものについての報告は歓迎します。

## 方針 / Policy

- 読み込むファイルは、ユーザー自身の書き出しであっても信頼しない入力として扱います。
- CRC-32 と SHA-256 で検出できるのは破損だけです。改ざんは防げません。
- リリースのたびに Unity Security Advisories を確認します。Windows プレイヤーに影響し、利用中の Unity 2022.3 系に修正が出ない脆弱性が公表された場合は、8 週以内に Unity 6 LTS 版を公開するか、Unity が提供するパッチ手段を当てて対応します。
- 報告者とは非公開でやり取りし、修正版を公開してからアドバイザリを公開します。
