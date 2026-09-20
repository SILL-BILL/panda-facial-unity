using System.Collections.Generic;
using UnityEngine;

namespace SillBill.PandaFacial.Editor
{
    internal readonly struct PandaFacialControllerValueState
    {
        internal PandaFacialControllerValueState(
            float value,
            int mappedCount,
            int unmappedCount,
            int invalidCount,
            bool hasConflict,
            string warning)
        {
            Value = value;
            MappedCount = mappedCount;
            UnmappedCount = unmappedCount;
            InvalidCount = invalidCount;
            HasConflict = hasConflict;
            Warning = warning;
        }

        internal float Value { get; }
        internal int MappedCount { get; }
        internal int UnmappedCount { get; }
        internal int InvalidCount { get; }
        internal bool HasConflict { get; }
        internal string Warning { get; }
        internal bool HasWarning => !string.IsNullOrEmpty(Warning);
    }

    internal readonly struct PandaFacialControllerVector2State
    {
        internal PandaFacialControllerVector2State(
            PandaFacialControllerValueState x,
            PandaFacialControllerValueState y)
        {
            X = x;
            Y = y;
        }

        internal PandaFacialControllerValueState X { get; }
        internal PandaFacialControllerValueState Y { get; }
        internal Vector2 Value => new Vector2(X.Value, Y.Value);
    }

    /// <summary>
    /// Reconstructs controller values from mapped BlendShape weights.
    /// When opposing channels are both active, positive minus negative is used and a warning is returned.
    /// For bilateral width channels, each available side is averaged before that subtraction.
    /// </summary>
    internal static class PandaFacialControllerValueReader
    {
        private const float ActiveThreshold = 0.0001f;

        internal static PandaFacialControllerVector2State ReadMouthPosition(
            PandaFacialAuthoringTarget target)
        {
            return new PandaFacialControllerVector2State(
                ReadSignedPair(
                    target,
                    "Mouth Position X",
                    PandaFacialSemanticChannels.MouthLeft,
                    PandaFacialSemanticChannels.MouthRight),
                ReadSignedPair(
                    target,
                    "Mouth Position Y",
                    PandaFacialSemanticChannels.MouthDown,
                    PandaFacialSemanticChannels.MouthUp));
        }

        internal static PandaFacialControllerValueState ReadMouthCornerLeft(
            PandaFacialAuthoringTarget target)
        {
            return ReadSignedPair(
                target,
                "Mouth Corner L",
                PandaFacialSemanticChannels.MouthCornerDownL,
                PandaFacialSemanticChannels.MouthCornerUpL);
        }

        internal static PandaFacialControllerVector2State ReadMouthCornerLeftXY(
            PandaFacialAuthoringTarget target)
        {
            return new PandaFacialControllerVector2State(
                ReadSignedPair(
                    target,
                    "Left Corner X",
                    PandaFacialSemanticChannels.MouthNarrowL,
                    PandaFacialSemanticChannels.MouthSpreadL),
                ReadMouthCornerLeft(target));
        }

        internal static PandaFacialControllerValueState ReadMouthCornerRight(
            PandaFacialAuthoringTarget target)
        {
            return ReadSignedPair(
                target,
                "Mouth Corner R",
                PandaFacialSemanticChannels.MouthCornerDownR,
                PandaFacialSemanticChannels.MouthCornerUpR);
        }

        internal static PandaFacialControllerVector2State ReadMouthCornerRightXY(
            PandaFacialAuthoringTarget target)
        {
            return new PandaFacialControllerVector2State(
                ReadSignedPair(
                    target,
                    "Right Corner X",
                    PandaFacialSemanticChannels.MouthSpreadR,
                    PandaFacialSemanticChannels.MouthNarrowR),
                ReadMouthCornerRight(target));
        }

