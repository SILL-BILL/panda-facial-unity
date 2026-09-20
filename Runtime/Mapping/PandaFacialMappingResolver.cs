using System;
using System.Collections.Generic;
using UnityEngine;

namespace SillBill.PandaFacial
{
    public enum PandaFacialMappingStatus
    {
        Mapped,
        Unmapped,
        InvalidRenderer,
        InvalidMesh,
        InvalidBlendShape,
        Disabled
    }

    public readonly struct PandaFacialResolvedMapping
    {
        internal PandaFacialResolvedMapping(
            string semanticId,
            PandaFacialMappingStatus status,
            SkinnedMeshRenderer renderer,
            string blendShapeName,
            int blendShapeIndex,
            float weightMultiplier,
            int targetIndex)
        {
            SemanticId = semanticId;
            Status = status;
            Renderer = renderer;
            BlendShapeName = blendShapeName;
            BlendShapeIndex = blendShapeIndex;
            WeightMultiplier = Mathf.Clamp01(weightMultiplier);
            TargetIndex = targetIndex;
        }

        public string SemanticId { get; }
        public PandaFacialMappingStatus Status { get; }
        public SkinnedMeshRenderer Renderer { get; }
        public string BlendShapeName { get; }
        public int BlendShapeIndex { get; }
        public float WeightMultiplier { get; }
        public int TargetIndex { get; }
        public bool IsMapped => Status == PandaFacialMappingStatus.Mapped;

        public float ApplyMultiplier(float semanticWeight)
        {
            return Mathf.Clamp(semanticWeight * WeightMultiplier, 0f, 100f);
        }
    }

