using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SillBill.PandaFacial.Editor
{
    internal enum PandaFacialUpperFaceSection
    {
        EyeExpression,
        Brow
    }

    internal static class PandaFacialUpperFaceFoldoutState
    {
        private const string KeyPrefix = "SillBill.PandaFacial.UpperFaceSection.";

        private static readonly string[] EyeExpressionChannels =
        {
            PandaFacialSemanticChannels.EyeCloseL,
            PandaFacialSemanticChannels.EyeCloseR,
            PandaFacialSemanticChannels.EyeSmileL,
            PandaFacialSemanticChannels.EyeSmileR,
            PandaFacialSemanticChannels.EyeSurpriseL,
            PandaFacialSemanticChannels.EyeSurpriseR,
            PandaFacialSemanticChannels.EyeAngryL,
            PandaFacialSemanticChannels.EyeAngryR,
            PandaFacialSemanticChannels.EyeSadL,
            PandaFacialSemanticChannels.EyeSadR,
            PandaFacialSemanticChannels.EyeJitoL,
            PandaFacialSemanticChannels.EyeJitoR
        };

        private static readonly string[] BrowChannels =
        {
            PandaFacialSemanticChannels.BrowUpL,
            PandaFacialSemanticChannels.BrowUpR,
            PandaFacialSemanticChannels.BrowDownL,
            PandaFacialSemanticChannels.BrowDownR,
            PandaFacialSemanticChannels.BrowAngryL,
            PandaFacialSemanticChannels.BrowAngryR,
            PandaFacialSemanticChannels.BrowSadL,
            PandaFacialSemanticChannels.BrowSadR,
            PandaFacialSemanticChannels.BrowSmileL,
            PandaFacialSemanticChannels.BrowSmileR,
            PandaFacialSemanticChannels.BrowSeriousL,
            PandaFacialSemanticChannels.BrowSeriousR
        };

        internal static bool Get(
            PandaFacialAuthoringTarget target,
            PandaFacialUpperFaceSection section)
        {
            string key = GetKey(target, section);
            int stored = SessionState.GetInt(key, -1);
            if (stored >= 0)
                return stored == 1;

            bool initialValue = CountMapped(target, section) > 0;
            SessionState.SetInt(key, initialValue ? 1 : 0);
            return initialValue;
        }

        internal static void Set(
            PandaFacialAuthoringTarget target,
            PandaFacialUpperFaceSection section,
            bool isOpen)
        {
            SessionState.SetInt(GetKey(target, section), isOpen ? 1 : 0);
        }

        internal static int CountMapped(
            PandaFacialAuthoringTarget target,
            PandaFacialUpperFaceSection section)
        {
            IReadOnlyList<string> channels = GetChannels(section);
            int mapped = 0;
            for (int i = 0; i < channels.Count; i++)
            {
                if (PandaFacialMappingResolver.Resolve(target, channels[i]).IsMapped)
                    mapped++;
            }
            return mapped;
        }

        internal static int GetChannelCount(PandaFacialUpperFaceSection section)
        {
            return GetChannels(section).Count;
        }

        internal static IReadOnlyList<string> GetChannels(PandaFacialUpperFaceSection section)
        {
            return section == PandaFacialUpperFaceSection.Brow
                ? BrowChannels
                : EyeExpressionChannels;
        }

        internal static void Clear(PandaFacialAuthoringTarget target)
        {
            foreach (PandaFacialUpperFaceSection section in
                     System.Enum.GetValues(typeof(PandaFacialUpperFaceSection)))
            {
                SessionState.EraseInt(GetKey(target, section));
            }
        }

        private static string GetKey(
            PandaFacialAuthoringTarget target,
            PandaFacialUpperFaceSection section)
        {
            int instanceId = target != null ? target.GetInstanceID() : 0;
            return KeyPrefix + instanceId + "." + section;
        }
    }

    internal enum PandaFacialPadAxis
    {
        None,
        Horizontal,
        Vertical
    }

    internal static class PandaFacialAxisLock
    {
        internal const float DeadZone = 5f;

        internal static PandaFacialPadAxis Determine(Vector2 delta, float deadZone = DeadZone)
        {
            if (delta.magnitude < deadZone)
                return PandaFacialPadAxis.None;
            return Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                ? PandaFacialPadAxis.Horizontal
                : PandaFacialPadAxis.Vertical;
        }

        internal static PandaFacialPadAxis Resolve(
            PandaFacialPadAxis lockedAxis,
            Vector2 delta,
            float deadZone = DeadZone)
        {
            return lockedAxis == PandaFacialPadAxis.None
                ? Determine(delta, deadZone)
                : lockedAxis;
        }

        internal static PandaFacialEyelidState Apply(
            PandaFacialEyelidState start,
            Vector2 delta,
            Vector2 padSize,
            PandaFacialPadAxis axis)
        {
            if (axis == PandaFacialPadAxis.Horizontal)
                return new PandaFacialEyelidState(start.Expression + delta.x / padSize.x, start.Openness);
            if (axis == PandaFacialPadAxis.Vertical)
                return new PandaFacialEyelidState(start.Expression, start.Openness - delta.y / padSize.y);
            return start;
        }
    }

    internal static class PandaFacialUpperFaceSessionState
    {
        private const string KeyPrefix = "SillBill.PandaFacial.UpperFaceSession.";

        internal static bool GetEyelidSync(PandaFacialAuthoringTarget target) =>
            GetBool(target, "EyelidSync", true);

        internal static void SetEyelidSync(PandaFacialAuthoringTarget target, bool value) =>
            SetBool(target, "EyelidSync", value);

        internal static bool GetBrowSync(PandaFacialAuthoringTarget target) =>
            GetBool(target, "BrowSync", true);

        internal static void SetBrowSync(PandaFacialAuthoringTarget target, bool value) =>
            SetBool(target, "BrowSync", value);

        internal static float GetOpenExpression(PandaFacialAuthoringTarget target, bool isLeft) =>
            SessionState.GetFloat(GetKey(target, isLeft ? "ExpressionL" : "ExpressionR"), 1f);

        internal static void SetOpenExpression(
            PandaFacialAuthoringTarget target,
            bool isLeft,
            float value) =>
            SessionState.SetFloat(GetKey(target, isLeft ? "ExpressionL" : "ExpressionR"), Mathf.Clamp01(value));

        internal static void Clear(PandaFacialAuthoringTarget target)
        {
            SessionState.EraseInt(GetKey(target, "EyelidSync"));
            SessionState.EraseInt(GetKey(target, "BrowSync"));
            SessionState.EraseFloat(GetKey(target, "ExpressionL"));
            SessionState.EraseFloat(GetKey(target, "ExpressionR"));
        }

        private static bool GetBool(PandaFacialAuthoringTarget target, string suffix, bool fallback)
        {
            int value = SessionState.GetInt(GetKey(target, suffix), -1);
            return value < 0 ? fallback : value == 1;
        }

        private static void SetBool(PandaFacialAuthoringTarget target, string suffix, bool value) =>
            SessionState.SetInt(GetKey(target, suffix), value ? 1 : 0);

        private static string GetKey(PandaFacialAuthoringTarget target, string suffix)
        {
            int instanceId = target != null ? target.GetInstanceID() : 0;
            return KeyPrefix + instanceId + "." + suffix;
        }
    }
}
