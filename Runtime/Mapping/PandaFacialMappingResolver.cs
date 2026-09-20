using System;
using UnityEngine;

namespace SillBill.PandaFacial
{
    public enum PandaFacialMappingStatus
    {
        Mapped,
        Unmapped,
        InvalidRenderer,
        InvalidMesh,
        InvalidBlendShape
    }

    public readonly struct PandaFacialResolvedMapping
    {
        internal PandaFacialResolvedMapping(
            string semanticId,
            PandaFacialMappingStatus status,
            SkinnedMeshRenderer renderer,
            string blendShapeName,
            int blendShapeIndex)
        {
            SemanticId = semanticId;
            Status = status;
            Renderer = renderer;
            BlendShapeName = blendShapeName;
            BlendShapeIndex = blendShapeIndex;
        }

        public string SemanticId { get; }
        public PandaFacialMappingStatus Status { get; }
        public SkinnedMeshRenderer Renderer { get; }
        public string BlendShapeName { get; }
        public int BlendShapeIndex { get; }
        public bool IsMapped => Status == PandaFacialMappingStatus.Mapped;
    }

    public static class PandaFacialMappingResolver
    {
        public static PandaFacialResolvedMapping Resolve(
            PandaFacialAuthoringTarget authoringTarget,
            string semanticId)
        {
            if (authoringTarget == null || string.IsNullOrEmpty(semanticId))
                return Unmapped(semanticId);

            var mappings = authoringTarget.SemanticMappings;
            for (int i = 0; i < mappings.Count; i++)
            {
                PandaFacialSemanticMapping mapping = mappings[i];
                if (mapping != null && string.Equals(mapping.SemanticId, semanticId, StringComparison.Ordinal))
                    return Resolve(mapping, authoringTarget.DefaultFaceRenderer);
            }

            return Unmapped(semanticId);
        }

        public static PandaFacialResolvedMapping Resolve(PandaFacialSemanticMapping mapping)
        {
            return Resolve(mapping, null);
        }

        public static PandaFacialResolvedMapping Resolve(
            PandaFacialSemanticMapping mapping,
            SkinnedMeshRenderer defaultRenderer)
        {
            if (mapping == null || string.IsNullOrEmpty(mapping.SemanticId))
                return Unmapped(mapping != null ? mapping.SemanticId : null);

            if (string.IsNullOrEmpty(mapping.BlendShapeName))
                return Unmapped(mapping.SemanticId);

            SkinnedMeshRenderer effectiveRenderer = mapping.TargetRenderer != null
                ? mapping.TargetRenderer
                : defaultRenderer;
            if (effectiveRenderer == null)
            {
                return Result(
                    mapping,
                    PandaFacialMappingStatus.InvalidRenderer,
                    -1,
                    null);
            }

            if (effectiveRenderer.sharedMesh == null)
                return Result(mapping, PandaFacialMappingStatus.InvalidMesh, -1, effectiveRenderer);

            int blendShapeIndex = effectiveRenderer.sharedMesh.GetBlendShapeIndex(mapping.BlendShapeName);
            return blendShapeIndex >= 0
                ? Result(mapping, PandaFacialMappingStatus.Mapped, blendShapeIndex, effectiveRenderer)
                : Result(mapping, PandaFacialMappingStatus.InvalidBlendShape, -1, effectiveRenderer);
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
                default:
                    return "Invalid mapping.";
            }
        }

        private static PandaFacialResolvedMapping Result(
            PandaFacialSemanticMapping mapping,
            PandaFacialMappingStatus status,
            int blendShapeIndex,
            SkinnedMeshRenderer effectiveRenderer)
        {
            return new PandaFacialResolvedMapping(
                mapping.SemanticId,
                status,
                effectiveRenderer,
                mapping.BlendShapeName,
                blendShapeIndex);
        }

        private static PandaFacialResolvedMapping Unmapped(string semanticId)
        {
            return new PandaFacialResolvedMapping(
                semanticId,
                PandaFacialMappingStatus.Unmapped,
                null,
                null,
                -1);
        }
    }
}
