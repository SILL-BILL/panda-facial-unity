using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SillBill.PandaFacial.Editor
{
    internal sealed class PandaFacialControllerOperationReport
    {
        private readonly List<string> warnings = new List<string>();

        internal int OutputCount { get; set; }
        internal int MappedCount { get; set; }
        internal int UnmappedCount { get; set; }
        internal int InvalidCount { get; set; }
        internal int AppliedCount { get; set; }
        internal IReadOnlyList<string> Warnings => warnings;

        internal void AddWarning(string message)
        {
            warnings.Add(message);
        }
    }

    internal static class PandaFacialControllerAnimationUtility
    {
        internal static PandaFacialControllerOperationReport ApplyPreview(
            PandaFacialAuthoringTarget authoringTarget,
            IReadOnlyList<PandaFacialSemanticWeight> outputs)
        {
            var report = new PandaFacialControllerOperationReport
            {
                OutputCount = outputs != null ? outputs.Count : 0
            };
            if (authoringTarget == null || outputs == null)
                return report;

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Preview Panda Facial Controller");
            var recordedRenderers = new HashSet<SkinnedMeshRenderer>();

            for (int i = 0; i < outputs.Count; i++)
            {
                PandaFacialSemanticWeight output = outputs[i];
                PandaFacialResolvedMapping resolved = PandaFacialMappingResolver.Resolve(
                    authoringTarget,
                    output.SemanticId);
                if (!TryAcceptMapping(output.SemanticId, resolved, report))
                    continue;

                if (recordedRenderers.Add(resolved.Renderer))
                    Undo.RecordObject(resolved.Renderer, "Preview Panda Facial Controller");

                resolved.Renderer.SetBlendShapeWeight(resolved.BlendShapeIndex, output.Weight);
                PrefabUtility.RecordPrefabInstancePropertyModifications(resolved.Renderer);
                EditorUtility.SetDirty(resolved.Renderer);
                report.AppliedCount++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            return report;
        }

        internal static PandaFacialControllerOperationReport WriteKeys(
            PandaFacialAuthoringTarget authoringTarget,
            IReadOnlyList<PandaFacialSemanticWeight> outputs,
            AnimationClip clip,
            Transform animationRoot,
            float time)
        {
            var report = new PandaFacialControllerOperationReport
            {
                OutputCount = outputs != null ? outputs.Count : 0
            };
            if (authoringTarget == null || outputs == null)
                return report;

            var keys = new List<PandaFacialBlendShapeKey>();
            for (int i = 0; i < outputs.Count; i++)
            {
                PandaFacialSemanticWeight output = outputs[i];
                PandaFacialResolvedMapping resolved = PandaFacialMappingResolver.Resolve(
                    authoringTarget,
                    output.SemanticId);
                if (!TryAcceptMapping(output.SemanticId, resolved, report))
                    continue;

                if (animationRoot == null ||
                    (resolved.Renderer.transform != animationRoot &&
                     !resolved.Renderer.transform.IsChildOf(animationRoot)))
                {
                    report.InvalidCount++;
                    report.MappedCount--;
                    report.AddWarning(
                        GetDisplayName(output.SemanticId) +
                        ": renderer is outside the Timeline binding root.");
                    continue;
                }

                EditorCurveBinding binding = PandaFacialAnimationUtility.CreateBlendShapeBinding(
                    animationRoot,
                    resolved.Renderer,
                    resolved.BlendShapeName);
                keys.Add(new PandaFacialBlendShapeKey(binding, output.Weight));
            }

            PandaFacialAnimationUtility.WriteBlendShapeKeys(clip, keys, time);
            report.AppliedCount = keys.Count;
            return report;
        }

        private static bool TryAcceptMapping(
            string semanticId,
            PandaFacialResolvedMapping resolved,
            PandaFacialControllerOperationReport report)
        {
            if (resolved.IsMapped)
            {
                report.MappedCount++;
                return true;
            }

            if (resolved.Status == PandaFacialMappingStatus.Unmapped)
                report.UnmappedCount++;
            else
                report.InvalidCount++;

            report.AddWarning(
                GetDisplayName(semanticId) + ": " +
                PandaFacialMappingResolver.GetStatusMessage(resolved));
            return false;
        }

        private static string GetDisplayName(string semanticId)
        {
            return PandaFacialSemanticChannels.TryGet(
                semanticId,
                out PandaFacialSemanticChannel channel)
                ? channel.DisplayName
                : semanticId;
        }
    }
}
