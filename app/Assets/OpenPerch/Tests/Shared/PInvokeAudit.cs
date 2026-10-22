using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace OpenPerch.App.Tests.Shared
{
    /// <summary>
    /// Privacy and placement checks over P/Invoke declarations and source text: nothing that reads other applications'
    /// window titles, class names or modules, nothing that names other processes' images, no UI Automation or MSAA, and
    /// every declaration inside the <c>OpenPerch.App.Platform.Windows.Native</c> aggregator classes.
    /// </summary>
    public static class PInvokeAudit
    {
        public const string NativeNamespace = "OpenPerch.App.Platform.Windows.Native";

        private const BindingFlags AllMethods =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        private static readonly Dictionary<string, string> LibraryByAggregator = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "User32", "user32" },
            { "Dwmapi", "dwmapi" },
            { "Shell32", "shell32" },
            { "Shcore", "shcore" },
            { "Wtsapi32", "wtsapi32" },
            { "Kernel32", "kernel32" },
            { "Advapi32", "advapi32" },
        };

        // RealGetWindowClass reads another window's class name as GetClassName does, and GetWindowModuleFileName its
        // module path. The toolhelp snapshot, WTSEnumerateProcesses and NtQuerySystemInformation name a process's image
        // without OpenProcess.
        private static readonly string[] ForbiddenFragments =
        {
            "GetWindowText",
            "GetClassName",
            "RealGetWindowClass",
            "GetWindowModuleFileName",
            "QueryFullProcessImageName",
            "AccessibleObjectFrom",
            "CreateToolhelp32Snapshot",
            "Process32",
            "WTSEnumerateProcesses",
            "NtQuerySystemInformation",
        };

        // Every export of UI Automation and of MSAA reaches into other applications' UI.
        private static readonly string[] ForbiddenLibraries = { "uiautomationcore", "oleacc" };

        // Routes to the same information that need no [DllImport] of their own: UI Automation's COM and managed
        // clients, window messages, the managed process API and late-bound COM activation.
        private static readonly string[] ForbiddenSourceTokens =
        {
            "UIAutomation",
            "System.Windows.Automation",
            "WM_GETTEXT",
            "GetProcessById",
            "GetProcesses",
            "MainWindowTitle",
            "MainModule",
            ".ProcessName",
            "GetTypeFromCLSID",
            "GetTypeFromProgID",
        };

        private const string UiAutomationExportPrefix = "Uia";

        // NtOpenProcess and ZwOpenProcess end the same way; OpenProcessToken, which opens only our own token, does not.
        private static readonly Regex OpenProcessText = new Regex(@"OpenProcess\b", RegexOptions.CultureInvariant);

        private static readonly Guid CuiAutomationClass = new Guid("ff48dba4-60ef-4201-aa87-54103eef594e");
        private static readonly Guid CuiAutomation8Class = new Guid("e22ad333-b25f-460c-83d0-0581107395c9");
        private static readonly Guid UiAutomationInterface = new Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee");
        private static readonly Guid AccessibleInterface = new Guid("618736e0-3c3d-11cf-810c-00aa00389b71");

        private static readonly Guid[] ForbiddenComGuids = { CuiAutomationClass, CuiAutomation8Class, UiAutomationInterface, AccessibleInterface };

        private static readonly char[] PathSeparators = { '\\', '/' };

        /// <summary>Every method declared with <c>[DllImport]</c> in <paramref name="assemblies"/>, nested and private types included.</summary>
        public static List<PInvokeDeclaration> Collect(IEnumerable<Assembly> assemblies)
        {
            var declarations = new List<PInvokeDeclaration>();
            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    foreach (MethodInfo method in type.GetMethods(AllMethods))
                    {
                        if ((method.Attributes & MethodAttributes.PinvokeImpl) == 0)
                        {
                            continue;
                        }
                        var import = (DllImportAttribute)Attribute.GetCustomAttribute(method, typeof(DllImportAttribute), false);
                        declarations.Add(new PInvokeDeclaration(method, import));
                    }
                }
            }
            return declarations;
        }

        /// <summary>
        /// True for exported or method names that read another window's text, class or module, open or list other
        /// processes, reach MSAA or call UI Automation. <c>OpenProcessToken</c> stays allowed.
        /// </summary>
        public static bool IsForbiddenName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }
            foreach (string fragment in ForbiddenFragments)
            {
                if (name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return name.EndsWith("OpenProcess", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith(UiAutomationExportPrefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// True for UI Automation and MSAA, named as a library, a file or a Windows path in any case, with or without
        /// <c>.dll</c>.
        /// </summary>
        public static bool IsForbiddenLibrary(string library)
        {
            string stem = LibraryStem(library);
            foreach (string forbidden in ForbiddenLibraries)
            {
                if (string.Equals(stem, forbidden, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>True for the UI Automation and MSAA COM classes and interfaces, by GUID or by a name that mentions UI Automation.</summary>
        public static bool IsForbiddenComType(Guid guid, string typeName)
        {
            return Array.IndexOf(ForbiddenComGuids, guid) >= 0 ||
                (typeName != null && typeName.IndexOf("UIAutomation", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>Declarations that bind a forbidden export or load a forbidden library.</summary>
        public static List<string> FindForbidden(IEnumerable<PInvokeDeclaration> declarations)
        {
            var violations = new List<string>();
            foreach (PInvokeDeclaration declaration in declarations)
            {
                if (IsForbiddenName(declaration.EntryPoint) ||
                    IsForbiddenName(declaration.Method.Name) ||
                    IsForbiddenLibrary(declaration.Library))
                {
                    violations.Add(declaration.ToString());
                }
            }
            return violations;
        }

        /// <summary>COM interop types that import UI Automation or MSAA.</summary>
        public static List<string> FindForbiddenComImports(IEnumerable<Assembly> assemblies)
        {
            var violations = new List<string>();
            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsImport && IsForbiddenComType(type.GUID, type.Name))
                    {
                        violations.Add(type.FullName);
                    }
                }
            }
            return violations;
        }

        /// <summary>
        /// Declarations outside the internal static aggregator classes that <paramref name="nativeAssembly"/> defines in
        /// <see cref="NativeNamespace"/>, or bound to a library other than the one their class is named after. Each rule
        /// a declaration breaks is reported on its own.
        /// </summary>
        public static List<string> FindMisplaced(IEnumerable<PInvokeDeclaration> declarations, Assembly nativeAssembly)
        {
            if (nativeAssembly == null) throw new ArgumentNullException(nameof(nativeAssembly));

            var violations = new List<string>();
            foreach (PInvokeDeclaration declaration in declarations)
            {
                Type type = declaration.DeclaringType;
                string prefix = declaration + ": ";

                // Another assembly can declare a class under an aggregator's full name; only the assembly tells them apart.
                if (type.Assembly != nativeAssembly)
                {
                    violations.Add(prefix + "declared outside " + nativeAssembly.GetName().Name);
                }
                if (type.IsNested)
                {
                    violations.Add(prefix + "declared in a nested type");
                }

                string library = null;
                if (type.Namespace != NativeNamespace || !LibraryByAggregator.TryGetValue(type.Name, out library))
                {
                    violations.Add(prefix + "not one of the aggregator classes");
                }
                if (type.IsVisible || !(type.IsAbstract && type.IsSealed))
                {
                    violations.Add(prefix + "the declaring class is not internal static");
                }

                if (library == null)
                {
                    continue;
                }
                if (declaration.Library == null)
                {
                    violations.Add(prefix + "the [DllImport] metadata is not readable");
                }
                else if (!string.Equals(LibraryStem(declaration.Library), library, StringComparison.OrdinalIgnoreCase))
                {
                    violations.Add(prefix + "bound to a library other than " + library);
                }
            }
            return violations;
        }

        /// <summary>
        /// Forbidden exports, libraries, COM identities, window messages and managed members named anywhere in
        /// <paramref name="source"/>, so code that an <c>#if</c> keeps out of the current build is checked too. Names
        /// match case-sensitively, as the loader and the compiler bind them; library names and GUIDs match in any case.
        /// </summary>
        public static List<string> FindForbiddenText(string source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var found = new List<string>();
            foreach (string fragment in ForbiddenFragments)
            {
                if (source.IndexOf(fragment, StringComparison.Ordinal) >= 0)
                {
                    found.Add(fragment);
                }
            }
            foreach (string token in ForbiddenSourceTokens)
            {
                if (source.IndexOf(token, StringComparison.Ordinal) >= 0)
                {
                    found.Add(token);
                }
            }
            if (OpenProcessText.IsMatch(source))
            {
                found.Add("OpenProcess");
            }
            foreach (string library in ForbiddenLibraries)
            {
                if (Regex.IsMatch(source, @"\b" + library + @"\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    found.Add(library);
                }
            }
            foreach (Guid guid in ForbiddenComGuids)
            {
                string text = guid.ToString("D");
                if (source.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    found.Add(text);
                }
            }
            return found;
        }

        // [DllImport] takes Windows paths, which Path would not split at '\' on the Linux and macOS hosts running these checks.
        private static string LibraryStem(string library)
        {
            if (string.IsNullOrEmpty(library))
            {
                return "";
            }
            string name = library.Substring(library.LastIndexOfAny(PathSeparators) + 1);
            return name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? name.Substring(0, name.Length - 4) : name;
        }
    }
}
