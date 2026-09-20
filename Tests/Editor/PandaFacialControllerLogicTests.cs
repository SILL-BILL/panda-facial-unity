using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SillBill.PandaFacial.Editor.Tests
{
    internal sealed class PandaFacialControllerLogicTests
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
            mesh = CreateMeshWithBlendShapes(
                "Left", "Right", "Down", "Up",
                "NarrowL", "NarrowR", "SpreadL", "SpreadR",
                "CornerDownL", "CornerUpL", "CornerDownR", "CornerUpR",
                "A", "I", "U", "E", "O", "BlinkL", "BlinkR");
            renderer.sharedMesh = mesh;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(mesh);
        }

        [TestCase(-1f, 100f, 0f)]
        [TestCase(-0.5f, 50f, 0f)]
        [TestCase(0f, 0f, 0f)]
        [TestCase(0.5f, 0f, 50f)]
        [TestCase(1f, 0f, 100f)]
        public void MouthPositionX_DistributesAndZerosOpposite(float input, float left, float right)
        {
            IReadOnlyList<PandaFacialSemanticWeight> result =
                PandaFacialControllerLogic.MouthPositionX(input);

            AssertWeight(result, PandaFacialSemanticChannels.MouthLeft, left);
            AssertWeight(result, PandaFacialSemanticChannels.MouthRight, right);
        }

        [TestCase(-1f, 100f, 0f)]
        [TestCase(0f, 0f, 0f)]
        [TestCase(1f, 0f, 100f)]
        public void MouthPositionY_DistributesAndZerosOpposite(float input, float down, float up)
        {
            IReadOnlyList<PandaFacialSemanticWeight> result =
                PandaFacialControllerLogic.MouthPositionY(input);

            AssertWeight(result, PandaFacialSemanticChannels.MouthDown, down);
            AssertWeight(result, PandaFacialSemanticChannels.MouthUp, up);
        }

        [TestCase(-1f, 100f, 0f)]
        [TestCase(0f, 0f, 0f)]
        [TestCase(1f, 0f, 100f)]
        public void MouthWidth_DrivesBothSidesAndZerosOpposites(float input, float narrow, float spread)
        {
            IReadOnlyList<PandaFacialSemanticWeight> result = PandaFacialControllerLogic.MouthWidth(input);

            AssertWeight(result, PandaFacialSemanticChannels.MouthNarrowL, narrow);
            AssertWeight(result, PandaFacialSemanticChannels.MouthNarrowR, narrow);
            AssertWeight(result, PandaFacialSemanticChannels.MouthSpreadL, spread);
            AssertWeight(result, PandaFacialSemanticChannels.MouthSpreadR, spread);
        }

        [TestCase(-1f, 100f, 0f)]
        [TestCase(0f, 0f, 0f)]
        [TestCase(1f, 0f, 100f)]
        public void MouthCorners_DistributeIndependently(float input, float down, float up)
        {
            IReadOnlyList<PandaFacialSemanticWeight> left =
                PandaFacialControllerLogic.MouthCornerLeft(input);
            IReadOnlyList<PandaFacialSemanticWeight> right =
                PandaFacialControllerLogic.MouthCornerRight(input);

            AssertWeight(left, PandaFacialSemanticChannels.MouthCornerDownL, down);
            AssertWeight(left, PandaFacialSemanticChannels.MouthCornerUpL, up);
            AssertWeight(right, PandaFacialSemanticChannels.MouthCornerDownR, down);
            AssertWeight(right, PandaFacialSemanticChannels.MouthCornerUpR, up);
        }

        [Test]
        public void MouthCornerXY_DistributesOuterInnerAndUpDownForEachScreenSide()
        {
            IReadOnlyList<PandaFacialSemanticWeight> left =
                PandaFacialControllerLogic.MouthCornerLeft(0.7f, 0.4f);
            IReadOnlyList<PandaFacialSemanticWeight> right =
                PandaFacialControllerLogic.MouthCornerRight(-0.7f, 0.4f);

            AssertWeight(left, PandaFacialSemanticChannels.MouthSpreadL, 70f);
            AssertWeight(left, PandaFacialSemanticChannels.MouthNarrowL, 0f);
            AssertWeight(left, PandaFacialSemanticChannels.MouthCornerUpL, 40f);
            AssertWeight(left, PandaFacialSemanticChannels.MouthCornerDownL, 0f);
            AssertWeight(right, PandaFacialSemanticChannels.MouthSpreadR, 70f);
            AssertWeight(right, PandaFacialSemanticChannels.MouthNarrowR, 0f);
            AssertWeight(right, PandaFacialSemanticChannels.MouthCornerUpR, 40f);
            AssertWeight(right, PandaFacialSemanticChannels.MouthCornerDownR, 0f);
        }

        [Test]
        public void MouthCornerMirror_IsBidirectionalAndPreservesOuterInnerMeaning()
        {
            IReadOnlyList<PandaFacialSemanticWeight> fromLeft =
                PandaFacialControllerLogic.MirrorMouthCornersFromLeft(-0.3f, -0.2f);
            IReadOnlyList<PandaFacialSemanticWeight> fromRight =
                PandaFacialControllerLogic.MirrorMouthCornersFromRight(0.3f, -0.2f);

            foreach (IReadOnlyList<PandaFacialSemanticWeight> outputs in
                     new[] { fromLeft, fromRight })
            {
                AssertWeight(outputs, PandaFacialSemanticChannels.MouthNarrowL, 30f);
                AssertWeight(outputs, PandaFacialSemanticChannels.MouthNarrowR, 30f);
                AssertWeight(outputs, PandaFacialSemanticChannels.MouthSpreadL, 0f);
                AssertWeight(outputs, PandaFacialSemanticChannels.MouthSpreadR, 0f);
                AssertWeight(outputs, PandaFacialSemanticChannels.MouthCornerDownL, 20f);
                AssertWeight(outputs, PandaFacialSemanticChannels.MouthCornerDownR, 20f);
            }
        }

        [Test]
        public void MirrorPreview_UndoesBothCornersAsOneOperation()
        {
            AddMapping(PandaFacialSemanticChannels.MouthNarrowL, renderer, "NarrowL");
            AddMapping(PandaFacialSemanticChannels.MouthNarrowR, renderer, "NarrowR");
            AddMapping(PandaFacialSemanticChannels.MouthSpreadL, renderer, "SpreadL");
            AddMapping(PandaFacialSemanticChannels.MouthSpreadR, renderer, "SpreadR");
            AddMapping(PandaFacialSemanticChannels.MouthCornerDownL, renderer, "CornerDownL");
            AddMapping(PandaFacialSemanticChannels.MouthCornerDownR, renderer, "CornerDownR");
            AddMapping(PandaFacialSemanticChannels.MouthCornerUpL, renderer, "CornerUpL");
            AddMapping(PandaFacialSemanticChannels.MouthCornerUpR, renderer, "CornerUpR");

            int undoGroup = Undo.GetCurrentGroup();
            PandaFacialControllerOperationReport report =
                PandaFacialControllerAnimationUtility.ApplyPreview(
                    authoringTarget,
                    PandaFacialControllerLogic.MirrorMouthCornersFromLeft(0.6f, 0.4f));
            Undo.CollapseUndoOperations(undoGroup);

            Assert.That(report.AppliedCount, Is.EqualTo(8));
            Assert.That(renderer.GetBlendShapeWeight(mesh.GetBlendShapeIndex("SpreadL")), Is.EqualTo(60f).Within(0.001f));
            Assert.That(renderer.GetBlendShapeWeight(mesh.GetBlendShapeIndex("SpreadR")), Is.EqualTo(60f).Within(0.001f));
            Assert.That(renderer.GetBlendShapeWeight(mesh.GetBlendShapeIndex("CornerUpL")), Is.EqualTo(40f).Within(0.001f));
            Assert.That(renderer.GetBlendShapeWeight(mesh.GetBlendShapeIndex("CornerUpR")), Is.EqualTo(40f).Within(0.001f));

            Undo.PerformUndo();

            Assert.That(renderer.GetBlendShapeWeight(mesh.GetBlendShapeIndex("SpreadL")), Is.EqualTo(0f));
            Assert.That(renderer.GetBlendShapeWeight(mesh.GetBlendShapeIndex("SpreadR")), Is.EqualTo(0f));
            Assert.That(renderer.GetBlendShapeWeight(mesh.GetBlendShapeIndex("CornerUpL")), Is.EqualTo(0f));
            Assert.That(renderer.GetBlendShapeWeight(mesh.GetBlendShapeIndex("CornerUpR")), Is.EqualTo(0f));
        }

        [Test]
        public void VowelsAndBlink_AreIndependentAndClampUnsignedInputs()
        {
            IReadOnlyList<PandaFacialSemanticWeight> vowels =
                PandaFacialControllerLogic.Vowels(0.2f, 0.4f, 0.6f, 0.8f, 1f);
            IReadOnlyList<PandaFacialSemanticWeight> blink = PandaFacialControllerLogic.Blink(0.25f, 0.75f);

            AssertWeight(vowels, PandaFacialSemanticChannels.MouthA, 20f);
            AssertWeight(vowels, PandaFacialSemanticChannels.MouthI, 40f);
            AssertWeight(vowels, PandaFacialSemanticChannels.MouthU, 60f);
            AssertWeight(vowels, PandaFacialSemanticChannels.MouthE, 80f);
            AssertWeight(vowels, PandaFacialSemanticChannels.MouthO, 100f);
            AssertWeight(blink, PandaFacialSemanticChannels.EyeCloseL, 25f);
            AssertWeight(blink, PandaFacialSemanticChannels.EyeCloseR, 75f);
        }

        [Test]
        public void Inputs_AreClampedAndNaNBecomesNeutral()
        {
            AssertWeight(
                PandaFacialControllerLogic.MouthPositionX(-2f),
                PandaFacialSemanticChannels.MouthLeft,
                100f);
            AssertWeight(
                PandaFacialControllerLogic.MouthPositionX(2f),
                PandaFacialSemanticChannels.MouthRight,
                100f);
            AssertWeight(
                PandaFacialControllerLogic.MouthPositionX(float.NaN),
                PandaFacialSemanticChannels.MouthRight,
                0f);
            AssertWeight(
                PandaFacialControllerLogic.Single(PandaFacialSemanticChannels.BrowUpL, 2f),
                PandaFacialSemanticChannels.BrowUpL,
                100f);
        }

        [Test]
        public void PartialAndInvalidMappings_DoNotBlockValidControllerOutputs()
        {
            AddMapping(PandaFacialSemanticChannels.MouthSpreadL, renderer, "SpreadL");
            AddMapping(PandaFacialSemanticChannels.MouthSpreadR, renderer, "DeletedShape");
            var clip = new AnimationClip { frameRate = 24f };

            try
            {
                PandaFacialControllerOperationReport report =
                    PandaFacialControllerAnimationUtility.WriteKeys(
                        authoringTarget,
                        PandaFacialControllerLogic.MouthWidth(1f),
                        clip,
                        root.transform,
                        1f);

                Assert.That(report.AppliedCount, Is.EqualTo(1));
                Assert.That(report.UnmappedCount, Is.EqualTo(2));
                Assert.That(report.InvalidCount, Is.EqualTo(1));
                Assert.That(
                    AnimationUtility.GetCurveBindings(clip).Single().propertyName,
                    Is.EqualTo("blendShape.SpreadL"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void MultipleOutputs_WriteAtSameTimePreserveUnrelatedCurveAndUndoAsOneOperation()
        {
            AddMapping(PandaFacialSemanticChannels.MouthNarrowL, renderer, "NarrowL");
            AddMapping(PandaFacialSemanticChannels.MouthNarrowR, renderer, "NarrowR");
            AddMapping(PandaFacialSemanticChannels.MouthSpreadL, renderer, "SpreadL");
            AddMapping(PandaFacialSemanticChannels.MouthSpreadR, renderer, "SpreadR");
            var clip = new AnimationClip { frameRate = 24f };
            EditorCurveBinding unrelated = EditorCurveBinding.FloatCurve(
                string.Empty,
                typeof(Transform),
                "m_LocalPosition.x");
            AnimationUtility.SetEditorCurve(clip, unrelated, AnimationCurve.Constant(0f, 2f, 3f));

            try
            {
                int undoGroup = Undo.GetCurrentGroup();
                PandaFacialControllerOperationReport report =
                    PandaFacialControllerAnimationUtility.WriteKeys(
                        authoringTarget,
                        PandaFacialControllerLogic.MouthWidth(0.5f),
                        clip,
                        root.transform,
                        1f);
                Undo.CollapseUndoOperations(undoGroup);

                Assert.That(report.AppliedCount, Is.EqualTo(4));
                EditorCurveBinding[] blendShapeBindings = AnimationUtility.GetCurveBindings(clip)
                    .Where(binding => binding.type == typeof(SkinnedMeshRenderer))
                    .ToArray();
                Assert.That(blendShapeBindings.Length, Is.EqualTo(4));
                foreach (EditorCurveBinding binding in blendShapeBindings)
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    Assert.That(curve.keys.Single().time, Is.EqualTo(1f));
                    float expected = binding.propertyName.Contains("Spread") ? 50f : 0f;
                    Assert.That(curve.keys.Single().value, Is.EqualTo(expected));
                }

                Assert.That(AnimationUtility.GetEditorCurve(clip, unrelated).Evaluate(1f), Is.EqualTo(3f));
                Assert.That(
                    AnimationUtility.GetCurveBindings(clip)
                        .Any(binding => binding.type == typeof(PandaFacialAuthoringTarget)),
                    Is.False);

                Undo.PerformUndo();

                Assert.That(
                    AnimationUtility.GetCurveBindings(clip)
                        .Count(binding => binding.type == typeof(SkinnedMeshRenderer)),
                    Is.EqualTo(0));
                Assert.That(AnimationUtility.GetEditorCurve(clip, unrelated).Evaluate(1f), Is.EqualTo(3f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void ControllerPreview_ChangesMappedBlendShapesAndUndoRestoresBoth()
        {
            AddMapping(PandaFacialSemanticChannels.MouthLeft, renderer, "Left");
            AddMapping(PandaFacialSemanticChannels.MouthRight, renderer, "Right");
            renderer.SetBlendShapeWeight(mesh.GetBlendShapeIndex("Left"), 12f);
            renderer.SetBlendShapeWeight(mesh.GetBlendShapeIndex("Right"), 34f);

            int undoGroup = Undo.GetCurrentGroup();
            PandaFacialControllerOperationReport report =
                PandaFacialControllerAnimationUtility.ApplyPreview(
                    authoringTarget,
                    PandaFacialControllerLogic.MouthPositionX(1f));
            Undo.CollapseUndoOperations(undoGroup);

            Assert.That(report.AppliedCount, Is.EqualTo(2));
            Assert.That(renderer.GetBlendShapeWeight(mesh.GetBlendShapeIndex("Left")), Is.EqualTo(0f));
            Assert.That(renderer.GetBlendShapeWeight(mesh.GetBlendShapeIndex("Right")), Is.EqualTo(100f));

            Undo.PerformUndo();

            Assert.That(renderer.GetBlendShapeWeight(mesh.GetBlendShapeIndex("Left")), Is.EqualTo(12f));
            Assert.That(renderer.GetBlendShapeWeight(mesh.GetBlendShapeIndex("Right")), Is.EqualTo(34f));
        }

        private void AddMapping(string semanticId, SkinnedMeshRenderer targetRenderer, string blendShapeName)
        {
            PandaFacialSemanticMappingTests.AddMapping(
                authoringTarget,
                semanticId,
                targetRenderer,
                blendShapeName);
        }

        private static void AssertWeight(
            IReadOnlyList<PandaFacialSemanticWeight> result,
            string semanticId,
            float expected)
        {
            PandaFacialSemanticWeight output = result.Single(item => item.SemanticId == semanticId);
            Assert.That(output.Weight, Is.EqualTo(expected).Within(0.0001f));
        }

        private static Mesh CreateMeshWithBlendShapes(params string[] names)
        {
            var result = new Mesh { name = "PandaFacialControllerTestMesh" };
            result.vertices = new[] { Vector3.zero };
            foreach (string name in names)
            {
                result.AddBlendShapeFrame(
                    name,
                    100f,
                    new[] { Vector3.right },
                    new[] { Vector3.zero },
                    new[] { Vector3.zero });
            }
            return result;
        }
    }
}
