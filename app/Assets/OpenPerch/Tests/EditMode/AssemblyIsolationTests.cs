using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OpenPerch.App.Tests.EditMode
{
    /// <summary>
    /// Checks the assembly reference rules on the asmdef files of the app and of the packages, and on the assemblies the
    /// editor compiled from them.
    /// </summary>
    /// <remarks>
    /// The asmdef files alone miss references the editor adds by itself: it compiles every assembly against UnityEditor,
    /// so code under <c>#if UNITY_EDITOR</c> in a player assembly still builds, and an asmdef that does not override
    /// references gets every auto-referenced precompiled DLL. The compiler keeps only the references the code uses, so
    /// the compiled assemblies show those as well.
    /// </remarks>
    public class AssemblyIsolationTests
    {
        private const string RuntimeAssembly = "OpenPerch.Runtime";
        private const string AppPrefix = "OpenPerch.App.";
        private const string GuidPrefix = "GUID:";

        // The only App assemblies that never ship in the player: the editor tooling and the editor tests.
        private static readonly string[] EditorAppAssemblies = { "OpenPerch.App.Editor", "OpenPerch.App.Tests.EditMode" };

        private static readonly string[] EngineFreeAssemblies =
        {
            "OpenPerch.App.Core", "OpenPerch.Format", "OpenPerch.App.Platform.Windows.Native", "OpenPerch.App.Tests.Shared",
        };

        [Test]
        [Category("T-ISO")]
        public void RuntimeReferencesOnlyUnityEngine()
        {
            var assemblies = LoadAssemblies();
            Assert.That(assemblies.ContainsKey(RuntimeAssembly), Is.True, RuntimeAssembly + ".asmdef was not found.");
            var runtime = assemblies[RuntimeAssembly];

            var problems = new List<string>();
            problems.AddRange(References(runtime).Select(r => "references " + r));
            problems.AddRange(Safe(runtime.precompiledReferences).Select(r => "references the precompiled " + r));
            if (IsEditorOnly(runtime))
            {
                problems.Add("is editor-only");
            }

            if (runtime.noEngineReferences)
            {
                problems.Add("sets noEngineReferences");
            }

            Assert.That(problems, Is.Empty, RuntimeAssembly + " must reference UnityEngine and nothing else:\n" + string.Join("\n", problems));
        }

        [Test]
        [Category("T-ISO")]
        public void PlayerAppAssembliesDoNotReferenceUnityEditor()
        {
            var assemblies = LoadAssemblies();
            var playerApps = assemblies.Values
                .Where(a => a.name.StartsWith(AppPrefix, StringComparison.Ordinal) && !EditorAppAssemblies.Contains(a.name))
                .ToList();
            Assert.That(playerApps, Is.Not.Empty, "No " + AppPrefix + "* asmdef was found.");

            var problems = new List<string>();
            foreach (var app in playerApps)
            {
                if (IsEditorOnly(app))
                {
                    problems.Add(app.name + " is editor-only");
                }

                foreach (var name in WithReferences(app.name, assemblies))
                {
                    if (name.StartsWith("UnityEditor", StringComparison.Ordinal))
                    {
                        problems.Add(app.name + " -> " + name);
                    }
                    else if (name.StartsWith(GuidPrefix, StringComparison.Ordinal))
                    {
                        problems.Add(app.name + " -> " + name + ", which cannot be resolved to an assembly");
                    }
                    else if (assemblies.TryGetValue(name, out var reached))
                    {
                        if (name != app.name && IsEditorOnly(reached))
                        {
                            problems.Add(app.name + " -> " + name + ", which is editor-only");
                        }

                        problems.AddRange(Safe(reached.precompiledReferences)
                            .Where(dll => dll.StartsWith("UnityEditor", StringComparison.OrdinalIgnoreCase))
                            .Select(dll => app.name + " -> " + name + " -> " + dll));
                    }
                }
            }

            Assert.That(problems, Is.Empty,
                "App assemblies that ship in the player must not reach UnityEditor, directly or through other assemblies:\n" +
                string.Join("\n", problems));
        }

        [Test]
        [Category("T-ISO")]
        public void AppAssembliesDoNotReferenceTheVrchatSdk()
        {
            var assemblies = LoadAssemblies();
            var apps = assemblies.Values.Where(a => a.name.StartsWith(AppPrefix, StringComparison.Ordinal)).ToList();
            Assert.That(apps, Is.Not.Empty, "No " + AppPrefix + "* asmdef was found.");

            var problems = new List<string>();
            foreach (var app in apps)
            {
                foreach (var name in WithReferences(app.name, assemblies))
                {
                    if (IsVrchatSdk(name))
                    {
                        problems.Add(app.name + " -> " + name);
                    }
                    else if (name.StartsWith(GuidPrefix, StringComparison.Ordinal))
                    {
                        problems.Add(app.name + " -> " + name + ", which cannot be resolved to an assembly");
                    }
                    else if (assemblies.TryGetValue(name, out var reached))
                    {
                        problems.AddRange(Safe(reached.precompiledReferences)
                            .Where(IsVrchatSdk)
                            .Select(dll => app.name + " -> " + name + " -> " + dll));
                    }
                }
            }

            Assert.That(problems, Is.Empty,
                "App assemblies must not reach the VRChat SDK, directly or through other assemblies:\n" + string.Join("\n", problems));
        }

        [Test]
        [Category("T-ISO")]
        public void EngineFreeAssembliesDoNotReferenceUnityEngine()
        {
            var assemblies = LoadAssemblies();
            var problems = new List<string>();
            foreach (var engineFree in EngineFreeAssemblies)
            {
                if (!assemblies.TryGetValue(engineFree, out var asmdef))
                {
                    problems.Add(engineFree + ".asmdef was not found");
                    continue;
                }

                if (!asmdef.noEngineReferences)
                {
                    problems.Add(engineFree + " does not set noEngineReferences");
                }

                foreach (var name in WithReferences(engineFree, assemblies))
                {
                    if (!assemblies.TryGetValue(name, out var reached))
                    {
                        // Only the repository's own asmdefs can be inspected, so anything else may pull UnityEngine in.
                        problems.Add(engineFree + " -> " + name + ", which is not an asmdef of this repository");
                        continue;
                    }

                    if (name != engineFree && !reached.noEngineReferences)
                    {
                        problems.Add(engineFree + " -> " + name + ", which references UnityEngine");
                    }

                    problems.AddRange(Safe(reached.precompiledReferences)
                        .Where(dll => dll.StartsWith("Unity", StringComparison.OrdinalIgnoreCase))
                        .Select(dll => engineFree + " -> " + name + " -> " + dll));
                }
            }

            Assert.That(problems, Is.Empty,
                "These assemblies must not reach UnityEngine, directly or through other assemblies:\n" + string.Join("\n", problems));
        }

        [Test]
        [Category("T-ISO")]
        public void CompiledRuntimeReferencesOnlyBaseLibrariesAndUnityEngine()
        {
            var compiled = CompiledAssemblies();
            Assert.That(compiled.ContainsKey(RuntimeAssembly), Is.True, RuntimeAssembly + " is not loaded in the editor.");

            var problems = ReferencedNames(compiled[RuntimeAssembly])
                .Where(name => !IsBaseLibrary(name) && !IsNameOrDottedChild(name, "UnityEngine"))
                .ToList();

            Assert.That(problems, Is.Empty,
                RuntimeAssembly + " was compiled against more than the base libraries and UnityEngine:\n" + string.Join("\n", problems));
        }

        [Test]
        [Category("T-ISO")]
        public void CompiledAppAssembliesReferenceNeitherUnityEditorInThePlayerNorTheVrchatSdk()
        {
            var compiled = CompiledAssemblies();
            var apps = compiled.Keys.Where(name => name.StartsWith(AppPrefix, StringComparison.Ordinal)).ToList();
            Assert.That(apps.Where(name => !EditorAppAssemblies.Contains(name)), Is.Not.Empty,
                "No player " + AppPrefix + "* assembly is loaded.");

            var problems = new List<string>();
            foreach (var app in apps)
            {
                foreach (var name in ReferencedNames(compiled[app]))
                {
                    if (IsVrchatSdk(name))
                    {
                        problems.Add(app + " -> " + name);
                    }
                    else if (!EditorAppAssemblies.Contains(app) && name.StartsWith("UnityEditor", StringComparison.Ordinal))
                    {
                        problems.Add(app + " -> " + name);
                    }
                }
            }

            Assert.That(problems, Is.Empty,
                "App assemblies were compiled against the VRChat SDK, or against UnityEditor although they ship in the player:\n" +
                string.Join("\n", problems));
        }

        [Test]
        [Category("T-ISO")]
        public void CompiledEngineFreeAssembliesReferenceNoUnityAssembly()
        {
            var compiled = CompiledAssemblies();
            var loaded = EngineFreeAssemblies.Where(compiled.ContainsKey).ToList();
            Assert.That(loaded, Is.Not.Empty, "None of the engine-free assemblies is loaded in the editor.");

            var problems = loaded
                .SelectMany(engineFree => ReferencedNames(compiled[engineFree])
                    .Where(name => name.StartsWith("Unity", StringComparison.Ordinal))
                    .Select(name => engineFree + " -> " + name))
                .ToList();

            Assert.That(problems, Is.Empty,
                "These assemblies were compiled against a Unity assembly:\n" + string.Join("\n", problems));
        }

        /// <summary>
        /// The assemblies loaded in the editor, by name. An asmdef whose folder holds no script compiles to nothing, so its
        /// assembly is absent.
        /// </summary>
        private static Dictionary<string, Assembly> CompiledAssemblies()
        {
            var compiled = new Dictionary<string, Assembly>(StringComparer.Ordinal);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic))
            {
                var name = assembly.GetName().Name;
                if (!compiled.ContainsKey(name))
                {
                    compiled.Add(name, assembly);
                }
            }

            return compiled;
        }

        private static IEnumerable<string> ReferencedNames(Assembly assembly)
        {
            return assembly.GetReferencedAssemblies().Select(r => r.Name).OrderBy(n => n, StringComparer.Ordinal);
        }

        private static bool IsBaseLibrary(string name)
        {
            return name == "mscorlib" || name == "netstandard" || IsNameOrDottedChild(name, "System");
        }

        private static bool IsNameOrDottedChild(string name, string root)
        {
            return name == root || name.StartsWith(root + ".", StringComparison.Ordinal);
        }

        /// <summary>Every asmdef of the app project and of the packages in this repository, by assembly name.</summary>
        private static Dictionary<string, AssemblyDefinitionJson> LoadAssemblies()
        {
            var packagesRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "packages"));
            var files = Directory.GetFiles(Application.dataPath, "*.asmdef", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(packagesRoot, "*.asmdef", SearchOption.AllDirectories));

            var assemblies = new Dictionary<string, AssemblyDefinitionJson>(StringComparer.Ordinal);
            foreach (var file in files)
            {
                var asmdef = JsonUtility.FromJson<AssemblyDefinitionJson>(File.ReadAllText(file));
                assemblies.Add(asmdef.name, asmdef);
            }

            return assemblies;
        }

        /// <summary>The assembly itself and every assembly it reaches through the repository's asmdefs.</summary>
        private static IEnumerable<string> WithReferences(string name, IReadOnlyDictionary<string, AssemblyDefinitionJson> assemblies)
        {
            var reached = new HashSet<string>(StringComparer.Ordinal) { name };
            var pending = new Stack<string>();
            pending.Push(name);
            while (pending.Count > 0)
            {
                if (!assemblies.TryGetValue(pending.Pop(), out var asmdef))
                {
                    continue;
                }

                foreach (var reference in References(asmdef))
                {
                    if (reached.Add(reference))
                    {
                        pending.Push(reference);
                    }
                }
            }

            return reached;
        }

        private static IEnumerable<string> References(AssemblyDefinitionJson asmdef)
        {
            return Safe(asmdef.references).Select(Resolve);
        }

        private static string Resolve(string reference)
        {
            if (!reference.StartsWith(GuidPrefix, StringComparison.Ordinal))
            {
                return reference;
            }

            var path = AssetDatabase.GUIDToAssetPath(reference.Substring(GuidPrefix.Length));
            var asset = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            return asset == null ? reference : JsonUtility.FromJson<AssemblyDefinitionJson>(asset.text).name;
        }

        private static bool IsEditorOnly(AssemblyDefinitionJson asmdef)
        {
            var platforms = Safe(asmdef.includePlatforms);
            return platforms.Length > 0 && platforms.All(p => p == "Editor");
        }

        private static bool IsVrchatSdk(string name)
        {
            return name.StartsWith("VRC", StringComparison.OrdinalIgnoreCase) ||
                name.IndexOf("vrchat", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string[] Safe(string[] values)
        {
            return values ?? Array.Empty<string>();
        }
    }
}
