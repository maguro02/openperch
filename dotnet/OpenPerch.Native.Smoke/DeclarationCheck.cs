using System;
using System.Reflection;
using System.Runtime.InteropServices;
using OpenPerch.App.Platform.Windows.Native;

namespace OpenPerch.Native.Smoke
{
    /// <summary>
    /// Resolves every declared export up front, so a misspelled entry point fails here even when no other check calls it.
    /// </summary>
    internal static class DeclarationCheck
    {
        private static readonly Type[] Aggregators =
        {
            typeof(User32), typeof(Dwmapi), typeof(Shell32), typeof(Shcore), typeof(Wtsapi32), typeof(Kernel32), typeof(Advapi32),
        };

        public static void Run(SmokeReport report)
        {
            int resolved = 0;
            int missing = 0;
            foreach (Type aggregator in Aggregators)
            {
                foreach (MethodInfo method in aggregator.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    DllImportAttribute import = method.GetCustomAttribute<DllImportAttribute>();
                    if (import == null)
                    {
                        continue;
                    }

                    string entryPoint = import.EntryPoint ?? method.Name;
                    if (NativeLibrary.TryLoad(import.Value, out IntPtr library) && NativeLibrary.TryGetExport(library, entryPoint, out _))
                    {
                        resolved++;
                    }
                    else
                    {
                        missing++;
                        report.Require(false, $"{aggregator.Name}.{method.Name} binds {import.Value}!{entryPoint}");
                    }
                }

                Marshal.PrelinkAll(aggregator);
            }

            report.Require(resolved > 0 && missing == 0, $"every declared entry point resolves ({resolved} declarations, {missing} missing)");
        }
    }
}
