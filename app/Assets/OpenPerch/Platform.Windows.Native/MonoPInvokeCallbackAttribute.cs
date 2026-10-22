using System;

namespace OpenPerch.App.Platform.Windows.Native
{
    // IL2CPP recognizes this attribute by its type name alone. This noEngineReferences assembly cannot reach
    // UnityEngine's AOT.MonoPInvokeCallbackAttribute, so it declares its own copy under the same name.

    /// <summary>
    /// Marks a static method that native code calls through a function pointer, so an ahead-of-time compiler emits a
    /// reverse P/Invoke wrapper for it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class MonoPInvokeCallbackAttribute : Attribute
    {
        public MonoPInvokeCallbackAttribute(Type delegateType)
        {
            DelegateType = delegateType;
        }

        public Type DelegateType { get; }
    }
}
