using System;
using System.Collections.Generic;
using OpenPerch.Runtime;
using UnityEditor;
using UnityEngine;

namespace OpenPerch.Fixtures.ExporterHarness
{
    /// <summary>
    /// Generates one humanoid <see cref="AnimationClip"/> for every <see cref="MotionRole"/> from muscle curves and
    /// saves them under <see cref="OutputFolder"/>. The clips stand in for user motions in exporter and app tests.
    /// </summary>
    /// <remarks>
    /// Muscle value 0 is Unity's humanoid reference pose (hips and knees bent, arms forward and down), not the
    /// T-pose. A straight standing leg is therefore Upper Leg Front-Back 0.6 with Lower Leg Stretch 1, arms hanging
    /// at the sides are about Arm Down-Up -0.6, and lower Front-Back values swing a limb forward.
    /// RootT is the body (centre of mass) position in units of the avatar's human scale: about 1 when standing.
    /// </remarks>
    public static class GenerateTestMotions
    {
        public const string OutputFolder = "Assets/TestMotions";

        private const float SampleRate = 30f;
        private const float SlopeStep = 1f / 240f;
        private const float StandingBodyHeight = 1f;
        private const float WalkCycle = 1f;
        private const float WalkStride = 0.6f;

        // Fingers, eyes and jaw keep their defaults: the roles differ only in body pose, and finger curves use
        // property names ("LeftHand.Index.1 Stretched") that differ from HumanTrait.MuscleName.
        private static readonly HumanBodyBones[] AnimatedBones =
        {
            HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest, HumanBodyBones.Neck,
            HumanBodyBones.Head,
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes,
            HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot,
            HumanBodyBones.RightToes,
            HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm,
            HumanBodyBones.LeftHand,
            HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm,
            HumanBodyBones.RightHand,
        };

        private static readonly string[] RootProperties =
        {
            "RootT.x", "RootT.y", "RootT.z", "RootQ.x", "RootQ.y", "RootQ.z", "RootQ.w",
        };

        private static readonly Dictionary<string, int> MuscleIndex = BuildMuscleIndex();

        /// <summary>
        /// Writes <c>TestMotion_&lt;role&gt;.anim</c> for every role. Existing clips are overwritten in place.
        /// Also usable from batch mode through <c>-executeMethod</c>.
        /// </summary>
        [MenuItem("OpenPerch/Generate Test Motions")]
        public static void Generate()
        {
            int[] muscles = BodyMuscles();
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder("Assets", "TestMotions");
            }

            var roles = (MotionRole[])Enum.GetValues(typeof(MotionRole));
            foreach (MotionRole role in roles)
            {
                AnimationClip clip = BuildClip(role, muscles);
                if (!clip.humanMotion)
                {
                    Debug.LogWarning($"[ExporterHarness] {clip.name} is not recognized as a humanoid clip.");
                }
                Save(clip, $"{OutputFolder}/{clip.name}.anim");
            }

            Debug.Log($"[ExporterHarness] Generated {roles.Length} test motions in {OutputFolder}.");
        }

        private static string ClipName(MotionRole role) => "TestMotion_" + role;

