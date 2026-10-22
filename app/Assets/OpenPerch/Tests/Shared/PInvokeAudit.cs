using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace OpenPerch.App.Tests.Shared
{
    /// <summary>
    /// Reflection checks over P/Invoke declarations: no API that reads other applications' window titles, class names or
    /// process images, no UI Automation, and every declaration inside the <c>OpenPerch.App.Platform.Windows.Native</c>
    /// aggregator classes.
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

        private static readonly string[] ForbiddenFragments =
        {
            "GetWindowText",
            "GetClassName",
            "QueryFullProcessImageName",
            "AccessibleObjectFromEvent",
        };

        private const string UiAutomationLibrary = "uiautomationcore";
        private const string UiAutomationExportPrefix = "Uia";

        private static readonly Guid CuiAutomationClass = new Guid("ff48dba4-60ef-4201-aa87-54103eef594e");
        private static readonly Guid CuiAutomation8Class = new Guid("e22ad333-b25f-460c-83d0-0581107395c9");
        private static readonly Guid UiAutomationInterface = new Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee");
        private static readonly Guid AccessibleInterface = new Guid("618736e0-3c3d-11cf-810c-00aa00389b71");

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
        /// True for exported or method names that read window text or class names, open other processes, query process
        /// images, reach MSAA events or call UI Automation. <c>OpenProcessToken</c> stays allowed.
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

        /// <summary>Declarations that bind a forbidden export or load the UI Automation library.</summary>
        public static List<string> FindForbidden(IEnumerable<PInvokeDeclaration> declarations)
        {
            var violations = new List<string>();
            foreach (PInvokeDeclaration declaration in declarations)
            {
                if (IsForbiddenName(declaration.EntryPoint) ||
                    IsForbiddenName(declaration.Method.Name) ||
                    string.Equals(LibraryStem(declaration.Library), UiAutomationLibrary, StringComparison.OrdinalIgnoreCase))
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
                    if (!type.IsImport)
                    {
                        continue;
                    }
                    Guid guid = type.GUID;
                    if (guid == CuiAutomationClass || guid == CuiAutomation8Class || guid == UiAutomationInterface ||
                        guid == AccessibleInterface || type.Name.IndexOf("UIAutomation", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        violations.Add(type.FullName);
                    }
                }
            }
            return violations;
        }

        /// <summary>
        /// Declarations outside the internal static aggregator classes of <see cref="NativeNamespace"/>, or bound to a
        /// library other than the one their class is named after.
        /// </summary>
        public static List<string> FindMisplaced(IEnumerable<PInvokeDeclaration> declarations)
        {
            var violations = new List<string>();
            foreach (PInvokeDeclaration declaration in declarations)
            {
                Type type = declaration.DeclaringType;
                if (type.IsNested || type.Namespace != NativeNamespace || !LibraryByAggregator.TryGetValue(type.Name, out string library))
                {
                    violations.Add(declaration + ": outside the Native aggregator classes");
                }
                else if (type.IsPublic || !(type.IsAbstract && type.IsSealed))
                {
                    violations.Add(declaration + ": the aggregator class is not internal static");
                }
                else if (declaration.Library == null)
                {
                    violations.Add(declaration + ": the [DllImport] metadata is not readable");
                }
                else if (!string.Equals(LibraryStem(declaration.Library), library, StringComparison.OrdinalIgnoreCase))
                {
                    violations.Add(declaration + ": bound to a library other than " + library);
                }
            }
            return violations;
        }

        private static string LibraryStem(string library) =>
            string.IsNullOrEmpty(library) ? "" : Path.GetFileNameWithoutExtension(library);
    }
}
