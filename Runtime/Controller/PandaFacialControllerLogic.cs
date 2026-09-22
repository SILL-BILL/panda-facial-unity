using System;
using System.Collections.Generic;
using UnityEngine;

namespace SillBill.PandaFacial
{
    public readonly struct PandaFacialSemanticWeight
    {
        public PandaFacialSemanticWeight(string semanticId, float weight)
        {
            if (string.IsNullOrEmpty(semanticId))
                throw new ArgumentException("A semantic channel ID is required.", nameof(semanticId));

            SemanticId = semanticId;
            Weight = Mathf.Clamp(Sanitize(weight), 0f, 100f);
        }

        public string SemanticId { get; }
        public float Weight { get; }

        private static float Sanitize(float value)
        {
            return float.IsNaN(value) ? 0f : value;
        }
    }

    /// <summary>
    /// Converts animator-facing controller values into semantic weights only.
    /// It intentionally has no knowledge of renderers, BlendShape names, Timeline, or UI.
    /// </summary>
    public static class PandaFacialControllerLogic
    {
        public static IReadOnlyList<PandaFacialSemanticWeight> MouthPositionX(float value)
        {
            float normalized = ClampSigned(value);
            return Result(
                Weight(PandaFacialSemanticChannels.MouthLeft, NegativeWeight(normalized)),
                Weight(PandaFacialSemanticChannels.MouthRight, PositiveWeight(normalized)));
        }

        public static IReadOnlyList<PandaFacialSemanticWeight> MouthPosition(float x, float y)
        {
            float normalizedX = ClampSigned(x);
            float normalizedY = ClampSigned(y);
            return Result(
                Weight(PandaFacialSemanticChannels.MouthLeft, NegativeWeight(normalizedX)),
                Weight(PandaFacialSemanticChannels.MouthRight, PositiveWeight(normalizedX)),
                Weight(PandaFacialSemanticChannels.MouthDown, NegativeWeight(normalizedY)),
                Weight(PandaFacialSemanticChannels.MouthUp, PositiveWeight(normalizedY)));
        }

        public static IReadOnlyList<PandaFacialSemanticWeight> MouthPositionY(float value)
        {
            float normalized = ClampSigned(value);
            return Result(
                Weight(PandaFacialSemanticChannels.MouthDown, NegativeWeight(normalized)),
                Weight(PandaFacialSemanticChannels.MouthUp, PositiveWeight(normalized)));
        }

        public static IReadOnlyList<PandaFacialSemanticWeight> MouthWidth(float value)
        {
            float normalized = ClampSigned(value);
            float narrow = NegativeWeight(normalized);
            float spread = PositiveWeight(normalized);
            return Result(
                Weight(PandaFacialSemanticChannels.MouthNarrowL, narrow),
                Weight(PandaFacialSemanticChannels.MouthNarrowR, narrow),
                Weight(PandaFacialSemanticChannels.MouthSpreadL, spread),
                Weight(PandaFacialSemanticChannels.MouthSpreadR, spread));
        }

        public static IReadOnlyList<PandaFacialSemanticWeight> MouthCornerLeft(float y)
        {
            float normalized = ClampSigned(y);
            return Result(
                Weight(PandaFacialSemanticChannels.MouthCornerDownL, NegativeWeight(normalized)),
                Weight(PandaFacialSemanticChannels.MouthCornerUpL, PositiveWeight(normalized)));
        }

        /// <summary>
        /// Evaluates the left corner in screen coordinates: positive X is Outer/Spread,
        /// negative X is Inner/Narrow, and positive Y is Up.
        /// </summary>
        public static IReadOnlyList<PandaFacialSemanticWeight> MouthCornerLeft(float x, float y)
        {
            float normalizedX = ClampSigned(x);
            float normalizedY = ClampSigned(y);
            return Result(
                Weight(PandaFacialSemanticChannels.MouthNarrowL, NegativeWeight(normalizedX)),
                Weight(PandaFacialSemanticChannels.MouthSpreadL, PositiveWeight(normalizedX)),
                Weight(PandaFacialSemanticChannels.MouthCornerDownL, NegativeWeight(normalizedY)),
                Weight(PandaFacialSemanticChannels.MouthCornerUpL, PositiveWeight(normalizedY)));
        }

        public static IReadOnlyList<PandaFacialSemanticWeight> MouthCornerRight(float y)
        {
            float normalized = ClampSigned(y);
            return Result(
                Weight(PandaFacialSemanticChannels.MouthCornerDownR, NegativeWeight(normalized)),
                Weight(PandaFacialSemanticChannels.MouthCornerUpR, PositiveWeight(normalized)));
        }