        internal static PandaFacialControllerValueState ReadMouthWidth(
            PandaFacialAuthoringTarget target)
        {
            var counters = new Counters();
            var narrow = new List<float>(2);
            var spread = new List<float>(2);
            ReadValue(target, PandaFacialSemanticChannels.MouthNarrowL, narrow, ref counters);
            ReadValue(target, PandaFacialSemanticChannels.MouthNarrowR, narrow, ref counters);
            ReadValue(target, PandaFacialSemanticChannels.MouthSpreadL, spread, ref counters);
            ReadValue(target, PandaFacialSemanticChannels.MouthSpreadR, spread, ref counters);

            float narrowAverage = Average(narrow);
            float spreadAverage = Average(spread);
            bool conflict = narrowAverage > ActiveThreshold && spreadAverage > ActiveThreshold;
            return State(
                "Mouth Width",
                Mathf.Clamp(spreadAverage - narrowAverage, -1f, 1f),
                counters,
                conflict);
        }

        internal static PandaFacialControllerValueState ReadSingle(
            PandaFacialAuthoringTarget target,
            string label,
            string semanticId)
        {
            var counters = new Counters();
            float value = ReadValue(target, semanticId, ref counters);
            return State(label, value, counters, false);
        }

        private static PandaFacialControllerValueState ReadSignedPair(
            PandaFacialAuthoringTarget target,
            string label,
            string negativeSemanticId,
            string positiveSemanticId)
        {
            var counters = new Counters();
            float negative = ReadValue(target, negativeSemanticId, ref counters);
            float positive = ReadValue(target, positiveSemanticId, ref counters);
            bool conflict = negative > ActiveThreshold && positive > ActiveThreshold;
            return State(
                label,
                Mathf.Clamp(positive - negative, -1f, 1f),
                counters,
                conflict);
        }

        private static float ReadValue(
            PandaFacialAuthoringTarget target,
            string semanticId,
            ref Counters counters)
        {
            IReadOnlyList<PandaFacialResolvedMapping> resolvedTargets =
                PandaFacialMappingResolver.ResolveAll(target, semanticId);
            float value = 0f;
            bool foundReadableTarget = false;
            for (int i = 0; i < resolvedTargets.Count; i++)
            {
                PandaFacialResolvedMapping resolved = resolvedTargets[i];
                if (resolved.IsMapped)
                {
                    counters.Mapped++;
                    if (!foundReadableTarget && resolved.WeightMultiplier > ActiveThreshold)
                    {
                        value = ReadNormalizedWeight(resolved);
                        foundReadableTarget = true;
                    }
                }
                else if (resolved.Status == PandaFacialMappingStatus.Unmapped)
                {
                    counters.Unmapped++;
                }
                else if (resolved.Status != PandaFacialMappingStatus.Disabled)
                {
                    counters.Invalid++;
                }
            }
            return value;
        }

        private static void ReadValue(
            PandaFacialAuthoringTarget target,
            string semanticId,
            List<float> destination,
            ref Counters counters)
        {
            int mappedBefore = counters.Mapped;
            float value = ReadValue(target, semanticId, ref counters);
            if (counters.Mapped > mappedBefore)
                destination.Add(value);
        }

        private static float ReadNormalizedWeight(PandaFacialResolvedMapping mapping)
        {
            return Mathf.Clamp01(
                mapping.Renderer.GetBlendShapeWeight(mapping.BlendShapeIndex) /
                (100f * mapping.WeightMultiplier));
        }

        private static float Average(List<float> values)
        {
            if (values.Count == 0)
                return 0f;
            float sum = 0f;
            for (int i = 0; i < values.Count; i++)
                sum += values[i];
            return sum / values.Count;
        }

        private static PandaFacialControllerValueState State(
            string label,
            float value,
            Counters counters,
            bool conflict)
        {
            string warning = null;
            if (counters.Unmapped > 0 || counters.Invalid > 0 || conflict)
            {
                warning = label + ": " + counters.Mapped + " mapped, " +
                          counters.Unmapped + " unmapped, " + counters.Invalid + " invalid.";
                if (conflict)
                    warning += " Opposing channels are both active; displaying positive minus negative.";
            }

            return new PandaFacialControllerValueState(
                value,
                counters.Mapped,
                counters.Unmapped,
                counters.Invalid,
                conflict,
                warning);
        }

        private struct Counters
        {
            internal int Mapped;
            internal int Unmapped;
            internal int Invalid;
        }
    }
}
