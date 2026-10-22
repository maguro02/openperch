using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;
using OpenPerch.App.Platform.Windows.Native;
using OpenPerch.App.Tests.Shared;

namespace OpenPerch.Core.Tests.Native
{
    public class InteropDeclarationTests
    {
        private static List<PInvokeDeclaration> NativeDeclarations() =>
            PInvokeAudit.Collect(new[] { typeof(User32).Assembly })
                .Where(d => d.DeclaringType.Namespace == PInvokeAudit.NativeNamespace)
                .ToList();

        [Test]
        public void EveryDeclarationIsUnicodeWithExactSpelling()
        {
            var offenders = NativeDeclarations()
                .Where(d => d.Import == null || d.Import.CharSet != CharSet.Unicode || !d.Import.ExactSpelling)
                .Select(d => d.ToString())
                .ToList();

            Assert.That(offenders, Is.Empty);
        }

        [Test]
        public void DeclarationsThatPassTextBindTheWideExport()
        {
            var offenders = NativeDeclarations()
                .Where(d => d.Method.GetParameters().Any(p => CarriesText(p.ParameterType)) && !d.EntryPoint.EndsWith("W", StringComparison.Ordinal))
                .Select(d => d.ToString())
                .ToList();

            Assert.That(offenders, Is.Empty);
        }

        [TestCase(nameof(User32.GetWindowLongPtr), "GetWindowLongPtrW")]
        [TestCase(nameof(User32.SetWindowLongPtr), "SetWindowLongPtrW")]
        public void WindowLongPtrBindsTheWide64BitExport(string method, string entryPoint)
        {
            var import = typeof(User32).GetMethod(method).GetCustomAttribute<DllImportAttribute>();

            Assert.That(import.EntryPoint, Is.EqualTo(entryPoint));
        }

        [TestCase(typeof(RECT), 16)]
        [TestCase(typeof(POINT), 8)]
        [TestCase(typeof(MSG), 48)]
        [TestCase(typeof(MONITORINFOEXW), 104)]
        [TestCase(typeof(APPBARDATA), 48)]
        [TestCase(typeof(NOTIFYICONDATAW), 976)]
        [TestCase(typeof(WNDCLASSEXW), 80)]
        [TestCase(typeof(TOKEN_ELEVATION), 4)]
        [TestCase(typeof(POWERBROADCAST_SETTING), 24)]
        public void StructSizesMatchTheX64Headers(Type type, int size)
        {
            Assume.That(IntPtr.Size, Is.EqualTo(8), "the player is 64-bit only");

            Assert.That(Marshal.SizeOf(type), Is.EqualTo(size));
        }

        [TestCase(typeof(MSG), "pt", 36)]
        [TestCase(typeof(MONITORINFOEXW), "szDevice", 40)]
        [TestCase(typeof(APPBARDATA), "rc", 24)]
        [TestCase(typeof(NOTIFYICONDATAW), "szTip", 40)]
        [TestCase(typeof(NOTIFYICONDATAW), "uVersion", 816)]
        [TestCase(typeof(NOTIFYICONDATAW), "guidItem", 952)]
        [TestCase(typeof(NOTIFYICONDATAW), "hBalloonIcon", 968)]
        [TestCase(typeof(WNDCLASSEXW), "lpszClassName", 64)]
        [TestCase(typeof(POWERBROADCAST_SETTING), "Data", 20)]
        public void FieldOffsetsMatchTheX64Headers(Type type, string field, int offset)
        {
            Assume.That(IntPtr.Size, Is.EqualTo(8), "the player is 64-bit only");

            Assert.That(Marshal.OffsetOf(type, field).ToInt32(), Is.EqualTo(offset));
        }

        [Test]
        public void CallbackAttributeKeepsTheNameAheadOfTimeCompilersLookFor()
        {
            var usage = typeof(MonoPInvokeCallbackAttribute).GetCustomAttribute<AttributeUsageAttribute>();

            Assert.That(typeof(MonoPInvokeCallbackAttribute).Name, Is.EqualTo("MonoPInvokeCallbackAttribute"));
            Assert.That(usage.ValidOn, Is.EqualTo(AttributeTargets.Method));
        }

        private static bool CarriesText(Type type)
        {
            if (type.IsByRef)
            {
                type = type.GetElementType();
            }
            if (type == typeof(string) || type == typeof(StringBuilder) || type == typeof(char[]))
            {
                return true;
            }
            return type.IsValueType && !type.IsPrimitive &&
                type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(f => f.FieldType == typeof(string));
        }
    }
}