        private static float LengthOf(MotionRole role) => role switch
        {
            MotionRole.Idle => 4f,
            MotionRole.IdleVariant => 3f,
            MotionRole.Walk => WalkCycle,
            MotionRole.SitOnEdge => 3f,
            MotionRole.Dragged => 1.6f,
            MotionRole.Fall => 0.8f,
            MotionRole.Land => 0.6f,
            MotionRole.SitOnFloor => 4f,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "No test motion is defined for this role."),
        };

        // The exporter's role presets play IdleVariant and Land once; every other role loops.
        private static bool Loops(MotionRole role) => role != MotionRole.IdleVariant && role != MotionRole.Land;

        private static AnimationClip BuildClip(MotionRole role, int[] muscles)
        {
            float length = LengthOf(role);
            int frames = Mathf.Max(1, Mathf.RoundToInt(length * SampleRate));
            string[] properties = PropertyNames(muscles);
            int channels = properties.Length;

            var keys = new Keyframe[channels][];
            for (int c = 0; c < channels; c++)
            {
                keys[c] = new Keyframe[frames + 1];
            }

            var pose = new float[HumanTrait.MuscleCount];
            var now = new float[channels];
            var before = new float[channels];
            var after = new float[channels];
            for (int f = 0; f <= frames; f++)
            {
                float t = length * f / frames;
                Sample(role, t, muscles, pose, now);
                Sample(role, t - SlopeStep, muscles, pose, before);
                Sample(role, t + SlopeStep, muscles, pose, after);
                for (int c = 0; c < channels; c++)
                {
                    float slope = (after[c] - before[c]) / (2f * SlopeStep);
                    keys[c][f] = new Keyframe(t, now[c], slope, slope);
                }
            }

            var clip = new AnimationClip { name = ClipName(role), frameRate = SampleRate };
            for (int c = 0; c < channels; c++)
            {
                EditorCurveBinding binding = EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), properties[c]);
                AnimationUtility.SetEditorCurve(clip, binding, new AnimationCurve(Compact(keys[c], length)));
            }

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = Loops(role);
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        private static void Save(AnimationClip clip, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(clip, path);
                return;
            }

            // Copying into the existing asset keeps its GUID, so controllers that use the clip keep working.
            EditorUtility.CopySerialized(clip, existing);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssetIfDirty(existing);
            UnityEngine.Object.DestroyImmediate(clip);
        }

        // Every body muscle gets a curve, even a constant one: a humanoid clip treats a missing muscle as 0, which
        // is the crouched reference pose rather than "leave unchanged".
        private static int[] BodyMuscles()
        {
            var muscles = new List<int>();
            foreach (HumanBodyBones bone in AnimatedBones)
            {
                for (int dof = 0; dof < 3; dof++)
                {
                    int muscle = HumanTrait.MuscleFromBone((int)bone, dof);
                    if (muscle >= 0)
                    {
                        muscles.Add(muscle);
                    }
                }
            }
            muscles.Sort();
            return muscles.ToArray();
        }

        private static string[] PropertyNames(int[] muscles)
        {
            string[] muscleNames = HumanTrait.MuscleName;
            var names = new string[muscles.Length + RootProperties.Length];
            for (int i = 0; i < muscles.Length; i++)
            {
                names[i] = muscleNames[muscles[i]];
            }
            RootProperties.CopyTo(names, muscles.Length);
            return names;
        }

        private static Dictionary<string, int> BuildMuscleIndex()
        {
            string[] names = HumanTrait.MuscleName;
            var index = new Dictionary<string, int>(names.Length);
            for (int i = 0; i < names.Length; i++)
            {
                index[names[i]] = i;
            }
            return index;
        }

        private static Keyframe[] Compact(Keyframe[] keys, float length)
        {
            float first = keys[0].value;
            for (int i = 1; i < keys.Length; i++)
            {
                if (Mathf.Abs(keys[i].value - first) > 1e-6f)
                {
                    return keys;
                }
            }
            return new[] { new Keyframe(0f, first), new Keyframe(length, first) };
        }

        private static void Sample(MotionRole role, float t, int[] muscles, float[] pose, float[] channels)
        {
            EvaluatePose(role, t, pose, out Vector3 rootT);
            for (int i = 0; i < muscles.Length; i++)
            {
                channels[i] = pose[muscles[i]];
            }

            int root = muscles.Length;
            channels[root] = rootT.x;
            channels[root + 1] = rootT.y;
            channels[root + 2] = rootT.z;
            channels[root + 3] = 0f;
            channels[root + 4] = 0f;
            channels[root + 5] = 0f;
            channels[root + 6] = 1f;
        }

        private static void EvaluatePose(MotionRole role, float t, float[] m, out Vector3 rootT)
        {
            Array.Clear(m, 0, m.Length);
            Stand(m);
            rootT = new Vector3(0f, StandingBodyHeight, 0f);
            switch (role)
            {
                case MotionRole.Idle:
                    Idle(t, m);
                    break;
                case MotionRole.IdleVariant:
                    IdleVariant(t, m);
                    break;
                case MotionRole.Walk:
                    Walk(t, m, ref rootT);
                    break;
                case MotionRole.SitOnEdge:
                    SitOnEdge(t, m, ref rootT);
                    break;
                case MotionRole.Dragged:
                    Dragged(t, m);
                    break;
                case MotionRole.Fall:
                    Fall(t, m);
                    break;
                case MotionRole.Land:
                    Land(t, m, ref rootT);
                    break;
                case MotionRole.SitOnFloor:
                    SitOnFloor(t, m, ref rootT);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "No test motion is defined for this role.");
            }
        }

        private static void Stand(float[] m)
        {
            Both(m, "Upper Leg Front-Back", 0.6f);
            Both(m, "Lower Leg Stretch", 0.97f);
            Both(m, "Arm Down-Up", -0.62f);
            Both(m, "Arm Front-Back", 0.25f);
            Both(m, "Forearm Stretch", 0.85f);
        }

        private static void Idle(float t, float[] m)
        {
            float breath = Wave(t, 4f);
            Set(m, "Spine Front-Back", 0.015f * breath);
            Set(m, "Chest Front-Back", 0.03f * breath);
            Both(m, "Shoulder Down-Up", 0.05f * breath);
            Both(m, "Arm Down-Up", -0.62f + 0.02f * breath);
            Set(m, "Head Nod Down-Up", 0.03f * Wave(t, 4f, 0.25f));
        }

        private static void IdleVariant(float t, float[] m)
        {
            float raise = Ramp(t, 0f, 0.5f) * (1f - Ramp(t, 2.4f, 3f));
            float sway = 0.12f * Wave(t, 0.5f);
            Set(m, "Right Arm Down-Up", Mathf.Lerp(-0.62f, 0.8f + sway, raise));
            Set(m, "Right Arm Front-Back", Mathf.Lerp(0.25f, 0.1f, raise));
            Set(m, "Right Forearm Stretch", Mathf.Lerp(0.85f, 0.7f, raise));
            Set(m, "Head Tilt Left-Right", 0.15f * raise);
        }

        private static void Walk(float t, float[] m, ref Vector3 rootT)
        {
            float swing = Wave(t, WalkCycle);
            float passing = Wave(t, WalkCycle, 0.25f);
            Sides(m, "Upper Leg Front-Back", 0.5f - 0.35f * swing, 0.5f + 0.35f * swing);
            Sides(m, "Lower Leg Stretch", KneeSwing(passing), KneeSwing(-passing));
            Sides(m, "Arm Front-Back", 0.25f + 0.2f * swing, 0.25f - 0.2f * swing);
            Both(m, "Arm Down-Up", -0.6f);
            Both(m, "Forearm Stretch", 0.8f);
            Set(m, "Spine Twist Left-Right", 0.06f * swing);
            Set(m, "Chest Twist Left-Right", -0.06f * swing);

            // The walk keeps its forward travel as root motion; the body is lowest in double support.
            rootT.z = WalkStride * t / WalkCycle;
            rootT.y = StandingBodyHeight + 0.015f * Wave(t, WalkCycle / 2f, 0.25f);
        }

        // The knee bends most while the leg swings through under the body.
        private static float KneeSwing(float passing)
        {
            float k = 0.5f + 0.5f * passing;
            return 0.9f - 0.625f * k * k;
        }

        private static void SitOnEdge(float t, float[] m, ref Vector3 rootT)
        {
            float kick = Wave(t, 3f);
            Both(m, "Upper Leg Front-Back", -0.62f);
            Both(m, "Upper Leg In-Out", 0.05f);
            Sides(m, "Lower Leg Stretch", -0.125f + 0.2f * kick, -0.125f - 0.2f * kick);
            Both(m, "Foot Up-Down", 0.15f);
            Both(m, "Arm Down-Up", -0.6f);
            Both(m, "Arm Front-Back", 0.45f);
            Both(m, "Forearm Stretch", 0.95f);
            Set(m, "Head Nod Down-Up", -0.08f + 0.03f * Wave(t, 3f, 0.25f));

            // Thighs sit at the height of a chair seat above the root, so the seat height estimated from the
            // upper legs lands on the window edge and the lower legs hang below it.
            rootT.y = 0.62f;
        }

        private static void Dragged(float t, float[] m)
        {
            float swing = Wave(t, 1.6f);
            Both(m, "Arm Down-Up", 0.55f);
            Both(m, "Arm Front-Back", 0.1f);
            Both(m, "Forearm Stretch", 0.7f);
            Sides(m, "Upper Leg Front-Back", 0.45f + 0.1f * swing, 0.45f - 0.1f * swing);
            Sides(m, "Lower Leg Stretch", 0.75f - 0.1f * swing, 0.75f + 0.1f * swing);
            Set(m, "Head Nod Down-Up", -0.1f);
        }

        private static void Fall(float t, float[] m)
        {
            float flail = Wave(t, 0.8f);
            Sides(m, "Arm Down-Up", 0.75f + 0.1f * flail, 0.75f - 0.1f * flail);
            Both(m, "Arm Front-Back", 0.15f);
            Both(m, "Forearm Stretch", 0.6f);
            Sides(m, "Upper Leg Front-Back", 0.3f + 0.08f * flail, 0.3f - 0.08f * flail);
            Sides(m, "Lower Leg Stretch", 0.6f - 0.1f * flail, 0.6f + 0.1f * flail);
            Set(m, "Head Nod Down-Up", 0.1f);
        }

        private static void Land(float t, float[] m, ref Vector3 rootT)
        {
            float crouch = Ramp(t, 0f, 0.12f) * (1f - Ramp(t, 0.2f, 0.6f));
            Both(m, "Upper Leg Front-Back", Mathf.Lerp(0.6f, -0.25f, crouch));
            Both(m, "Lower Leg Stretch", Mathf.Lerp(0.97f, -0.1f, crouch));
            Both(m, "Foot Up-Down", Mathf.Lerp(0f, -0.35f, crouch));
            Set(m, "Spine Front-Back", Mathf.Lerp(0f, -0.2f, crouch));
            Both(m, "Arm Down-Up", Mathf.Lerp(-0.62f, 0f, crouch));
            Both(m, "Arm Front-Back", Mathf.Lerp(0.25f, -0.1f, crouch));
            Both(m, "Forearm Stretch", Mathf.Lerp(0.85f, 0.6f, crouch));
            rootT.y = Mathf.Lerp(StandingBodyHeight, 0.72f, crouch);
        }

        private static void SitOnFloor(float t, float[] m, ref Vector3 rootT)
        {
            float sway = Wave(t, 4f);
            Both(m, "Upper Leg Front-Back", -0.55f);
            Both(m, "Upper Leg In-Out", 0.08f);
            Sides(m, "Lower Leg Stretch", 0.8f + 0.1f * sway, 0.8f - 0.1f * sway);
            Both(m, "Foot Up-Down", 0.2f);
            Set(m, "Spine Front-Back", 0.1f);
            Both(m, "Arm Down-Up", -0.55f);
            Both(m, "Arm Front-Back", 0.75f);
            Both(m, "Forearm Stretch", 1f);
            Set(m, "Head Tilt Left-Right", 0.06f * sway);
            rootT.y = 0.28f;
        }

        private static float Wave(float t, float period, float phase = 0f) =>
            Mathf.Sin(2f * Mathf.PI * (t / period + phase));

        private static float Ramp(float t, float start, float end)
        {
            float x = Mathf.Clamp01((t - start) / (end - start));
            return x * x * (3f - 2f * x);
        }

        private static void Set(float[] m, string muscle, float value)
        {
            if (!MuscleIndex.TryGetValue(muscle, out int index))
            {
                throw new ArgumentException($"'{muscle}' is not a humanoid muscle name.", nameof(muscle));
            }
            m[index] = value;
        }

        private static void Both(float[] m, string muscle, float value)
        {
            Set(m, "Left " + muscle, value);
            Set(m, "Right " + muscle, value);
        }

        private static void Sides(float[] m, string muscle, float left, float right)
        {
            Set(m, "Left " + muscle, left);
            Set(m, "Right " + muscle, right);
        }
    }
}
