using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace OpenPerch.Fixtures.ExporterHarness
{
    /// <summary>
    /// Keeps <c>OPENPERCH_TEST_SUPPORT</c> in the Standalone scripting defines of this project, which is what
    /// compiles <c>OpenPerch.Runtime.TestSupport</c> (FixtureDump) here.
    /// </summary>
    /// <remarks>
    /// TestSupport is not auto-referenced, so only code in an assembly definition that references it can call
    /// FixtureDump. This file must stay outside any assembly whose define constraints require the symbol: such an
    /// assembly is not compiled until the symbol exists, so the symbol would never be added.
    /// </remarks>
    [InitializeOnLoad]
    public static class TestSupportDefine
    {
        public const string Symbol = "OPENPERCH_TEST_SUPPORT";

        static TestSupportDefine()
        {
            string current = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            var symbols = new List<string>();
            foreach (string part in current.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string symbol = part.Trim();
                if (symbol.Length > 0) symbols.Add(symbol);
            }

            // Writing the defines triggers a recompile, so only write when the symbol is missing; otherwise every
            // domain reload would start another one.
            if (symbols.Contains(Symbol)) return;

            symbols.Add(Symbol);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, string.Join(";", symbols));
            Debug.Log($"[ExporterHarness] Added {Symbol} to the Standalone scripting define symbols.");
        }
    }
}