        /// <summary>
        /// Evaluates the right corner in screen coordinates: negative X is Outer/Spread,
        /// positive X is Inner/Narrow, and positive Y is Up.
        /// </summary>
        public static IReadOnlyList<PandaFacialSemanticWeight> MouthCornerRight(float x, float y)
        {
            float normalizedX = ClampSigned(x);
            float normalizedY = ClampSigned(y);
            return Result(
                Weight(PandaFacialSemanticChannels.MouthSpreadR, NegativeWeight(normalizedX)),
                Weight(PandaFacialSemanticChannels.MouthNarrowR, PositiveWeight(normalizedX)),
                Weight(PandaFacialSemanticChannels.MouthCornerDownR, NegativeWeight(normalizedY)),
                Weight(PandaFacialSemanticChannels.MouthCornerUpR, PositiveWeight(normalizedY)));
        }

        public static IReadOnlyList<PandaFacialSemanticWeight> MouthCorners(
            float leftX,
            float leftY,
            float rightX,
            float rightY)
        {
            var outputs = new List<PandaFacialSemanticWeight>(8);
            Add(outputs, MouthCornerLeft(leftX, leftY));
            Add(outputs, MouthCornerRight(rightX, rightY));
            return outputs.AsReadOnly();
        }

        public static IReadOnlyList<PandaFacialSemanticWeight> MirrorMouthCornersFromLeft(
            float leftX,
            float leftY)
        {
            return MouthCorners(leftX, leftY, -leftX, leftY);
        }

        public static IReadOnlyList<PandaFacialSemanticWeight> MirrorMouthCornersFromRight(
            float rightX,
            float rightY)
        {
            return MouthCorners(-rightX, rightY, rightX, rightY);
        }

        public static IReadOnlyList<PandaFacialSemanticWeight> Vowels(
            float a,
            float i,
            float u,
            float e,
            float o)
        {
            return Result(
                NormalizedWeight(PandaFacialSemanticChannels.MouthA, a),
                NormalizedWeight(PandaFacialSemanticChannels.MouthI, i),
                NormalizedWeight(PandaFacialSemanticChannels.MouthU, u),
                NormalizedWeight(PandaFacialSemanticChannels.MouthE, e),
                NormalizedWeight(PandaFacialSemanticChannels.MouthO, o));
        }

        public static IReadOnlyList<PandaFacialSemanticWeight> Blink(float left, float right)
        {
            return Pair(
                PandaFacialSemanticChannels.EyeCloseL,
                PandaFacialSemanticChannels.EyeCloseR,
                left,
                right);
        }

        public static IReadOnlyList<PandaFacialSemanticWeight> Pair(
            string leftSemanticId,
            string rightSemanticId,
            float left,
            float right)
        {
            return Result(
                NormalizedWeight(leftSemanticId, left),
                NormalizedWeight(rightSemanticId, right));
        }

        public static IReadOnlyList<PandaFacialSemanticWeight> EyeExpressions(
            float blinkLeft,
            float blinkRight,
            float smileLeft,
            float smileRight,
            float surpriseLeft,
            float surpriseRight,
            float angryLeft,
            float angryRight,
            float sadLeft,
            float sadRight,
            float jitoLeft,
            float jitoRight)
        {
            return Result(
                NormalizedWeight(PandaFacialSemanticChannels.EyeCloseL, blinkLeft),
                NormalizedWeight(PandaFacialSemanticChannels.EyeCloseR, blinkRight),
                NormalizedWeight(PandaFacialSemanticChannels.EyeSmileL, smileLeft),
                NormalizedWeight(PandaFacialSemanticChannels.EyeSmileR, smileRight),
                NormalizedWeight(PandaFacialSemanticChannels.EyeSurpriseL, surpriseLeft),
                NormalizedWeight(PandaFacialSemanticChannels.EyeSurpriseR, surpriseRight),
                NormalizedWeight(PandaFacialSemanticChannels.EyeAngryL, angryLeft),
                NormalizedWeight(PandaFacialSemanticChannels.EyeAngryR, angryRight),
                NormalizedWeight(PandaFacialSemanticChannels.EyeSadL, sadLeft),
                NormalizedWeight(PandaFacialSemanticChannels.EyeSadR, sadRight),
                NormalizedWeight(PandaFacialSemanticChannels.EyeJitoL, jitoLeft),
                NormalizedWeight(PandaFacialSemanticChannels.EyeJitoR, jitoRight));
        }

