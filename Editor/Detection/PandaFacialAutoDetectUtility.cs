using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SillBill.PandaFacial.Editor
{
    internal static class PandaFacialAutoDetectUtility
    {
        internal static PandaFacialDetectionReport DetectAndApply(
            PandaFacialAuthoringTarget authoringTarget,
            IPandaFacialDetectionDictionary dictionary = null)
        {
            if (authoringTarget == null)
                return Error("No Panda Facial Authoring Target is available.");

            SkinnedMeshRenderer renderer = authoringTarget.DefaultFaceRenderer;
            if (renderer == null)
                return Error("Assign Default Face Renderer before running Auto Detect.");
            if (renderer.sharedMesh == null || renderer.sharedMesh.blendShapeCount == 0)
                return Error("Default Face Renderer has no BlendShapes.");

            var names = new List<string>(renderer.sharedMesh.blendShapeCount);
            for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                names.Add(renderer.sharedMesh.GetBlendShapeName(i));

            var existing = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<PandaFacialSemanticMapping> mappings = authoringTarget.SemanticMappings;
            for (int i = 0; i < mappings.Count; i++)
            {
                PandaFacialSemanticMapping mapping = mappings[i];
                if (mapping != null && !string.IsNullOrEmpty(mapping.SemanticId) && HasSavedTarget(mapping))
                    existing.Add(mapping.SemanticId);
            }

            PandaFacialDetectionReport report = PandaFacialAutoDetector.Detect(
                PandaFacialSemanticChannels.BuiltIn,
                names,
                dictionary ?? PandaFacialBuiltInDetectionDictionary.Instance,
                existing);
            if (report.MappedCount == 0)
                return report;

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Auto Detect Panda Facial Mappings");
            Undo.RegisterCompleteObjectUndo(authoringTarget, "Auto Detect Panda Facial Mappings");
            var serializedTarget = new SerializedObject(authoringTarget);
            SerializedProperty serializedMappings = serializedTarget.FindProperty("semanticMappings");
            for (int i = 0; i < report.Entries.Count; i++)
            {
                PandaFacialDetectionEntry entry = report.Entries[i];
                if (entry.Status != PandaFacialDetectionStatus.Detected)
                    continue;

                SerializedProperty mapping = FindMapping(serializedMappings, entry.SemanticId) ??
                                             AddMapping(serializedMappings, entry.SemanticId);
                Migrate(mapping);
                mapping.FindPropertyRelative("targetRenderer").objectReferenceValue = null;
                mapping.FindPropertyRelative("blendShapeName").stringValue = entry.DetectedBlendShape;
                mapping.FindPropertyRelative("primaryWeightMultiplier").floatValue = 1f;
                mapping.FindPropertyRelative("primaryEnabled").boolValue = true;
            }
            serializedTarget.ApplyModifiedProperties();
            EditorUtility.SetDirty(authoringTarget);
            Undo.CollapseUndoOperations(undoGroup);
            return report;
        }

        internal static void Migrate(SerializedProperty mapping)
        {
            if (mapping == null)
                return;
            SerializedProperty schema = mapping.FindPropertyRelative("schemaVersion");
            if (schema == null || schema.intValue >= 2)
                return;
            mapping.FindPropertyRelative("primaryWeightMultiplier").floatValue = 1f;
            mapping.FindPropertyRelative("primaryEnabled").boolValue = true;
            schema.intValue = 2;
        }

        private static bool HasSavedTarget(PandaFacialSemanticMapping mapping)
        {
            if (!string.IsNullOrEmpty(mapping.BlendShapeName))
                return true;
            IReadOnlyList<PandaFacialMappingTarget> additional = mapping.AdditionalTargets;
            for (int i = 0; i < additional.Count; i++)
            {
                if (additional[i] != null && !string.IsNullOrEmpty(additional[i].BlendShapeName))
                    return true;
            }
            return false;
        }

        private static SerializedProperty FindMapping(SerializedProperty mappings, string semanticId)
        {
            for (int i = 0; i < mappings.arraySize; i++)
            {
                SerializedProperty mapping = mappings.GetArrayElementAtIndex(i);
                if (mapping.FindPropertyRelative("semanticId").stringValue == semanticId)
                    return mapping;
            }
            return null;
        }

        private static SerializedProperty AddMapping(SerializedProperty mappings, string semanticId)
        {
            int index = mappings.arraySize;
            mappings.InsertArrayElementAtIndex(index);
            SerializedProperty mapping = mappings.GetArrayElementAtIndex(index);
            mapping.FindPropertyRelative("semanticId").stringValue = semanticId;
            mapping.FindPropertyRelative("targetRenderer").objectReferenceValue = null;
            mapping.FindPropertyRelative("blendShapeName").stringValue = string.Empty;
            SerializedProperty additional = mapping.FindPropertyRelative("additionalTargets");
            if (additional != null)
                additional.arraySize = 0;
            return mapping;
        }

        private static PandaFacialDetectionReport Error(string message)
        {
            return new PandaFacialDetectionReport { Error = message };
        }
    }
}
