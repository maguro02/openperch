using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OpenPerch.App.Platform.Windows.Native;
using OpenPerch.App.Tests.Shared;

namespace OpenPerch.Core.Tests.Native
{
    public class PrivacyDeclarationTests
    {
        private static readonly Assembly[] Scanned = { typeof(User32).Assembly, typeof(PrivacyDeclarationTests).Assembly };

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
            Assert.That(PInvokeAudit.FindMisplaced(PInvokeAudit.Collect(Scanned)), Is.Empty);
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

        [TestCase("GetWindowTextW")]
        [TestCase("GetWindowTextLengthW")]
        [TestCase("InternalGetWindowText")]
        [TestCase("GetClassNameW")]
        [TestCase("OpenProcess")]
        [TestCase("QueryFullProcessImageNameW")]
        [TestCase("AccessibleObjectFromEvent")]
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
        [Category("T-PRIV")]
        public void AllowedNamesPass(string name)
        {
            Assert.That(PInvokeAudit.IsForbiddenName(name), Is.False);
        }
    }
}
