using System;

namespace OpenPerch.Format
{
    /// <summary>
    /// The message codes in <see cref="MessageCatalog"/>, and the code for each error and verdict enum of the format.
    /// The import and load errors of the app are listed here too so that both the exporter and the app use one table.
    /// </summary>
    public static class MessageCodes
    {
        /// <summary>The app runs as administrator; also <c>ImportError.Elevated</c>.</summary>
        public const string E_ELEVATED = "E_ELEVATED";

        /// <summary>A definition failed structural validation; also <c>LoadError.DefinitionInvalid</c>.</summary>
        public const string E_DEF_INVALID = "E_DEF_INVALID";

        public const string W_DEF_CLAMPED = "W_DEF_CLAMPED";
        public const string W_REF_OUTSIDE = "W_REF_OUTSIDE";
        public const string W_THUMB_REJECTED = "W_THUMB_REJECTED";
        public const string W_REMOVED_COMPONENT = "W_REMOVED_COMPONENT";

        public const string E_PKG_BAD_MAGIC = "E_PKG_BAD_MAGIC";
        public const string E_PKG_UNSUPPORTED_CONTAINER = "E_PKG_UNSUPPORTED_CONTAINER";
        public const string E_PKG_BAD_FLAGS = "E_PKG_BAD_FLAGS";
        public const string E_PKG_BAD_RESERVED = "E_PKG_BAD_RESERVED";
        public const string E_PKG_BAD_HEADER_CRC = "E_PKG_BAD_HEADER_CRC";
        public const string E_PKG_BAD_MANIFEST_OFFSET = "E_PKG_BAD_MANIFEST_OFFSET";
        public const string E_PKG_OUT_OF_RANGE = "E_PKG_OUT_OF_RANGE";
        public const string E_PKG_OVERLAP = "E_PKG_OVERLAP";
        public const string E_PKG_MISALIGNED = "E_PKG_MISALIGNED";
        public const string E_PKG_LENGTH_MISMATCH = "E_PKG_LENGTH_MISMATCH";
        public const string E_PKG_MANIFEST_TOO_LARGE = "E_PKG_MANIFEST_TOO_LARGE";
        public const string E_PKG_BAD_THUMBNAIL_FIELDS = "E_PKG_BAD_THUMBNAIL_FIELDS";

        public const string E_MANIFEST_JSON_INVALID = "E_MANIFEST_JSON_INVALID";
        public const string E_MANIFEST_MISSING_FIELD = "E_MANIFEST_MISSING_FIELD";
        public const string E_MANIFEST_BAD_EXPORT_ID = "E_MANIFEST_BAD_EXPORT_ID";
        public const string E_MANIFEST_BAD_LINEAGE_ID = "E_MANIFEST_BAD_LINEAGE_ID";
        public const string E_MANIFEST_BAD_SHA256 = "E_MANIFEST_BAD_SHA256";
        public const string E_MANIFEST_BUNDLE_NAME_MISMATCH = "E_MANIFEST_BUNDLE_NAME_MISMATCH";
        public const string E_MANIFEST_BUNDLE_LENGTH_MISMATCH = "E_MANIFEST_BUNDLE_LENGTH_MISMATCH";
        public const string E_MANIFEST_KIND_ROOT_MISMATCH = "E_MANIFEST_KIND_ROOT_MISMATCH";
        public const string E_MANIFEST_UNKNOWN_KIND = "E_MANIFEST_UNKNOWN_KIND";
        public const string E_MANIFEST_BAD_UNITY_VERSION = "E_MANIFEST_BAD_UNITY_VERSION";
        public const string E_MANIFEST_BAD_EXPORTER_VERSION = "E_MANIFEST_BAD_EXPORTER_VERSION";
        public const string E_MANIFEST_NEGATIVE_NUMBER = "E_MANIFEST_NEGATIVE_NUMBER";
        public const string E_MANIFEST_TOO_LONG = "E_MANIFEST_TOO_LONG";
        public const string E_MANIFEST_TOO_MANY = "E_MANIFEST_TOO_MANY";