    public static class PandaFacialMappingResolver
    {
        public static PandaFacialResolvedMapping Resolve(
            PandaFacialAuthoringTarget authoringTarget,
            string semanticId)
        {
            IReadOnlyList<PandaFacialResolvedMapping> targets = ResolveAll(authoringTarget, semanticId);
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].IsMapped)
                    return targets[i];
            }
            return targets[0];
        }

        public static IReadOnlyList<PandaFacialResolvedMapping> ResolveAll(
            PandaFacialAuthoringTarget authoringTarget,
            string semanticId)
        {
            if (authoringTarget == null || string.IsNullOrEmpty(semanticId))
                return Single(Unmapped(semanticId));

            IReadOnlyList<PandaFacialSemanticMapping> mappings = authoringTarget.SemanticMappings;
            for (int i = 0; i < mappings.Count; i++)
            {
                PandaFacialSemanticMapping mapping = mappings[i];
                if (mapping != null && string.Equals(mapping.SemanticId, semanticId, StringComparison.Ordinal))
                    return ResolveAll(mapping, authoringTarget.DefaultFaceRenderer);
            }

            return Single(Unmapped(semanticId));
        }

        public static PandaFacialResolvedMapping Resolve(PandaFacialSemanticMapping mapping)
        {
            return Resolve(mapping, null);
        }

        public static PandaFacialResolvedMapping Resolve(
            PandaFacialSemanticMapping mapping,
            SkinnedMeshRenderer defaultRenderer)
        {
            IReadOnlyList<PandaFacialResolvedMapping> targets = ResolveAll(mapping, defaultRenderer);
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].IsMapped)
                    return targets[i];
            }
            return targets[0];
        }

        public static IReadOnlyList<PandaFacialResolvedMapping> ResolveAll(
            PandaFacialSemanticMapping mapping,
            SkinnedMeshRenderer defaultRenderer)
        {
            if (mapping == null || string.IsNullOrEmpty(mapping.SemanticId))
                return Single(Unmapped(mapping != null ? mapping.SemanticId : null));

            int additionalCount = mapping.AdditionalTargets != null
                ? mapping.AdditionalTargets.Count
                : 0;
            var results = new List<PandaFacialResolvedMapping>(1 + additionalCount)
            {
                ResolveTarget(
                    mapping.SemanticId,
                    mapping.TargetRenderer,
                    defaultRenderer,
                    mapping.BlendShapeName,
                    mapping.PrimaryWeightMultiplier,
                    mapping.PrimaryEnabled,
                    0)
            };

            for (int i = 0; i < additionalCount; i++)
            {
                PandaFacialMappingTarget target = mapping.AdditionalTargets[i];
                if (target == null)
                {
                    results.Add(Unmapped(mapping.SemanticId, i + 1));
                    continue;
                }

                results.Add(ResolveTarget(
                    mapping.SemanticId,
                    target.TargetRenderer,
                    defaultRenderer,
                    target.BlendShapeName,
                    target.WeightMultiplier,
                    target.Enabled,
                    i + 1));
            }

            return results.AsReadOnly();
        }

        public static string GetStatusMessage(PandaFacialResolvedMapping mapping)
        {
            switch (mapping.Status)
            {
                case PandaFacialMappingStatus.Mapped:
                    return null;
                case PandaFacialMappingStatus.Unmapped:
                    return "Unmapped";
                case PandaFacialMappingStatus.InvalidRenderer:
                    return "Mapped BlendShape has no target renderer.";
                case PandaFacialMappingStatus.InvalidMesh:
                    return "The mapped renderer has no Mesh.";
                case PandaFacialMappingStatus.InvalidBlendShape:
                    return "The mapped BlendShape does not exist on the renderer's current Mesh.";
                case PandaFacialMappingStatus.Disabled:
                    return "Target is disabled.";
                default:
                    return "Invalid mapping.";
            }
        }

        private static PandaFacialResolvedMapping ResolveTarget(
            string semanticId,
            SkinnedMeshRenderer rendererOverride,
            SkinnedMeshRenderer defaultRenderer,
            string blendShapeName,
            float weightMultiplier,
            bool enabled,
            int targetIndex)
        {
            if (!enabled)
            {
                return new PandaFacialResolvedMapping(
                    semanticId,
                    PandaFacialMappingStatus.Disabled,
                    rendererOverride != null ? rendererOverride : defaultRenderer,
                    blendShapeName,
                    -1,
                    weightMultiplier,
                    targetIndex);
            }

            if (string.IsNullOrEmpty(blendShapeName))
                return Unmapped(semanticId, targetIndex, weightMultiplier);

            SkinnedMeshRenderer effectiveRenderer = rendererOverride != null
                ? rendererOverride
                : defaultRenderer;
            if (effectiveRenderer == null)
            {
                return Result(semanticId, PandaFacialMappingStatus.InvalidRenderer, null,
                    blendShapeName, -1, weightMultiplier, targetIndex);
            }
            if (effectiveRenderer.sharedMesh == null)
            {
                return Result(semanticId, PandaFacialMappingStatus.InvalidMesh, effectiveRenderer,
                    blendShapeName, -1, weightMultiplier, targetIndex);
            }

            int blendShapeIndex = effectiveRenderer.sharedMesh.GetBlendShapeIndex(blendShapeName);
            return blendShapeIndex >= 0
                ? Result(semanticId, PandaFacialMappingStatus.Mapped, effectiveRenderer,
                    blendShapeName, blendShapeIndex, weightMultiplier, targetIndex)
                : Result(semanticId, PandaFacialMappingStatus.InvalidBlendShape, effectiveRenderer,
                    blendShapeName, -1, weightMultiplier, targetIndex);
        }

        private static PandaFacialResolvedMapping Result(
            string semanticId,
            PandaFacialMappingStatus status,
            SkinnedMeshRenderer renderer,
            string blendShapeName,
            int blendShapeIndex,
            float multiplier,
            int targetIndex)
        {
            return new PandaFacialResolvedMapping(
                semanticId, status, renderer, blendShapeName, blendShapeIndex, multiplier, targetIndex);
        }

        private static PandaFacialResolvedMapping Unmapped(
            string semanticId,
            int targetIndex = 0,
            float multiplier = 1f)
        {
            return Result(semanticId, PandaFacialMappingStatus.Unmapped, null, null, -1,
                multiplier, targetIndex);
        }

        private static IReadOnlyList<PandaFacialResolvedMapping> Single(
            PandaFacialResolvedMapping mapping)
        {
            return Array.AsReadOnly(new[] { mapping });
        }
    }
}
