using System;
using UnityEngine;

namespace OpenPerch.Runtime
{
    /// <summary>
    /// Root asset of an avatar bundle: the avatar prefab and what the app needs from the VRChat avatar descriptor.
    /// </summary>
    public sealed class AvatarDefinition : ScriptableObject
    {
        public int schemaVersion = 1;
        public string exportId;
        public string displayName;
        public GameObject avatarPrefab;

        /// <summary>Null when the avatar has no FX layer.</summary>
        public RuntimeAnimatorController fxController;

        public ExpressionParameterDef[] parameters = Array.Empty<ExpressionParameterDef>();
        public MenuPageDef[] menuPages = Array.Empty<MenuPageDef>();
        public EyeSettings eyes = new();
        public Vector3 viewPositionLocal;
        public float avatarHeightMeters;
        public ShaderVariantCollection warmupVariants;
    }
}
