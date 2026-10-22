using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenPerch.App.Tests.Shared;
using OpenPerch.Core.Tests.Support;

namespace OpenPerch.Core.Tests.Native
{
    /// <summary>
    /// Reads the Unity sources as text, which also covers the platform <c>#if</c> branches that neither this build nor
    /// the macOS editor compiles.
    /// </summary>
    public class PrivacySourceTests
    {
        private const string NativeFolder = "app/Assets/OpenPerch/Platform.Windows.Native/";
        private const string AuditFile = "app/Assets/OpenPerch/Tests/Shared/PInvokeAudit.cs";

        private static readonly string[] ScannedRoots = { "app/Assets", "packages" };

        // A P/Invoke cannot avoid both: the attribute in either spelling, and the extern modifier.
        private static readonly Regex NativeDeclaration = new Regex(@"\bDllImport(?:Attribute)?\s*\(|\bextern\b", RegexOptions.CultureInvariant);

        [Test]
        [Category("T-PRIV")]
        public void OnlyTheNativeFolderDeclaresNativeEntryPoints()
        {
            var offenders = ReadSources()
                .Where(source => !source.Key.StartsWith(NativeFolder, StringComparison.Ordinal) && NativeDeclaration.IsMatch(source.Value))
                .Select(source => source.Key)
                .ToList();

            Assert.That(offenders, Is.Empty);
        }

        [Test]
        [Category("T-PRIV")]
        public void NoSourceNamesAnApiThatReadsOtherWindowsOrProcesses()
        {
            var offenders = ReadSources()
                .Where(source => source.Key != AuditFile)
                .SelectMany(source => PInvokeAudit.FindForbiddenText(source.Value).Select(token => source.Key + ": " + token))
                .ToList();

            Assert.That(offenders, Is.Empty);
        }

        [Test]
        [Category("T-PRIV")]
        public void TheScanReadsTheNativeDeclarationsAndTheAuditList()
        {
            List<KeyValuePair<string, string>> sources = ReadSources();

            var declaring = sources
                .Where(source => source.Key.StartsWith(NativeFolder, StringComparison.Ordinal) && NativeDeclaration.IsMatch(source.Value))
                .Select(source => Path.GetFileName(source.Key))
                .ToList();
            Assert.That(declaring, Has.Member("User32.cs"));
            Assert.That(declaring, Has.Member("Kernel32.cs"));
            Assert.That(PInvokeAudit.FindForbiddenText(sources.Single(source => source.Key == AuditFile).Value), Is.Not.Empty);
        }

        [TestCase("[DllImport(\"user32.dll\")]")]
        [TestCase("[System.Runtime.InteropServices.DllImportAttribute (\"user32.dll\")]")]
        [TestCase("private extern static int Probe();")]
        [TestCase("internal static extern int Probe();")]
        [Category("T-PRIV")]
        public void DeclarationsAreRecognized(string text)
        {
            Assert.That(NativeDeclaration.IsMatch(text), Is.True);
        }

        [TestCase("var import = (DllImportAttribute)Attribute.GetCustomAttribute(method, typeof(DllImportAttribute));")]
        [TestCase("// opens the folder in an external file manager")]
        [Category("T-PRIV")]
        public void ReflectionOverDeclarationsIsNotADeclaration(string text)
        {
            Assert.That(NativeDeclaration.IsMatch(text), Is.False);
        }

        [TestCase("[DllImport(\"user32.dll\", EntryPoint = \"GetWindowTextW\")] static extern int Title(IntPtr hwnd, char[] text, int max);", "GetWindowText")]
        [TestCase("#if UNITY_STANDALONE_WIN && !UNITY_EDITOR\nstring title = Process.GetProcessById(pid).MainWindowTitle;\n#endif", "MainWindowTitle")]
        [TestCase("string name = process.ProcessName;", ".ProcessName")]
        [TestCase("string path = Process.GetProcessById(pid).MainModule.FileName;", "MainModule")]
        [TestCase("foreach (Process other in Process.GetProcessesByName(name)) { }", "GetProcesses")]
        [TestCase("object automation = Activator.CreateInstance(Type.GetTypeFromCLSID(clsid));", "GetTypeFromCLSID")]
        [TestCase("SendMessage(hwnd, WM_GETTEXT, new IntPtr(256), buffer);", "WM_GETTEXT")]
        [TestCase("IntPtr process = OpenProcess(0x1000, false, pid);", "OpenProcess")]
        [TestCase("[DllImport(\"ntdll.dll\")] static extern int NtOpenProcess(out IntPtr handle, uint access, IntPtr attributes, IntPtr client);", "OpenProcess")]
        [TestCase("[DllImport(\"OLEACC.DLL\")] static extern int AccessibleChildren(IntPtr container, int start, int count, object[] found, out int obtained);", "oleacc")]
        [TestCase("IntPtr module = LoadLibrary(\"UIAutomationCore.dll\");", "uiautomationcore")]
        [TestCase("[ComImport, Guid(\"30CBE57D-D9D0-452A-AB13-7AC5AC4825EE\")] interface IHarmless { }", "30cbe57d-d9d0-452a-ab13-7ac5ac4825ee")]
        [Category("T-PRIV")]
        public void ForbiddenTextIsFound(string source, string token)
        {
            Assert.That(PInvokeAudit.FindForbiddenText(source), Has.Member(token));
        }

        [TestCase("Advapi32.OpenProcessToken(Kernel32.GetCurrentProcess(), Advapi32.TOKEN_QUERY, out IntPtr token);")]
        [TestCase("User32.GetWindowThreadProcessId(hwnd, out uint pid);")]
        [TestCase("int own = Process.GetCurrentProcess().Id;")]
        [TestCase("string targetClassName = nameof(SurfaceKind);")]
        [TestCase("long wholeAccount = 0;")]
        [Category("T-PRIV")]
        public void AllowedTextPasses(string source)
        {
            Assert.That(PInvokeAudit.FindForbiddenText(source), Is.Empty);
        }

        private static List<KeyValuePair<string, string>> ReadSources()
        {
            var sources = new List<KeyValuePair<string, string>>();
            foreach (string root in ScannedRoots)
            {
                foreach (string path in Directory.EnumerateFiles(RepoPaths.Combine(root), "*.cs", SearchOption.AllDirectories))
                {
                    string relative = Path.GetRelativePath(RepoPaths.Root, path).Replace('\\', '/');
                    sources.Add(new KeyValuePair<string, string>(relative, File.ReadAllText(path)));
                }
            }
            return sources;
        }
    }
}