        public static IReadOnlyList<PandaFacialSemanticWeight> ResetEyeExpressions()
        {
            return EyeExpressions(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
        }

        public static IReadOnlyList<PandaFacialSemanticWeight> Brows(
            float upLeft,
            float upRight,
            float downLeft,
            float downRight,
            float angryLeft,
            float angryRight,
            float sadLeft,
            float sadRight,
            float smileLeft,
            float smileRight,
            float seriousLeft,
            float seriousRight)
        {
            return Result(
                NormalizedWeight(PandaFacialSemanticChannels.BrowUpL, upLeft),
                NormalizedWeight(PandaFacialSemanticChannels.BrowUpR, upRight),
                NormalizedWeight(PandaFacialSemanticChannels.BrowDownL, downLeft),
                NormalizedWeight(PandaFacialSemanticChannels.BrowDownR, downRight),
                NormalizedWeight(PandaFacialSemanticChannels.BrowAngryL, angryLeft),
                NormalizedWeight(PandaFacialSemanticChannels.BrowAngryR, angryRight),
                NormalizedWeight(PandaFacialSemanticChannels.BrowSadL, sadLeft),
                NormalizedWeight(PandaFacialSemanticChannels.BrowSadR, sadRight),
                NormalizedWeight(PandaFacialSemanticChannels.BrowSmileL, smileLeft),
                NormalizedWeight(PandaFacialSemanticChannels.BrowSmileR, smileRight),
                NormalizedWeight(PandaFacialSemanticChannels.BrowSeriousL, seriousLeft),
                NormalizedWeight(PandaFacialSemanticChannels.BrowSeriousR, seriousRight));
        }

        public static IReadOnlyList<PandaFacialSemanticWeight> ResetBrows()
        {
            return Brows(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
        }

        public static IReadOnlyList<PandaFacialSemanticWeight> Single(string semanticId, float value)
        {
            return Result(NormalizedWeight(semanticId, value));
        }

        public static IReadOnlyList<PandaFacialSemanticWeight> Mouth(
            float positionX,
            float positionY,
            float cornerLeftY,
            float cornerRightY,
            float width,
            float a,
            float i,
            float u,
            float e,
            float o)
        {
            var outputs = new List<PandaFacialSemanticWeight>(17);
            Add(outputs, MouthPosition(positionX, positionY));
            Add(outputs, MouthCornerLeft(cornerLeftY));
            Add(outputs, MouthCornerRight(cornerRightY));
            Add(outputs, MouthWidth(width));
            Add(outputs, Vowels(a, i, u, e, o));
            return outputs.AsReadOnly();
        }

        public static IReadOnlyList<PandaFacialSemanticWeight> ResetMouth()
        {
            return Mouth(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
        }

        public static IReadOnlyList<PandaFacialSemanticWeight> Mouth(
            float positionX,
            float positionY,
            float cornerLeftX,
            float cornerLeftY,
            float cornerRightX,
            float cornerRightY,
            float a,
            float i,
            float u,
            float e,
            float o)
        {
            var outputs = new List<PandaFacialSemanticWeight>(17);
            Add(outputs, MouthPosition(positionX, positionY));
            Add(outputs, MouthCorners(cornerLeftX, cornerLeftY, cornerRightX, cornerRightY));
            Add(outputs, Vowels(a, i, u, e, o));
            return outputs.AsReadOnly();
        }

        private static PandaFacialSemanticWeight NormalizedWeight(string semanticId, float value)
        {
            return Weight(semanticId, ClampUnsigned(value) * 100f);
        }

        private static PandaFacialSemanticWeight Weight(string semanticId, float weight)
        {
            return new PandaFacialSemanticWeight(semanticId, weight);
        }

        private static float PositiveWeight(float value)
        {
            return Mathf.Max(0f, value) * 100f;
        }

        private static float NegativeWeight(float value)
        {
            return Mathf.Max(0f, -value) * 100f;
        }

        private static float ClampSigned(float value)
        {
            return float.IsNaN(value) ? 0f : Mathf.Clamp(value, -1f, 1f);
        }

        private static float ClampUnsigned(float value)
        {
            return float.IsNaN(value) ? 0f : Mathf.Clamp01(value);
        }

        private static IReadOnlyList<PandaFacialSemanticWeight> Result(
            params PandaFacialSemanticWeight[] weights)
        {
            return Array.AsReadOnly(weights);
        }

        private static void Add(
            List<PandaFacialSemanticWeight> destination,
            IReadOnlyList<PandaFacialSemanticWeight> source)
        {
            for (int i = 0; i < source.Count; i++)
                destination.Add(source[i]);
        }
    }
}
