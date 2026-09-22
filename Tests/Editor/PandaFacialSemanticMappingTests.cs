using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SillBill.PandaFacial.Editor.Tests
{
    internal sealed class PandaFacialSemanticMappingTests
    {
        private GameObject root;
        private GameObject face;
        private PandaFacialAuthoringTarget authoringTarget;
        private SkinnedMeshRenderer renderer;
        private Mesh mesh;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("CharacterRoot");
            face = new GameObject("Face");
            face.transform.SetParent(root.transform);
            authoringTarget = root.AddComponent<PandaFacialAuthoringTarget>();
            renderer = face.AddComponent<SkinnedMeshRenderer>();
            mesh = CreateMeshWithBlendShape("Character_A");
            renderer.sharedMesh = mesh;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(mesh);
        }

        [Test]
        public void BuiltInSemanticIds_AreStableUniqueAndExcludeEyeLook()
        {
            string[] expected =
            {
                "mouth_a", "mouth_i", "mouth_u", "mouth_e", "mouth_o",
                "mouth_left", "mouth_right", "mouth_up", "mouth_down",
                "mouth_corner_up_l", "mouth_corner_down_l", "mouth_corner_up_r", "mouth_corner_down_r",
                "mouth_spread_l", "mouth_narrow_l", "mouth_spread_r", "mouth_narrow_r",
                "eye_close_l", "eye_close_r", "eye_smile_l", "eye_smile_r",
                "eye_surprise_l", "eye_surprise_r", "eye_angry_l", "eye_angry_r",
                "eye_sad_l", "eye_sad_r", "eye_jito_l", "eye_jito_r",
                "brow_up_l", "brow_up_r", "brow_down_l", "brow_down_r",
                "brow_angry_l", "brow_angry_r", "brow_sad_l", "brow_sad_r",
                "brow_smile_l", "brow_smile_r", "brow_serious_l", "brow_serious_r"
            };

            string[] actual = PandaFacialSemanticChannels.BuiltIn.Select(channel => channel.Id).ToArray();
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.Distinct().Count(), Is.EqualTo(actual.Length));
            Assert.That(actual.Any(id => id.Contains("look") || id.Contains("aim")), Is.False);
        }

        [Test]
        public void Resolver_ReturnsMappedActualRendererNameAndIndex()
        {
            AddMapping(authoringTarget, PandaFacialSemanticChannels.MouthA, renderer, "Character_A");

            PandaFacialResolvedMapping result = PandaFacialMappingResolver.Resolve(
                authoringTarget,
                PandaFacialSemanticChannels.MouthA);

            Assert.That(result.Status, Is.EqualTo(PandaFacialMappingStatus.Mapped));
            Assert.That(result.Renderer, Is.SameAs(renderer));
            Assert.That(result.BlendShapeName, Is.EqualTo("Character_A"));
            Assert.That(result.BlendShapeIndex, Is.EqualTo(0));
        }

        [Test]
        public void Resolver_InheritsDefaultFaceRendererWhenChannelHasNoOverride()
        {
            SetDefaultRenderer(authoringTarget, renderer);
            AddMapping(authoringTarget, PandaFacialSemanticChannels.MouthA, null, "Character_A");

            PandaFacialResolvedMapping result = PandaFacialMappingResolver.Resolve(
                authoringTarget,
                PandaFacialSemanticChannels.MouthA);

            Assert.That(result.IsMapped, Is.True);
            Assert.That(result.Renderer, Is.SameAs(renderer));
        }

        [Test]
        public void Resolver_ChannelRendererOverrideTakesPriorityOverDefault()
        {
            var overrideObject = new GameObject("OverrideFace");
            overrideObject.transform.SetParent(root.transform);
            SkinnedMeshRenderer overrideRenderer = overrideObject.AddComponent<SkinnedMeshRenderer>();
            Mesh overrideMesh = CreateMeshWithBlendShape("Character_A");
            overrideRenderer.sharedMesh = overrideMesh;
            SetDefaultRenderer(authoringTarget, renderer);
            AddMapping(
                authoringTarget,
                PandaFacialSemanticChannels.MouthA,
                overrideRenderer,
                "Character_A");

            try
            {
                PandaFacialResolvedMapping result = PandaFacialMappingResolver.Resolve(
                    authoringTarget,
                    PandaFacialSemanticChannels.MouthA);

                Assert.That(result.IsMapped, Is.True);
                Assert.That(result.Renderer, Is.SameAs(overrideRenderer));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(overrideMesh);
            }
        }

        [Test]
        public void Resolver_DefaultRendererChangePreservesNameAndReportsInvalidMapping()
        {
            var replacementObject = new GameObject("ReplacementFace");
            replacementObject.transform.SetParent(root.transform);
            SkinnedMeshRenderer replacement = replacementObject.AddComponent<SkinnedMeshRenderer>();
            Mesh replacementMesh = CreateMeshWithBlendShape("DifferentShape");
            replacement.sharedMesh = replacementMesh;
            AddMapping(authoringTarget, PandaFacialSemanticChannels.MouthA, null, "Character_A");
            SetDefaultRenderer(authoringTarget, renderer);
            Assert.That(
                PandaFacialMappingResolver.Resolve(authoringTarget, PandaFacialSemanticChannels.MouthA).IsMapped,
                Is.True);

            try
            {
                SetDefaultRenderer(authoringTarget, replacement);
                PandaFacialResolvedMapping result = PandaFacialMappingResolver.Resolve(
                    authoringTarget,
                    PandaFacialSemanticChannels.MouthA);

                Assert.That(result.Status, Is.EqualTo(PandaFacialMappingStatus.InvalidBlendShape));
                Assert.That(authoringTarget.SemanticMappings.Single().BlendShapeName, Is.EqualTo("Character_A"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(replacementMesh);
            }
        }

        [Test]
        public void Resolver_ReportsMissingAndInvalidMappingsIndependently()
        {
            Assert.That(
                PandaFacialMappingResolver.Resolve(authoringTarget, PandaFacialSemanticChannels.MouthI).Status,
                Is.EqualTo(PandaFacialMappingStatus.Unmapped));

            AddMapping(authoringTarget, PandaFacialSemanticChannels.MouthA, null, "Character_A");
            AddMapping(authoringTarget, PandaFacialSemanticChannels.MouthI, renderer, "DeletedShape");
            GameObject noMeshObject = new GameObject("NoMesh");
            noMeshObject.transform.SetParent(root.transform);
            SkinnedMeshRenderer noMeshRenderer = noMeshObject.AddComponent<SkinnedMeshRenderer>();
            AddMapping(authoringTarget, PandaFacialSemanticChannels.MouthU, noMeshRenderer, "Character_U");

            Assert.That(
                PandaFacialMappingResolver.Resolve(authoringTarget, PandaFacialSemanticChannels.MouthA).Status,
                Is.EqualTo(PandaFacialMappingStatus.InvalidRenderer));
            Assert.That(
                PandaFacialMappingResolver.Resolve(authoringTarget, PandaFacialSemanticChannels.MouthI).Status,
                Is.EqualTo(PandaFacialMappingStatus.InvalidBlendShape));
            Assert.That(
                PandaFacialMappingResolver.Resolve(authoringTarget, PandaFacialSemanticChannels.MouthU).Status,
                Is.EqualTo(PandaFacialMappingStatus.InvalidMesh));
            Assert.That(
                PandaFacialMappingResolver.Resolve(authoringTarget, PandaFacialSemanticChannels.MouthE).Status,
                Is.EqualTo(PandaFacialMappingStatus.Unmapped));
        }

        [Test]
        public void Mapping_SavesAndRestoresWithPrefabAuthoringData()
        {
            string path = "Assets/__PandaFacialMappingTest_" + Guid.NewGuid().ToString("N") + ".prefab";
            AddMapping(authoringTarget, PandaFacialSemanticChannels.MouthA, renderer, "Character_A");

            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                PandaFacialAuthoringTarget restored = prefab.GetComponent<PandaFacialAuthoringTarget>();
                PandaFacialSemanticMapping mapping = restored.SemanticMappings.Single();

                Assert.That(mapping.SemanticId, Is.EqualTo(PandaFacialSemanticChannels.MouthA));
                Assert.That(mapping.TargetRenderer, Is.Not.Null);
                Assert.That(mapping.TargetRenderer.name, Is.EqualTo("Face"));
                Assert.That(mapping.BlendShapeName, Is.EqualTo("Character_A"));
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void MappingChanges_CanBeUndone()
        {
            int undoGroup = Undo.GetCurrentGroup();
            Undo.RegisterCompleteObjectUndo(authoringTarget, "Map Panda Facial Semantic");
            AddMapping(authoringTarget, PandaFacialSemanticChannels.MouthA, renderer, "Character_A");
            Undo.CollapseUndoOperations(undoGroup);
            Assert.That(authoringTarget.SemanticMappings.Count, Is.EqualTo(1));

            Undo.PerformUndo();

            Assert.That(authoringTarget.SemanticMappings.Count, Is.EqualTo(0));
        }

        [Test]
        public void SemanticMapping_WritesOnlyActualBlendShapeAndPreservesExistingCurve()
        {
            AddMapping(authoringTarget, PandaFacialSemanticChannels.MouthA, renderer, "Character_A");
            PandaFacialResolvedMapping resolved = PandaFacialMappingResolver.Resolve(
                authoringTarget,
                PandaFacialSemanticChannels.MouthA);
            AnimationClip clip = new AnimationClip { frameRate = 24f };

            try
            {
                EditorCurveBinding unrelated = EditorCurveBinding.FloatCurve(
                    string.Empty,
                    typeof(Transform),
                    "m_LocalPosition.x");
                AnimationUtility.SetEditorCurve(clip, unrelated, AnimationCurve.Constant(0f, 1f, 3f));
                EditorCurveBinding actual = PandaFacialAnimationUtility.CreateBlendShapeBinding(
                    root.transform,
                    resolved.Renderer,
                    resolved.BlendShapeName);

                PandaFacialAnimationUtility.WriteBlendShapeKey(clip, actual, 1f, 50f);

                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
                Assert.That(bindings.Length, Is.EqualTo(2));
                Assert.That(bindings.Any(binding =>
                    binding.type == typeof(SkinnedMeshRenderer) &&
                    binding.propertyName == "blendShape.Character_A"), Is.True);
                Assert.That(bindings.Any(binding => binding.type == typeof(PandaFacialAuthoringTarget)), Is.False);
                Assert.That(AnimationUtility.GetEditorCurve(clip, unrelated).Evaluate(0.5f), Is.EqualTo(3f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        internal static void AddMapping(
            PandaFacialAuthoringTarget target,
            string semanticId,
            SkinnedMeshRenderer targetRenderer,
            string blendShapeName)
        {
            var serializedTarget = new SerializedObject(target);
            SerializedProperty mappings = serializedTarget.FindProperty("semanticMappings");
            int index = mappings.arraySize;
            mappings.InsertArrayElementAtIndex(index);
            SerializedProperty mapping = mappings.GetArrayElementAtIndex(index);
            mapping.FindPropertyRelative("semanticId").stringValue = semanticId;
            mapping.FindPropertyRelative("targetRenderer").objectReferenceValue = targetRenderer;
            mapping.FindPropertyRelative("blendShapeName").stringValue = blendShapeName;
            SerializedProperty schemaVersion = mapping.FindPropertyRelative("schemaVersion");
            if (schemaVersion != null)
            {
                schemaVersion.intValue = 2;
                mapping.FindPropertyRelative("primaryWeightMultiplier").floatValue = 1f;
                mapping.FindPropertyRelative("primaryEnabled").boolValue = true;
                mapping.FindPropertyRelative("additionalTargets").arraySize = 0;
            }
            serializedTarget.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetDefaultRenderer(
            PandaFacialAuthoringTarget target,
            SkinnedMeshRenderer defaultRenderer)
        {
            var serializedTarget = new SerializedObject(target);
            serializedTarget.FindProperty("defaultFaceRenderer").objectReferenceValue = defaultRenderer;
            serializedTarget.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Mesh CreateMeshWithBlendShape(string blendShapeName)
        {
            var result = new Mesh { name = "PandaFacialMappingTestMesh" };
            result.vertices = new[] { Vector3.zero };
            result.AddBlendShapeFrame(
                blendShapeName,
                100f,
                new[] { Vector3.right },
                new[] { Vector3.zero },
                new[] { Vector3.zero });
            return result;
        }
    }
}