        public const string E_COMPAT_FORMAT_TOO_NEW = "E_COMPAT_FORMAT_TOO_NEW";
        public const string E_COMPAT_SCHEMA_TOO_NEW = "E_COMPAT_SCHEMA_TOO_NEW";
        public const string E_COMPAT_WRONG_TARGET = "E_COMPAT_WRONG_TARGET";
        public const string E_COMPAT_WRONG_COLOR_SPACE = "E_COMPAT_WRONG_COLOR_SPACE";
        public const string E_COMPAT_DIFFERENT_STREAM = "E_COMPAT_DIFFERENT_STREAM";
        public const string E_COMPAT_NEWER_THAN_APP = "E_COMPAT_NEWER_THAN_APP";
        public const string E_COMPAT_KNOWN_BAD_EXPORTER = "E_COMPAT_KNOWN_BAD_EXPORTER";
        public const string W_COMPAT_KNOWN_BAD_EXPORTER = "W_COMPAT_KNOWN_BAD_EXPORTER";
        public const string W_COMPAT_LEGACY_STREAM_ACCEPTED = "W_COMPAT_LEGACY_STREAM_ACCEPTED";

        /// <summary><c>ImportError.UntrustedDeclined</c>.</summary>
        public const string I_IMPORT_UNTRUSTED_DECLINED = "I_IMPORT_UNTRUSTED_DECLINED";

        /// <summary><c>ImportError.CopyFailed</c>.</summary>
        public const string E_IMPORT_COPY_FAILED = "E_IMPORT_COPY_FAILED";

        /// <summary><c>ImportError.DiskFull</c>.</summary>
        public const string E_IMPORT_DISK_FULL = "E_IMPORT_DISK_FULL";

        /// <summary><c>ImportError.Header</c>; the detail comes from <see cref="For(PackageError)"/>.</summary>
        public const string E_IMPORT_HEADER = "E_IMPORT_HEADER";

        /// <summary><c>ImportError.Manifest</c>; the details come from <see cref="For(ManifestErrorCode)"/>.</summary>
        public const string E_IMPORT_MANIFEST = "E_IMPORT_MANIFEST";

        /// <summary><c>ImportError.Incompatible</c>; the detail comes from <see cref="ForRejection"/>.</summary>
        public const string E_IMPORT_INCOMPATIBLE = "E_IMPORT_INCOMPATIBLE";

        /// <summary><c>ImportError.Sha256Mismatch</c>.</summary>
        public const string E_IMPORT_SHA256_MISMATCH = "E_IMPORT_SHA256_MISMATCH";

        /// <summary><c>LoadError.Crc</c>.</summary>
        public const string E_LOAD_CRC = "E_LOAD_CRC";

        /// <summary><c>LoadError.DefinitionMissing</c>.</summary>
        public const string E_LOAD_DEFINITION_MISSING = "E_LOAD_DEFINITION_MISSING";

        /// <summary><c>LoadError.ExportIdMismatch</c>.</summary>
        public const string E_LOAD_EXPORT_ID_MISMATCH = "E_LOAD_EXPORT_ID_MISMATCH";

        /// <summary><c>LoadError.BundleNameCollision</c>.</summary>
        public const string E_LOAD_BUNDLE_NAME_COLLISION = "E_LOAD_BUNDLE_NAME_COLLISION";

        /// <summary><c>LoadError.SanitizeRejected</c>.</summary>
        public const string E_LOAD_SANITIZE_REJECTED = "E_LOAD_SANITIZE_REJECTED";

        public const string W_EXPORT_UNITY6_APP_ONLY = "W_EXPORT_UNITY6_APP_ONLY";
        public const string E_EXPORT_UNSUPPORTED_UNITY = "E_EXPORT_UNSUPPORTED_UNITY";

        public static string For(PackageError error)
        {
            switch (error)
            {
                case PackageError.BadMagic: return E_PKG_BAD_MAGIC;
                case PackageError.UnsupportedContainer: return E_PKG_UNSUPPORTED_CONTAINER;
                case PackageError.BadFlags: return E_PKG_BAD_FLAGS;
                case PackageError.BadReserved: return E_PKG_BAD_RESERVED;
                case PackageError.BadHeaderCrc: return E_PKG_BAD_HEADER_CRC;
                case PackageError.BadManifestOffset: return E_PKG_BAD_MANIFEST_OFFSET;
                case PackageError.OutOfRange: return E_PKG_OUT_OF_RANGE;
                case PackageError.Overlap: return E_PKG_OVERLAP;
                case PackageError.Misaligned: return E_PKG_MISALIGNED;
                case PackageError.LengthMismatch: return E_PKG_LENGTH_MISMATCH;
                case PackageError.ManifestTooLarge: return E_PKG_MANIFEST_TOO_LARGE;
                case PackageError.BadThumbnailFields: return E_PKG_BAD_THUMBNAIL_FIELDS;
                default: throw new ArgumentOutOfRangeException(nameof(error), error, null);
            }
        }

