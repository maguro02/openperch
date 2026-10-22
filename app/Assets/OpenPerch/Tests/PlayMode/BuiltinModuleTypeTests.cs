using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace OpenPerch.App.Tests.PlayMode
{
    public class BuiltinModuleTypeTests
    {
        // Every component type the sanitizer lets through. Unity's types only resolve in a player when the built-in
        // module that provides them is in Packages/manifest.json, so this catches a module dropped from the manifest
        // when the tests run in a player.
        private static readonly string[] AllowedComponentTypes =
        {
            "UnityEngine.Transform",
            "UnityEngine.Animator",
            "UnityEngine.SkinnedMeshRenderer",
            "UnityEngine.MeshRenderer",
            "UnityEngine.MeshFilter",
            "UnityEngine.ParticleSystem",
            "UnityEngine.ParticleSystemRenderer",
            "UnityEngine.TrailRenderer",
            "UnityEngine.LineRenderer",
            "UnityEngine.Cloth",
            "UnityEngine.SphereCollider",
            "UnityEngine.CapsuleCollider",
            "UnityEngine.Animations.PositionConstraint",
            "UnityEngine.Animations.RotationConstraint",
            "UnityEngine.Animations.ScaleConstraint",
            "UnityEngine.Animations.ParentConstraint",
            "UnityEngine.Animations.AimConstraint",
            "UnityEngine.Animations.LookAtConstraint",
            "OpenPerch.Runtime.PerchPhysBone",
            "OpenPerch.Runtime.PerchPhysBoneCollider",
            "OpenPerch.Runtime.PerchConstraint",
            "OpenPerch.Runtime.PerchContactData",
        };

        [Test]
        public void AllowedComponentTypesResolve()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).ToList();
            var unresolved = AllowedComponentTypes
                .Where(name => !assemblies
                    .Select(a => a.GetType(name, false))
                    .Any(t => t != null && typeof(Component).IsAssignableFrom(t)))
                .ToList();

            Assert.That(unresolved, Is.Empty, "Component types missing from this build:\n" + string.Join("\n", unresolved));
        }
    }
}
