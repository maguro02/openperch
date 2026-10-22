using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace OpenPerch.App.Tests.Shared
{
    /// <summary>A P/Invoke method found by <see cref="PInvokeAudit.Collect"/>.</summary>
    public readonly struct PInvokeDeclaration
    {
        public PInvokeDeclaration(MethodInfo method, DllImportAttribute import)
        {
            Method = method;
            Import = import;
        }

        public MethodInfo Method { get; }

        /// <summary>The method's <c>[DllImport]</c>, or null when the runtime does not surface it through reflection.</summary>
        public DllImportAttribute Import { get; }

        public Type DeclaringType => Method.DeclaringType;

        /// <summary>The library named in <c>[DllImport]</c>, or null when <see cref="Import"/> is unavailable.</summary>
        public string Library => Import?.Value;

        /// <summary>The exported name the runtime binds to.</summary>
        public string EntryPoint => Import?.EntryPoint ?? Method.Name;

        public override string ToString() =>
            DeclaringType.FullName + "." + Method.Name + " -> " + (Library ?? "?") + "!" + EntryPoint;
    }
}
