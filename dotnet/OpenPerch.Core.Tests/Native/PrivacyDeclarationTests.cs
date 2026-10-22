using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;
using OpenPerch.App.Platform.Windows.Native;
using OpenPerch.App.Tests.Shared;

namespace OpenPerch.Core.Tests.Native
{
    public class PrivacyDeclarationTests
    {
        private static readonly Assembly[] Scanned = { typeof(User32).Assembly, typeof(PrivacyDeclarationTests).Assembly };

        private static readonly Assembly NativeAssembly = typeof(User32).Assembly;

        private static readonly MethodInfo HarmlessUser32Method = typeof(User32).GetMethod(nameof(User32.GetWindowRect));

        [Test]
        [Category("T-PRIV")]
        public void NoDeclarationReadsOtherWindowsOrProcesses()
        {
            Assert.That(PInvokeAudit.FindForbidden(PInvokeAudit.Collect(Scanned)), Is.Empty);
        }

        [Test]
        [Category("T-PRIV")]
        public void NoUiAutomationOrMsaaComTypeIsImported()
        {
            Assert.That(PInvokeAudit.FindForbiddenComImports(Scanned), Is.Empty);
        }

        [Test]
        [Category("T-PRIV")]
        public void DeclarationsLiveOnlyInTheNativeAggregatorClasses()
        {
            Assert.That(PInvokeAudit.FindMisplaced(PInvokeAudit.Collect(Scanned), NativeAssembly), Is.Empty);
        }

        [Test]
        [Category("T-PRIV")]
        public void TheScanSeesTheNativeDeclarations()
        {
            var bindings = PInvokeAudit.Collect(Scanned).Select(d => d.DeclaringType.Name + "!" + d.EntryPoint).ToList();

            Assert.That(bindings, Has.Member("User32!EnumWindows"));
            Assert.That(bindings, Has.Member("User32!GetWindowLongPtrW"));
            Assert.That(bindings, Has.Member("Kernel32!CreateFileW"));
            Assert.That(bindings, Has.Member("Advapi32!OpenProcessToken"));
        }

        [Test]
        [Category("T-PRIV")]
        public void TheScanReachesNonPublicDeclarationsInNestedTypes()
        {
            // The runtime library reaches the OS through non-public stubs in nested interop classes.
            List<PInvokeDeclaration> declarations = PInvokeAudit.Collect(new[] { typeof(object).Assembly });

            Assert.That(declarations, Has.Some.Matches<PInvokeDeclaration>(d => d.DeclaringType.IsNested && !d.Method.IsPublic));
        }

        [TestCase("user32.dll", "GetWindowTextW")]
        [TestCase("user32.dll", "RealGetWindowClassW")]
        [TestCase("kernel32.dll", "Process32NextW")]
        [TestCase("ntdll.dll", "NtOpenProcess")]
        [Category("T-PRIV")]
        public void AHarmlessMethodBoundToAForbiddenExportIsReported(string library, string entryPoint)
        {
            var declaration = new PInvokeDeclaration(HarmlessUser32Method, new DllImportAttribute(library) { EntryPoint = entryPoint });

            Assert.That(PInvokeAudit.FindForbidden(new[] { declaration }), Has.Count.EqualTo(1));
        }

        [TestCase("uiautomationcore.dll")]
        [TestCase("UIAutomationCore")]
        [TestCase("oleacc.dll")]
        [TestCase(@"C:\Windows\System32\OLEACC.DLL")]
        [Category("T-PRIV")]
        public void AnyExportOfUiAutomationOrMsaaIsReported(string library)
        {
            var declaration = new PInvokeDeclaration(HarmlessUser32Method, new DllImportAttribute(library) { EntryPoint = "GetWindowRect" });

            Assert.That(PInvokeAudit.FindForbidden(new[] { declaration }), Has.Count.EqualTo(1));
        }

        [Test]
        [Category("T-PRIV")]
        public void AnAllowedBindingIsNotReported()
        {
            var declaration = new PInvokeDeclaration(HarmlessUser32Method, new DllImportAttribute("user32.dll") { EntryPoint = "GetWindowRect" });

            Assert.That(PInvokeAudit.FindForbidden(new[] { declaration }), Is.Empty);
        }

        [Test]
        [Category("T-PRIV")]
        public void AggregatorNamesInAnotherAssemblyAreMisplaced()
        {
            List<PInvokeDeclaration> declarations = PInvokeAudit.Collect(new[] { NativeAssembly });

            List<string> violations = PInvokeAudit.FindMisplaced(declarations, typeof(PrivacyDeclarationTests).Assembly);

            Assert.That(violations, Has.Count.EqualTo(declarations.Count));
            Assert.That(violations, Has.All.Contains("declared outside OpenPerch.Core.Tests"));
        }

        [Test]
        [Category("T-PRIV")]
        public void ANestedDeclaringTypeIsMisplaced()
        {
            MethodInfo nested = typeof(Dictionary<int, int>.Enumerator).GetMethod(nameof(Dictionary<int, int>.Enumerator.MoveNext));

            Assert.That(Misplacements(nested, "user32.dll"), Has.Some.Contains("nested type"));
        }

        [Test]
        [Category("T-PRIV")]
        public void AClassOutsideTheNativeNamespaceIsMisplaced()
        {
            MethodInfo test = typeof(PrivacyDeclarationTests).GetMethod(nameof(AClassOutsideTheNativeNamespaceIsMisplaced));

            Assert.That(Misplacements(test, "user32.dll"), Has.Some.Contains("not one of the aggregator classes"));
        }

