using System;
using System.Linq;
using NUnit.Framework;
using OpenPerch.Runtime;
using UnityEngine;
using UnityEngine.Animations;

namespace OpenPerch.App.Tests.PlayMode
{
    public class BuiltinModuleTypeTests
    {
        // Every component type the sanitizer lets through, named with typeof: scripts compile only against the built-in
        // modules listed in Packages/manifest.json, so a module dropped from the manifest stops this assembly from
        // compiling, in the editor as well as in a player. Resolving a type by its name at run time could not catch that
        // in the editor, which loads every module's assembly whatever the manifest says.
        private static readonly Type[] AllowedComponentTypes =
        {
            typeof(Transform),
            typeof(Animator),
            typeof(SkinnedMeshRenderer),
            typeof(MeshRenderer),
            typeof(MeshFilter),
            typeof(ParticleSystem),
            typeof(ParticleSystemRenderer),
            typeof(TrailRenderer),
            typeof(LineRenderer),
            typeof(Cloth),
            typeof(SphereCollider),
            typeof(CapsuleCollider),
            typeof(PositionConstraint),
            typeof(RotationConstraint),
            typeof(ScaleConstraint),
            typeof(ParentConstraint),
            typeof(AimConstraint),
            typeof(LookAtConstraint),
            typeof(PerchPhysBone),
            typeof(PerchPhysBoneCollider),
            typeof(PerchConstraint),
            typeof(PerchContactData),
        };

        [Test]
        public void AllowedComponentTypesResolve()
        {
            var unresolved = AllowedComponentTypes
                .Where(t => t.IsAbstract || !typeof(Component).IsAssignableFrom(t) || Type.GetType(t.AssemblyQualifiedName, false) != t)
                .Select(t => t.FullName)
                .ToList();

            Assert.That(unresolved, Is.Empty,
                "Component types that are not concrete components or do not resolve by name in this build:\n" + string.Join("\n", unresolved));
        }
    }
}
