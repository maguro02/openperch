using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OpenPerch.App.Platform.Windows.Native;
using OpenPerch.App.Tests.Shared;

namespace OpenPerch.App.Tests.EditMode
{
    public class PrivacyDeclarationTests
    {
        private static Assembly[] AppAssemblies()
        {
            var assemblies = new List<Assembly> { typeof(User32).Assembly };
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.IsDynamic &&
                    assembly.GetName().Name.StartsWith("OpenPerch.", StringComparison.Ordinal) &&
                    !assemblies.Contains(assembly))
                {
                    assemblies.Add(assembly);
                }
            }
            return assemblies.ToArray();
        }

        [Test]
        [Category("T-PRIV")]
        public void NoDeclarationReadsOtherWindowsOrProcesses()
        {
            Assert.That(PInvokeAudit.FindForbidden(PInvokeAudit.Collect(AppAssemblies())), Is.Empty);
        }

        [Test]
        [Category("T-PRIV")]
        public void NoUiAutomationOrMsaaComTypeIsImported()
        {
            Assert.That(PInvokeAudit.FindForbiddenComImports(AppAssemblies()), Is.Empty);
        }

        [Test]
        [Category("T-PRIV")]
        public void DeclarationsLiveOnlyInTheNativeAggregatorClasses()
        {
            Assert.That(PInvokeAudit.FindMisplaced(PInvokeAudit.Collect(AppAssemblies()), typeof(User32).Assembly), Is.Empty);
        }

        [Test]
        [Category("T-PRIV")]
        public void TheScanCoversTheAppAssembliesAndSeesTheNativeDeclarations()
        {
            Assembly[] assemblies = AppAssemblies();
            var bindings = PInvokeAudit.Collect(assemblies).Select(d => d.DeclaringType.Name + "!" + d.EntryPoint).ToList();

            Assert.That(assemblies, Has.Member(typeof(PrivacyDeclarationTests).Assembly));
            Assert.That(bindings, Has.Member("User32!EnumWindows"));
            Assert.That(bindings, Has.Member("User32!GetWindowLongPtrW"));
            Assert.That(bindings, Has.Member("Kernel32!CreateFileW"));
        }
    }
}