        [Test]
        [Category("T-PRIV")]
        public void ANativeClassNotNamedAfterALibraryIsMisplaced()
        {
            MethodInfo parser = typeof(ZoneIdentifier).GetMethod(nameof(ZoneIdentifier.TryParseZoneId));

            List<string> violations = Misplacements(parser, "kernel32.dll");

            Assert.That(violations, Has.Count.EqualTo(1));
            Assert.That(violations[0], Does.Contain("not one of the aggregator classes"));
        }

        [Test]
        [Category("T-PRIV")]
        public void APublicDeclaringClassIsMisplaced()
        {
            MethodInfo publicStatic = typeof(Math).GetMethod(nameof(Math.Sqrt));

            Assert.That(Misplacements(publicStatic, "user32.dll"), Has.Some.Contains("not internal static"));
        }

        [Test]
        [Category("T-PRIV")]
        public void AnInstanceDeclaringClassIsMisplaced()
        {
            MethodInfo instance = typeof(MonoPInvokeCallbackAttribute).GetProperty(nameof(MonoPInvokeCallbackAttribute.DelegateType)).GetMethod;

            Assert.That(Misplacements(instance, "user32.dll"), Has.Some.Contains("not internal static"));
        }

        [Test]
        [Category("T-PRIV")]
        public void AnAggregatorBoundToAnotherLibraryIsMisplaced()
        {
            List<string> violations = Misplacements(HarmlessUser32Method, "kernel32.dll");

            Assert.That(violations, Has.Count.EqualTo(1));
            Assert.That(violations[0], Does.Contain("other than user32"));
        }

        [Test]
        [Category("T-PRIV")]
        public void AnAggregatorWithoutReadableMetadataIsMisplaced()
        {
            var declaration = new PInvokeDeclaration(HarmlessUser32Method, null);

            List<string> violations = PInvokeAudit.FindMisplaced(new[] { declaration }, NativeAssembly);

            Assert.That(violations, Has.Count.EqualTo(1));
            Assert.That(violations[0], Does.Contain("not readable"));
        }

        [TestCase("user32.dll")]
        [TestCase("USER32")]
        [TestCase(@"C:\Windows\System32\user32.dll")]
        [Category("T-PRIV")]
        public void AnAggregatorBoundToItsOwnLibraryIsInPlace(string library)
        {
            Assert.That(Misplacements(HarmlessUser32Method, library), Is.Empty);
        }

        [TestCase("GetWindowTextW")]
        [TestCase("GetWindowTextLengthW")]
        [TestCase("InternalGetWindowText")]
        [TestCase("GetClassNameW")]
        [TestCase("RealGetWindowClassW")]
        [TestCase("RealGetWindowClassA")]
        [TestCase("GetWindowModuleFileNameW")]
        [TestCase("OpenProcess")]
        [TestCase("NtOpenProcess")]
        [TestCase("QueryFullProcessImageNameW")]
        [TestCase("AccessibleObjectFromEvent")]
        [TestCase("AccessibleObjectFromWindow")]
        [TestCase("AccessibleObjectFromPoint")]
        [TestCase("CreateToolhelp32Snapshot")]
        [TestCase("Process32FirstW")]
        [TestCase("Process32NextW")]
        [TestCase("WTSEnumerateProcessesW")]
        [TestCase("WTSEnumerateProcessesExW")]
        [TestCase("NtQuerySystemInformation")]
        [TestCase("UiaNodeFromHandle")]
        [Category("T-PRIV")]
        public void ForbiddenNamesAreRecognized(string name)
        {
            Assert.That(PInvokeAudit.IsForbiddenName(name), Is.True);
        }

        [TestCase("OpenProcessToken")]
        [TestCase("FindWindowW")]
        [TestCase("GetWindowThreadProcessId")]
        [TestCase("GetWindowRect")]
        [TestCase("EnumWindows")]
        [TestCase("WTSRegisterSessionNotification")]
        [Category("T-PRIV")]
        public void AllowedNamesPass(string name)
        {
            Assert.That(PInvokeAudit.IsForbiddenName(name), Is.False);
        }

        [TestCase("uiautomationcore.dll", true)]
        [TestCase("UIAutomationCore", true)]
        [TestCase("oleacc", true)]
        [TestCase(@"C:\Windows\System32\OLEACC.DLL", true)]
        [TestCase("user32.dll", false)]
        [TestCase("oleaut32.dll", false)]
        [TestCase(null, false)]
        [Category("T-PRIV")]
        public void AccessibilityLibrariesAreRecognizedInAnySpelling(string library, bool forbidden)
        {
            Assert.That(PInvokeAudit.IsForbiddenLibrary(library), Is.EqualTo(forbidden));
        }

        [TestCase("ff48dba4-60ef-4201-aa87-54103eef594e", "IHarmless", true)]
        [TestCase("618736e0-3c3d-11cf-810c-00aa00389b71", "IHarmless", true)]
        [TestCase("00000000-0000-0000-0000-000000000000", "IUIAutomationElement", true)]
        [TestCase("00000101-0000-0000-c000-000000000046", "IEnumString", false)]
        [Category("T-PRIV")]
        public void UiAutomationAndMsaaComTypesAreRecognized(string guid, string typeName, bool forbidden)
        {
            Assert.That(PInvokeAudit.IsForbiddenComType(new Guid(guid), typeName), Is.EqualTo(forbidden));
        }

        private static List<string> Misplacements(MethodInfo method, string library) =>
            PInvokeAudit.FindMisplaced(new[] { new PInvokeDeclaration(method, new DllImportAttribute(library)) }, NativeAssembly);
    }
}
