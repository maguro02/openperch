using System;
using UnityEngine;

namespace OpenPerch.Runtime
{
    /// <summary>Eye look and eyelid settings taken from the avatar descriptor.</summary>
    [Serializable]
    public sealed class EyeSettings
    {
        public bool enableEyeLook;
        public string leftEyePath = "";
        public string rightEyePath = "";
        public Quaternion straightL;
        public Quaternion straightR;
        public Quaternion upL;
        public Quaternion upR;
        public Quaternion downL;
        public Quaternion downR;
        public Quaternion leftL;
        public Quaternion leftR;
        public Quaternion rightL;
        public Quaternion rightR;
        public EyelidType eyelidType;
        public string eyelidMeshPath = "";

        // Blendshapes are kept by name because mesh optimizers such as Avatar Optimizer renumber blendshape indices.
        public string blinkShape = "";
        public string lookUpShape = "";
        public string lookDownShape = "";
    }
}
