using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SillBill.PandaFacial.Editor
{
    internal readonly struct PandaFacialBlendShapeKey
    {
        internal PandaFacialBlendShapeKey(EditorCurveBinding binding, float value)
        {
            Binding = binding;
            Value = value;
        }

        internal EditorCurveBinding Binding { get; }
        internal float Value { get; }
    }

    internal static class PandaFacialAnimationUtility
    {
        internal static EditorCurveBinding CreateBlendShapeBinding(
            Transform animationRoot,
            SkinnedMeshRenderer renderer,
            string blendShapeName)
        {
            if (animationRoot == null)
                throw new ArgumentNullException(nameof(animationRoot));
            if (renderer == null)
                throw new ArgumentNullException(nameof(renderer));
            if (string.IsNullOrEmpty(blendShapeName))
                throw new ArgumentException("A BlendShape name is required.", nameof(blendShapeName));

            string path = AnimationUtility.CalculateTransformPath(renderer.transform, animationRoot);
            bool isRootOrChild = renderer.transform == animationRoot || renderer.transform.IsChildOf(animationRoot);
            if (!isRootOrChild)
                throw new ArgumentException("The target renderer is not under the Timeline track binding root.", nameof(renderer));

            return EditorCurveBinding.FloatCurve(
                path,
                typeof(SkinnedMeshRenderer),
                "blendShape." + blendShapeName);
        }

        internal static void WriteBlendShapeKey(
            AnimationClip clip,
            EditorCurveBinding binding,
            float time,
            float value)
        {
            ValidateWriteArguments(clip, time);

            Undo.RegisterCompleteObjectUndo(clip, "Write Panda Facial BlendShape Key");
            WriteBlendShapeKeyWithoutUndo(clip, binding, time, value);
        }

        internal static void WriteBlendShapeKeys(
            AnimationClip clip,
            IReadOnlyList<PandaFacialBlendShapeKey> keys,
            float time)
        {
            ValidateWriteArguments(clip, time);
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));
            if (keys.Count == 0)
                return;

            Undo.RegisterCompleteObjectUndo(clip, "Write Panda Facial Controller Keys");
            for (int i = 0; i < keys.Count; i++)
            {
                PandaFacialBlendShapeKey key = keys[i];
                WriteBlendShapeKeyWithoutUndo(clip, key.Binding, time, key.Value);
            }
        }

        private static void WriteBlendShapeKeyWithoutUndo(
            AnimationClip clip,
            EditorCurveBinding binding,
            float time,
            float value)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
            int existingKeyIndex = FindKeyAtTime(curve, time, clip.frameRate);
            if (existingKeyIndex >= 0)
            {
                Keyframe key = curve.keys[existingKeyIndex];
                key.time = time;
                key.value = value;
                curve.MoveKey(existingKeyIndex, key);
            }
            else
            {
                curve.AddKey(new Keyframe(time, value));
            }

            AnimationUtility.SetEditorCurve(clip, binding, curve);
            EditorUtility.SetDirty(clip);
        }

        private static void ValidateWriteArguments(AnimationClip clip, float time)
        {
            if (clip == null)
                throw new ArgumentNullException(nameof(clip));
            if (time < 0f || float.IsNaN(time) || float.IsInfinity(time))
                throw new ArgumentOutOfRangeException(nameof(time));
        }

        private static int FindKeyAtTime(AnimationCurve curve, float time, float frameRate)
        {
            float safeFrameRate = frameRate > 0f ? frameRate : 60f;
            float tolerance = Mathf.Min(0.0001f, 0.01f / safeFrameRate);
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                if (Mathf.Abs(keys[i].time - time) <= tolerance)
                    return i;
            }

            return -1;
        }
    }
}
