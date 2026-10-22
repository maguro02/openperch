using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OpenPerch.Format
{
    /// <summary>
    /// Every message code with its Japanese and English text. Each code has a section of the same name in
    /// <see cref="TroubleshootingPath"/>, whose anchor is <see cref="MessageDef.RemedyAnchor"/>.
    /// </summary>
    public static class MessageCatalog
    {
        /// <summary>The document, relative to the repository root, that explains every code.</summary>
        public const string TroubleshootingPath = "docs/troubleshooting.md";

        private static readonly MessageDef[] Defs =
        {
            Error(MessageCodes.E_ELEVATED,
                "管理者として実行されているため、取り込みとロードを無効にしています。",
                "Importing and loading are disabled because the app is running as administrator."),
            Error(MessageCodes.E_DEF_INVALID,
                "アバターまたはモーションの定義の構造が不正なため、読み込めません。",
                "The avatar or motion definition has an invalid structure and cannot be loaded."),
            Warning(MessageCodes.W_DEF_CLAMPED,
                "許容範囲外の値を範囲内に収めました。揺れ方や見た目が書き出し元と異なることがあります。",
                "Out-of-range values were clamped. Motion or appearance may differ from the original."),
            Warning(MessageCodes.W_REF_OUTSIDE,
                "アバターの外を指す参照を外しました。該当する揺れものや Constraint は動きません。",
                "References pointing outside the avatar were removed. The affected PhysBones and constraints do not move."),
            Warning(MessageCodes.W_THUMB_REJECTED,
                "サムネイルが条件を満たさないため、表示しません。",
                "The thumbnail does not meet the requirements and is not shown."),
            Warning(MessageCodes.W_REMOVED_COMPONENT,
                "OpenPerch で扱えないコンポーネントを除去しました。",
                "Components that OpenPerch does not support were removed."),

            Error(MessageCodes.E_PKG_BAD_MAGIC,
                "OpenPerch のパッケージ（.opk）ではありません。",
                "The file is not an OpenPerch package (.opk)."),
            Error(MessageCodes.E_PKG_UNSUPPORTED_CONTAINER,
                "このアプリが対応していないコンテナ形式のパッケージです。",
                "The package uses a container version this app does not support."),
            Error(MessageCodes.E_PKG_BAD_FLAGS,
                "パッケージのヘッダに未対応のフラグがあります。",
                "The package header has unsupported flags."),
            Error(MessageCodes.E_PKG_BAD_RESERVED,
                "パッケージのヘッダの予約領域が 0 ではありません。",
                "Reserved fields in the package header are not zero."),
            Error(MessageCodes.E_PKG_BAD_HEADER_CRC,
                "パッケージのヘッダが壊れています（CRC が一致しません）。",
                "The package header is corrupted (CRC mismatch)."),
            Error(MessageCodes.E_PKG_BAD_MANIFEST_OFFSET,
                "パッケージのヘッダが示す manifest の位置が不正です。",
                "The manifest offset in the package header is invalid."),
            Error(MessageCodes.E_PKG_OUT_OF_RANGE,
                "パッケージ内のデータの位置または長さが範囲外です。",
                "An offset or length in the package is out of range."),
            Error(MessageCodes.E_PKG_OVERLAP,
                "パッケージ内のデータが重なっているか、並び順が正しくありません。",
                "Sections of the package overlap or are out of order."),
            Error(MessageCodes.E_PKG_MISALIGNED,
                "パッケージ内のバンドルの位置が 4096 バイトの境界にありません。",
                "The bundle in the package is not aligned to 4096 bytes."),
            Error(MessageCodes.E_PKG_LENGTH_MISMATCH,
                "ファイルの長さがパッケージのヘッダと一致しません。ファイルが途中で切れているか、余分なデータが付いています。",
                "The file length does not match the package header. The file is truncated or has extra data."),
            Error(MessageCodes.E_PKG_MANIFEST_TOO_LARGE,
                "manifest が上限の 256 KiB を超えています。",
                "The manifest exceeds the 256 KiB limit."),
            Error(MessageCodes.E_PKG_BAD_THUMBNAIL_FIELDS,
                "パッケージのヘッダのサムネイルの指定が不正です。",
                "The thumbnail fields in the package header are invalid."),

            Error(MessageCodes.E_MANIFEST_JSON_INVALID,
                "manifest を JSON のオブジェクトとして読めません。",
                "The manifest is not a valid JSON object."),
            Error(MessageCodes.E_MANIFEST_MISSING_FIELD,
                "manifest に必須の項目がありません。",
                "A required manifest field is missing."),
            Error(MessageCodes.E_MANIFEST_BAD_EXPORT_ID,
                "manifest の exportId の書式が正しくありません。",
                "The manifest exportId is malformed."),
            Error(MessageCodes.E_MANIFEST_BAD_LINEAGE_ID,
                "manifest の lineageId の書式が正しくありません。",
                "The manifest lineageId is malformed."),
            Error(MessageCodes.E_MANIFEST_BAD_SHA256,
                "manifest の bundle.sha256 の書式が正しくありません。",
                "The manifest bundle.sha256 is malformed."),
            Error(MessageCodes.E_MANIFEST_BUNDLE_NAME_MISMATCH,
                "manifest の bundle.name が exportId と対応していません。",
                "The manifest bundle.name does not match the exportId."),
            Error(MessageCodes.E_MANIFEST_BUNDLE_LENGTH_MISMATCH,
                "manifest の bundle.length がヘッダのバンドルの長さと一致しません。",
                "The manifest bundle.length does not match the package header."),
            Error(MessageCodes.E_MANIFEST_KIND_ROOT_MISMATCH,
                "manifest の kind と bundle.rootAsset の組み合わせが正しくありません。",
                "The manifest kind and bundle.rootAsset do not match."),
            Error(MessageCodes.E_MANIFEST_UNKNOWN_KIND,
                "manifest の kind が不明な値です。",
                "The manifest kind is unknown."),
            Error(MessageCodes.E_MANIFEST_BAD_UNITY_VERSION,
                "manifest の exporter.unityVersion を Unity のバージョンとして読めません。",
                "The manifest exporter.unityVersion is not a Unity version."),
            Error(MessageCodes.E_MANIFEST_BAD_EXPORTER_VERSION,
                "manifest の exporter.version が SemVer の形式ではありません。",
                "The manifest exporter.version is not a SemVer version."),
            Error(MessageCodes.E_MANIFEST_NEGATIVE_NUMBER,
                "manifest に負の数値があります。",
                "The manifest contains a negative number."),
            Error(MessageCodes.E_MANIFEST_TOO_LONG,
                "manifest の文字列が長さの上限を超えています。",
                "A manifest string exceeds its length limit."),
            Error(MessageCodes.E_MANIFEST_TOO_MANY,
                "manifest の項目の数が上限を超えています。",
                "A manifest list has too many entries."),

            Error(MessageCodes.E_COMPAT_FORMAT_TOO_NEW,
                "このアプリより新しい形式のパッケージです。アプリを更新してください。",
                "The package uses a newer format than this app supports. Update the app."),
            Error(MessageCodes.E_COMPAT_SCHEMA_TOO_NEW,
                "このアプリより新しいエクスポーターで書き出されています。アプリを更新してください。",
                "The package was exported by a newer exporter than this app supports. Update the app."),
            Error(MessageCodes.E_COMPAT_WRONG_TARGET,
                "Windows（StandaloneWindows64）向けに書き出されていません。",
                "The package was not built for Windows (StandaloneWindows64)."),
            Error(MessageCodes.E_COMPAT_WRONG_COLOR_SPACE,
                "色空間が Linear ではないプロジェクトから書き出されています。",
                "The package was exported from a project whose color space is not Linear."),
            Error(MessageCodes.E_COMPAT_DIFFERENT_STREAM,
                "このアプリとは系統の異なる Unity で書き出されています。",
                "The package was exported with a Unity version from a different release stream than this app."),
            Error(MessageCodes.E_COMPAT_NEWER_THAN_APP,
                "このアプリより新しい Unity で書き出されています。",
                "The package was exported with a newer Unity version than this app uses."),
            Error(MessageCodes.E_COMPAT_KNOWN_BAD_EXPORTER,
                "不具合が知られているエクスポーターで書き出されています。エクスポーターを更新して書き出し直してください。",
                "The package was exported by an exporter version with a known defect. Update the exporter and export again."),
            Warning(MessageCodes.W_COMPAT_KNOWN_BAD_EXPORTER,
                "不具合が知られているエクスポーターで書き出されています。表示がおかしい場合は、エクスポーターを更新して書き出し直してください。",
                "The package was exported by an exporter version with a known defect. If it looks wrong, update the exporter and export again."),
            Warning(MessageCodes.W_COMPAT_LEGACY_STREAM_ACCEPTED,
                "Unity 2022.3 で書き出されたパッケージを、Unity 6 版のアプリで読み込みます。",
                "A package exported with Unity 2022.3 is being read by the Unity 6 build of the app."),

            Info(MessageCodes.I_IMPORT_UNTRUSTED_DECLINED,
                "入手元を確認できないファイルの取り込みを取りやめました。",
                "Importing a file from an unverified source was cancelled."),
            Error(MessageCodes.E_IMPORT_COPY_FAILED,
                "ライブラリへのコピーに失敗しました。",
                "Copying the file into the library failed."),
            Error(MessageCodes.E_IMPORT_DISK_FULL,
                "ディスクの空き容量が足りないため、取り込めません。",
                "There is not enough disk space to import the file."),
            Error(MessageCodes.E_IMPORT_HEADER,
                "パッケージのヘッダが不正なため、取り込めません。",
                "The package header is invalid, so the file cannot be imported."),
            Error(MessageCodes.E_IMPORT_MANIFEST,
                "パッケージの manifest が不正なため、取り込めません。",
                "The package manifest is invalid, so the file cannot be imported."),
            Error(MessageCodes.E_IMPORT_INCOMPATIBLE,
                "このアプリでは使えないパッケージです。",
                "This package cannot be used with this app."),
            Error(MessageCodes.E_IMPORT_SHA256_MISMATCH,
                "バンドルの内容が manifest のハッシュと一致しません。ファイルが壊れています。",
                "The bundle does not match the hash in the manifest. The file is corrupted."),

            Error(MessageCodes.E_LOAD_CRC,
                "バンドルの CRC が一致しません。ファイルが壊れています。",
                "The bundle CRC does not match. The file is corrupted."),
            Error(MessageCodes.E_LOAD_DEFINITION_MISSING,
                "パッケージにアバターまたはモーションの定義が含まれていません。",
                "The package does not contain an avatar or motion definition."),
            Error(MessageCodes.E_LOAD_EXPORT_ID_MISMATCH,
                "パッケージの中身が manifest の exportId と一致しません。",
                "The package contents do not match the exportId in the manifest."),
            Error(MessageCodes.E_LOAD_BUNDLE_NAME_COLLISION,
                "同じ名前のバンドルがすでに読み込まれています。",
                "A bundle with the same name is already loaded."),
            Error(MessageCodes.E_LOAD_SANITIZE_REJECTED,
                "安全のための検査で拒否されたため、表示できません。",
                "The avatar was rejected by the safety checks and cannot be shown."),

            Warning(MessageCodes.W_EXPORT_UNITY6_APP_ONLY,
                "Unity 6 で書き出したファイルは、Unity 6 版のアプリでのみ読めます。",
                "Files exported with Unity 6 can only be read by the Unity 6 build of the app."),
            Error(MessageCodes.E_EXPORT_UNSUPPORTED_UNITY,
                "この Unity のバージョンでは書き出せません。Unity 2022.3.22f1〜2022.3.62f3 で書き出してください。",
                "This Unity version cannot export. Use Unity 2022.3.22f1 to 2022.3.62f3."),
        };

        private static readonly ReadOnlyCollection<MessageDef> AllDefs = Array.AsReadOnly(Defs);
        private static readonly Dictionary<string, MessageDef> ByCode = Index(Defs);

        public static IReadOnlyList<MessageDef> All => AllDefs;

        public static bool TryGet(string code, out MessageDef def)
        {
            if (code == null)
            {
                def = null;
                return false;
            }
            return ByCode.TryGetValue(code, out def);
        }

        private static Dictionary<string, MessageDef> Index(MessageDef[] defs)
        {
            var index = new Dictionary<string, MessageDef>(StringComparer.Ordinal);
            foreach (MessageDef def in defs)
            {
                index.Add(def.Code, def);
            }
            return index;
        }

        private static MessageDef Info(string code, string ja, string en) => Define(code, MessageSeverity.Info, ja, en);

        private static MessageDef Warning(string code, string ja, string en) => Define(code, MessageSeverity.Warning, ja, en);

        private static MessageDef Error(string code, string ja, string en) => Define(code, MessageSeverity.Error, ja, en);

        // GitHub derives a heading's anchor by lowercasing it, and code headings contain nothing else it would rewrite.
        private static MessageDef Define(string code, MessageSeverity severity, string ja, string en) =>
            new MessageDef(code, severity, ja, en, code.ToLowerInvariant(), false);
    }
}
