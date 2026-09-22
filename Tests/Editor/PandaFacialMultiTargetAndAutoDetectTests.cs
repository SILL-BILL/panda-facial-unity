using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SillBill.PandaFacial.Editor.Tests
{
    internal sealed class PandaFacialMultiTargetMappingTests
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
            mesh = CreateMesh("Primary", "Half", "Zero", "Override", "Unrelated");
            renderer.sharedMesh = mesh;
            PandaFacialSemanticMappingTests.SetDefaultRenderer(authoringTarget, renderer);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void LegacySingleMapping_MigratesWithoutLosingSavedTarget()
        {
            PandaFacialSemanticMappingTests.AddMapping(
                authoringTarget, PandaFacialSemanticChannels.EyeCloseL, null, "Primary");
            SerializedProperty mapping = GetMappingProperty();
            mapping.FindPropertyRelative("schemaVersion").intValue = 0;
            mapping.FindPropertyRelative("primaryWeightMultiplier").floatValue = 0f;
            mapping.FindPropertyRelative("primaryEnabled").boolValue = false;
            mapping.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            PandaFacialSemanticMapping saved = authoringTarget.SemanticMappings.Single();
            saved.OnAfterDeserialize();

            Assert.That(saved.SchemaVersion, Is.EqualTo(2));
            Assert.That(saved.SemanticId, Is.EqualTo(PandaFacialSemanticChannels.EyeCloseL));
            Assert.That(saved.BlendShapeName, Is.EqualTo("Primary"));
            Assert.That(saved.PrimaryWeightMultiplier, Is.EqualTo(1f));
            Assert.That(saved.PrimaryEnabled, Is.True);
            Assert.That(PandaFacialMappingResolver.Resolve(authoringTarget,
                PandaFacialSemanticChannels.EyeCloseL).IsMapped, Is.True);
        }

        [TestCase(0, 1)]
        [TestCase(1, 2)]
        public void SingleAndTwoTargetMappings_InheritDefaultRenderer(
            int additionalTargetCount,
            int expectedTargetCount)
        {
            AddPrimary("Primary");
            if (additionalTargetCount > 0)
                AddAdditional(null, "Half", 0.5f, true);

            IReadOnlyList<PandaFacialResolvedMapping> resolved =
                PandaFacialMappingResolver.ResolveAll(
                    authoringTarget, PandaFacialSemanticChannels.EyeCloseL);

            Assert.That(resolved.Count, Is.EqualTo(expectedTargetCount));
            Assert.That(resolved.All(target => target.IsMapped), Is.True);
            Assert.That(resolved.All(target => target.Renderer == renderer), Is.True);
        }

        [Test]
        public void ThreeTargets_ApplyIndependentMultipliersIncludingZero()
        {
            AddPrimary("Primary");
            AddAdditional(null, "Half", 0.5f, true);
            AddAdditional(null, "Zero", 0f, true);

            IReadOnlyList<PandaFacialResolvedMapping> resolved =
                PandaFacialMappingResolver.ResolveAll(
                    authoringTarget, PandaFacialSemanticChannels.EyeCloseL);
            PandaFacialControllerOperationReport report =
                PandaFacialControllerAnimationUtility.ApplyPreview(
                    authoringTarget,
                    PandaFacialControllerLogic.Single(PandaFacialSemanticChannels.EyeCloseL, 0.8f));

            Assert.That(resolved.Count, Is.EqualTo(3));
            Assert.That(report.AppliedCount, Is.EqualTo(3));
            Assert.That(Weight("Primary"), Is.EqualTo(80f));
            Assert.That(Weight("Half"), Is.EqualTo(40f));
            Assert.That(Weight("Zero"), Is.EqualTo(0f));
        }

        [Test]
        public void MultiplierAndFinalWeight_AreClamped()
        {
            AddPrimary("Primary");
            SetPrimaryMultiplier(2f);
            AddAdditional(null, "Half", -1f, true);

            IReadOnlyList<PandaFacialResolvedMapping> resolved =
                PandaFacialMappingResolver.ResolveAll(
                    authoringTarget, PandaFacialSemanticChannels.EyeCloseL);
            PandaFacialControllerAnimationUtility.ApplyPreview(
                authoringTarget,
                new[] { new PandaFacialSemanticWeight(PandaFacialSemanticChannels.EyeCloseL, 150f) });

            Assert.That(resolved[0].WeightMultiplier, Is.EqualTo(1f));
            Assert.That(resolved[1].WeightMultiplier, Is.EqualTo(0f));
            Assert.That(Weight("Primary"), Is.EqualTo(100f));
            Assert.That(Weight("Half"), Is.EqualTo(0f));
        }

        [Test]
        public void DisabledAndInvalidTargets_DoNotBlockValidTarget()
        {
            AddPrimary("Primary");
            AddAdditional(null, "Missing", 1f, true);
            AddAdditional(null, "Half", 1f, false);

            PandaFacialControllerOperationReport report =
                PandaFacialControllerAnimationUtility.ApplyPreview(
                    authoringTarget,
                    PandaFacialControllerLogic.Single(PandaFacialSemanticChannels.EyeCloseL, 0.6f));

            Assert.That(report.AppliedCount, Is.EqualTo(1));
            Assert.That(report.InvalidCount, Is.EqualTo(1));
            Assert.That(report.DisabledCount, Is.EqualTo(1));
            Assert.That(Weight("Primary"), Is.EqualTo(60f).Within(0.001f));
            Assert.That(Weight("Half"), Is.EqualTo(0f));
        }

        [Test]
        public void AdditionalTarget_CanOverrideDefaultRenderer()
        {
            var overrideObject = new GameObject("OverrideFace");
            overrideObject.transform.SetParent(root.transform);
            SkinnedMeshRenderer overrideRenderer = overrideObject.AddComponent<SkinnedMeshRenderer>();
            Mesh overrideMesh = CreateMesh("OverrideTarget");
            overrideRenderer.sharedMesh = overrideMesh;
            AddPrimary("Primary");
            AddAdditional(overrideRenderer, "OverrideTarget", 0.5f, true);

            try
            {
                PandaFacialControllerAnimationUtility.ApplyPreview(
                    authoringTarget,
                    PandaFacialControllerLogic.Single(PandaFacialSemanticChannels.EyeCloseL, 0.8f));

                Assert.That(Weight("Primary"), Is.EqualTo(80f));
                Assert.That(overrideRenderer.GetBlendShapeWeight(0), Is.EqualTo(40f));
            }
            finally
            {
                Object.DestroyImmediate(overrideMesh);
            }
        }

        [Test]
        public void MultipleCurveWrite_PreservesUnrelatedCurveAndCreatesNoAuthoringCurve()
        {
            AddPrimary("Primary");
            AddAdditional(null, "Half", 0.5f, true);
            AddAdditional(null, "Zero", 0f, true);
            var clip = new AnimationClip();
            EditorCurveBinding unrelated = EditorCurveBinding.FloatCurve(
                string.Empty, typeof(Transform), "m_LocalPosition.x");
            AnimationUtility.SetEditorCurve(clip, unrelated, AnimationCurve.Constant(0f, 1f, 9f));

            try
            {
                PandaFacialControllerOperationReport report =
                    PandaFacialControllerAnimationUtility.WriteKeys(
                        authoringTarget,
                        PandaFacialControllerLogic.Single(PandaFacialSemanticChannels.EyeCloseL, 0.8f),
                        clip,
                        root.transform,
                        0.5f);
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

                Assert.That(report.AppliedCount, Is.EqualTo(3));
                Assert.That(bindings.Count(binding => binding.propertyName.StartsWith("blendShape.")),
                    Is.EqualTo(3));
                Assert.That(AnimationUtility.GetEditorCurve(clip, unrelated).Evaluate(0.5f), Is.EqualTo(9f));
                Assert.That(bindings.Any(binding => binding.type == typeof(PandaFacialAuthoringTarget)),
                    Is.False);
                Assert.That(AnimationUtility.GetEditorCurve(clip, bindings.Single(binding =>
                    binding.propertyName == "blendShape.Half")).Evaluate(0.5f), Is.EqualTo(40f));
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void MultiTargetPreview_UndoRestoresEveryTargetAsOneOperation()
        {
            AddPrimary("Primary");
            AddAdditional(null, "Half", 0.5f, true);
            renderer.SetBlendShapeWeight(mesh.GetBlendShapeIndex("Primary"), 12f);
            renderer.SetBlendShapeWeight(mesh.GetBlendShapeIndex("Half"), 34f);

            PandaFacialControllerAnimationUtility.ApplyPreview(
                authoringTarget,
                PandaFacialControllerLogic.Single(PandaFacialSemanticChannels.EyeCloseL, 0.8f));
            Undo.PerformUndo();

            Assert.That(Weight("Primary"), Is.EqualTo(12f));
            Assert.That(Weight("Half"), Is.EqualTo(34f));
        }

        private void AddPrimary(string blendShapeName)
        {
            PandaFacialSemanticMappingTests.AddMapping(
                authoringTarget, PandaFacialSemanticChannels.EyeCloseL, null, blendShapeName);
        }

        private void AddAdditional(
            SkinnedMeshRenderer targetRenderer,
            string blendShapeName,
            float multiplier,
            bool enabled)
        {
            SerializedProperty mapping = GetMappingProperty();
            SerializedProperty targets = mapping.FindPropertyRelative("additionalTargets");
            int index = targets.arraySize;
            targets.InsertArrayElementAtIndex(index);
            SerializedProperty target = targets.GetArrayElementAtIndex(index);
            target.FindPropertyRelative("targetRenderer").objectReferenceValue = targetRenderer;
            target.FindPropertyRelative("blendShapeName").stringValue = blendShapeName;
            target.FindPropertyRelative("weightMultiplier").floatValue = multiplier;
            target.FindPropertyRelative("enabled").boolValue = enabled;
            mapping.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private void SetPrimaryMultiplier(float value)
        {
            SerializedProperty mapping = GetMappingProperty();
            mapping.FindPropertyRelative("primaryWeightMultiplier").floatValue = value;
            mapping.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private SerializedProperty GetMappingProperty()
        {
            var serialized = new SerializedObject(authoringTarget);
            return serialized.FindProperty("semanticMappings").GetArrayElementAtIndex(0);
        }

        private float Weight(string name)
        {
            return renderer.GetBlendShapeWeight(mesh.GetBlendShapeIndex(name));
        }

        private static Mesh CreateMesh(params string[] names)
        {
            var result = new Mesh { name = "PandaFacialMultiTargetTestMesh" };
            result.vertices = new[] { Vector3.zero };
            foreach (string name in names)
            {
                result.AddBlendShapeFrame(name, 100f, new[] { Vector3.right },
                    new[] { Vector3.zero }, new[] { Vector3.zero });
            }
            return result;
        }
    }

    internal sealed class PandaFacialAutoDetectTests
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
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            if (mesh != null)
                Object.DestroyImmediate(mesh);
        }

        [TestCase("Fcl_MTH_A", PandaFacialDetectionMatchKind.Exact)]
        [TestCase("fcl_mth_a", PandaFacialDetectionMatchKind.CaseInsensitive)]
        [TestCase("Fcl-MTH A", PandaFacialDetectionMatchKind.Normalized)]
        public void Detector_UsesSafePriorityMatching(string blendShapeName, PandaFacialDetectionMatchKind expected)
        {
            PandaFacialDetectionEntry entry = DetectSingle(
                PandaFacialSemanticChannels.MouthA, blendShapeName);

            Assert.That(entry.Status, Is.EqualTo(PandaFacialDetectionStatus.Detected));
            Assert.That(entry.MatchKind, Is.EqualTo(expected));
            Assert.That(entry.DetectedBlendShape, Is.EqualTo(blendShapeName));
        }

        [Test]
        public void Detector_LeavesUnknownNameUnmapped()
        {
            PandaFacialDetectionEntry entry = DetectSingle(
                PandaFacialSemanticChannels.MouthA, "CompletelyDifferent");
            Assert.That(entry.Status, Is.EqualTo(PandaFacialDetectionStatus.Unmapped));
        }

        [Test]
        public void Detector_DoesNotChooseWhenSamePriorityIsAmbiguous()
        {
            PandaFacialDetectionEntry entry = DetectSingle(
                PandaFacialSemanticChannels.MouthA, "A", "Fcl_MTH_A");
            Assert.That(entry.Status, Is.EqualTo(PandaFacialDetectionStatus.Ambiguous));
            Assert.That(entry.Candidates, Is.EquivalentTo(new[] { "A", "Fcl_MTH_A" }));
        }

        [Test]
        public void Detector_SkipsExistingSemanticMapping()
        {
            var existing = new HashSet<string> { PandaFacialSemanticChannels.MouthA };
            PandaFacialDetectionReport report = PandaFacialAutoDetector.Detect(
                new[] { Channel(PandaFacialSemanticChannels.MouthA) },
                new[] { "Fcl_MTH_A" },
                PandaFacialBuiltInDetectionDictionary.Instance,
                existing);

            Assert.That(report.Entries.Single().Status,
                Is.EqualTo(PandaFacialDetectionStatus.SkippedExisting));
        }

        [Test]
        public void AutoDetect_RequiresDefaultRendererAndBlendShapes()
        {
            PandaFacialDetectionReport missingRenderer =
                PandaFacialAutoDetectUtility.DetectAndApply(authoringTarget);
            PandaFacialSemanticMappingTests.SetDefaultRenderer(authoringTarget, renderer);
            mesh = new Mesh();
            renderer.sharedMesh = mesh;
            PandaFacialDetectionReport noBlendShapes =
                PandaFacialAutoDetectUtility.DetectAndApply(authoringTarget);

            Assert.That(missingRenderer.Error, Does.Contain("Default Face Renderer"));
            Assert.That(noBlendShapes.Error, Does.Contain("no BlendShapes"));
            Assert.That(authoringTarget.SemanticMappings, Is.Empty);
        }

        [Test]
        public void AutoDetect_MapsOnlyUnmappedAndDoesNotChangeWeightsOrAnimation()
        {
            AssignMesh("Fcl_MTH_A", "Fcl_EYE_Close_L", "ManualShape");
            renderer.SetBlendShapeWeight(mesh.GetBlendShapeIndex("Fcl_MTH_A"), 33f);
            PandaFacialSemanticMappingTests.AddMapping(
                authoringTarget, PandaFacialSemanticChannels.EyeCloseL, null, "ManualShape");
            var clip = new AnimationClip();
            EditorCurveBinding unrelated = EditorCurveBinding.FloatCurve(
                string.Empty, typeof(Transform), "m_LocalPosition.x");
            AnimationUtility.SetEditorCurve(clip, unrelated, AnimationCurve.Constant(0f, 1f, 4f));

            try
            {
                PandaFacialDetectionReport report =
                    PandaFacialAutoDetectUtility.DetectAndApply(authoringTarget);

                Assert.That(report.MappedCount, Is.GreaterThanOrEqualTo(1));
                Assert.That(report.SkippedExistingCount, Is.EqualTo(1));
                Assert.That(authoringTarget.SemanticMappings.Single(mapping =>
                    mapping.SemanticId == PandaFacialSemanticChannels.EyeCloseL).BlendShapeName,
                    Is.EqualTo("ManualShape"));
                Assert.That(renderer.GetBlendShapeWeight(mesh.GetBlendShapeIndex("Fcl_MTH_A")),
                    Is.EqualTo(33f));
                Assert.That(AnimationUtility.GetCurveBindings(clip), Is.EqualTo(new[] { unrelated }));
                Assert.That(AnimationUtility.GetEditorCurve(clip, unrelated).Evaluate(0.5f), Is.EqualTo(4f));
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void AutoDetect_CanBeUndoneAsOneOperation()
        {
            AssignMesh("Fcl_MTH_A", "Fcl_MTH_I", "Fcl_EYE_Close_L");

            PandaFacialDetectionReport report =
                PandaFacialAutoDetectUtility.DetectAndApply(authoringTarget);
            Assert.That(report.MappedCount, Is.EqualTo(3));
            Assert.That(authoringTarget.SemanticMappings.Count, Is.EqualTo(3));

            Undo.PerformUndo();

            Assert.That(authoringTarget.SemanticMappings, Is.Empty);
        }

        [TestCase(PandaFacialSemanticChannels.MouthI, "Fcl_MTH_I")]
        [TestCase(PandaFacialSemanticChannels.EyeCloseL, "Fcl_EYE_Close_L")]
        public void ConfirmedBuiltInAliasFixtures_AreDetected(string semanticId, string confirmedName)
        {
            PandaFacialDetectionEntry entry = DetectSingle(semanticId, confirmedName);
            Assert.That(entry.Status, Is.EqualTo(PandaFacialDetectionStatus.Detected));
            Assert.That(entry.DetectedBlendShape, Is.EqualTo(confirmedName));
        }

        private PandaFacialDetectionEntry DetectSingle(string semanticId, params string[] names)
        {
            return PandaFacialAutoDetector.Detect(
                    new[] { Channel(semanticId) },
                    names,
                    PandaFacialBuiltInDetectionDictionary.Instance)
                .Entries.Single();
        }

        private static PandaFacialSemanticChannel Channel(string semanticId)
        {
            Assert.That(PandaFacialSemanticChannels.TryGet(semanticId, out PandaFacialSemanticChannel channel),
                Is.True);
            return channel;
        }

        private void AssignMesh(params string[] names)
        {
            mesh = new Mesh { name = "PandaFacialAutoDetectTestMesh" };
            mesh.vertices = new[] { Vector3.zero };
            foreach (string name in names)
            {
                mesh.AddBlendShapeFrame(name, 100f, new[] { Vector3.right },
                    new[] { Vector3.zero }, new[] { Vector3.zero });
            }
            renderer.sharedMesh = mesh;
            PandaFacialSemanticMappingTests.SetDefaultRenderer(authoringTarget, renderer);
        }
    }
}