        public static string For(ManifestErrorCode code)
        {
            switch (code)
            {
                case ManifestErrorCode.JsonInvalid: return E_MANIFEST_JSON_INVALID;
                case ManifestErrorCode.MissingField: return E_MANIFEST_MISSING_FIELD;
                case ManifestErrorCode.BadExportId: return E_MANIFEST_BAD_EXPORT_ID;
                case ManifestErrorCode.BadLineageId: return E_MANIFEST_BAD_LINEAGE_ID;
                case ManifestErrorCode.BadSha256: return E_MANIFEST_BAD_SHA256;
                case ManifestErrorCode.BundleNameMismatch: return E_MANIFEST_BUNDLE_NAME_MISMATCH;
                case ManifestErrorCode.BundleLengthMismatch: return E_MANIFEST_BUNDLE_LENGTH_MISMATCH;
                case ManifestErrorCode.KindRootMismatch: return E_MANIFEST_KIND_ROOT_MISMATCH;
                case ManifestErrorCode.UnknownKind: return E_MANIFEST_UNKNOWN_KIND;
                case ManifestErrorCode.BadUnityVersion: return E_MANIFEST_BAD_UNITY_VERSION;
                case ManifestErrorCode.BadExporterVersion: return E_MANIFEST_BAD_EXPORTER_VERSION;
                case ManifestErrorCode.NegativeNumber: return E_MANIFEST_NEGATIVE_NUMBER;
                case ManifestErrorCode.TooLong: return E_MANIFEST_TOO_LONG;
                case ManifestErrorCode.TooMany: return E_MANIFEST_TOO_MANY;
                default: throw new ArgumentOutOfRangeException(nameof(code), code, null);
            }
        }

        /// <summary>The code for <see cref="CompatResult.Code"/> of a rejected package.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="code"/> is not a rejection.</exception>
        public static string ForRejection(CompatCode code)
        {
            switch (code)
            {
                case CompatCode.FormatTooNew: return E_COMPAT_FORMAT_TOO_NEW;
                case CompatCode.SchemaTooNew: return E_COMPAT_SCHEMA_TOO_NEW;
                case CompatCode.WrongTarget: return E_COMPAT_WRONG_TARGET;
                case CompatCode.WrongColorSpace: return E_COMPAT_WRONG_COLOR_SPACE;
                case CompatCode.DifferentStream: return E_COMPAT_DIFFERENT_STREAM;
                case CompatCode.NewerThanApp: return E_COMPAT_NEWER_THAN_APP;
                case CompatCode.KnownBadExporter: return E_COMPAT_KNOWN_BAD_EXPORTER;
                default: throw new ArgumentOutOfRangeException(nameof(code), code, null);
            }
        }

        /// <summary>The code for an entry of <see cref="CompatResult.Warnings"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="code"/> is never a warning.</exception>
        public static string ForWarning(CompatCode code)
        {
            switch (code)
            {
                case CompatCode.KnownBadExporter: return W_COMPAT_KNOWN_BAD_EXPORTER;
                case CompatCode.LegacyStreamAccepted: return W_COMPAT_LEGACY_STREAM_ACCEPTED;
                default: throw new ArgumentOutOfRangeException(nameof(code), code, null);
            }
        }

        /// <exception cref="ArgumentOutOfRangeException"><paramref name="support"/> is <see cref="ExporterUnitySupport.Supported"/>.</exception>
        public static string For(ExporterUnitySupport support)
        {
            switch (support)
            {
                case ExporterUnitySupport.Unity6AppOnly: return W_EXPORT_UNITY6_APP_ONLY;
                case ExporterUnitySupport.Unsupported: return E_EXPORT_UNSUPPORTED_UNITY;
                default: throw new ArgumentOutOfRangeException(nameof(support), support, null);
            }
        }
    }
}
