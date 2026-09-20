using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SillBill.PandaFacial.Editor.Tests
{
    internal sealed class PandaFacialUpperFaceControllerTests
    {
        private static readonly string[] EyeChannels =
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
            PandaFacialSemanticChannels.EyeSquintL,
            PandaFacialSemanticChannels.EyeSquintR
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
            mesh = CreateMesh(EyeChannels.Concat(BrowChannels));
            renderer.sharedMesh = mesh;
            PandaFacialUpperFaceFoldoutState.Clear(authoringTarget);
        }

        [TearDown]
        public void TearDown()
        {
            PandaFacialUpperFaceFoldoutState.Clear(authoringTarget);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void SemanticChannels_UseHumanReadableNamesAndContainNoEyeLook()
        {
            string[] names = PandaFacialSemanticChannels.BuiltIn
                .Where(channel => channel.Group == PandaFacialSemanticGroup.EyeExpression ||
                                  channel.Group == PandaFacialSemanticGroup.Brow)
                .Select(channel => channel.DisplayName)
                .ToArray();

            Assert.That(names, Does.Contain("Eyelid Close Left"));
            Assert.That(names, Does.Contain("Eyelid Jito Right"));
            Assert.That(names, Does.Contain("Eye Smile Right"));
            Assert.That(names, Does.Contain("Brow Serious Left"));
            Assert.That(names.All(name => !name.Contains("eye_") && !name.Contains("brow_")), Is.True);
            Assert.That(PandaFacialSemanticChannels.BuiltIn.All(channel =>
                !channel.Id.Contains("look") && !channel.Id.Contains("aim")), Is.True);
        }

        [TestCase(PandaFacialSemanticChannels.EyeCloseL, PandaFacialSemanticChannels.EyeCloseR)]
        [TestCase(PandaFacialSemanticChannels.EyeSmileL, PandaFacialSemanticChannels.EyeSmileR)]
        [TestCase(PandaFacialSemanticChannels.EyeSurpriseL, PandaFacialSemanticChannels.EyeSurpriseR)]
        [TestCase(PandaFacialSemanticChannels.EyeAngryL, PandaFacialSemanticChannels.EyeAngryR)]
        [TestCase(PandaFacialSemanticChannels.EyeSadL, PandaFacialSemanticChannels.EyeSadR)]
        [TestCase(PandaFacialSemanticChannels.EyeSquintL, PandaFacialSemanticChannels.EyeSquintR)]
        [TestCase(PandaFacialSemanticChannels.BrowUpL, PandaFacialSemanticChannels.BrowUpR)]
        [TestCase(PandaFacialSemanticChannels.BrowDownL, PandaFacialSemanticChannels.BrowDownR)]
        [TestCase(PandaFacialSemanticChannels.BrowAngryL, PandaFacialSemanticChannels.BrowAngryR)]
        [TestCase(PandaFacialSemanticChannels.BrowSadL, PandaFacialSemanticChannels.BrowSadR)]
        [TestCase(PandaFacialSemanticChannels.BrowSmileL, PandaFacialSemanticChannels.BrowSmileR)]
        [TestCase(PandaFacialSemanticChannels.BrowSeriousL, PandaFacialSemanticChannels.BrowSeriousR)]
        public void PairLogic_PreservesIndependentLeftAndRightValues(string leftId, string rightId)
        {
            IReadOnlyList<PandaFacialSemanticWeight> outputs =
                PandaFacialControllerLogic.Pair(leftId, rightId, 0.75f, 0.25f);

            Assert.That(outputs.Single(output => output.SemanticId == leftId).Weight,
                Is.EqualTo(75f));
            Assert.That(outputs.Single(output => output.SemanticId == rightId).Weight,
                Is.EqualTo(25f));
        }

        [Test]
        public void Foldouts_UseMappedInitialStateAndRememberManualChoiceIndependently()
        {
            Assert.That(PandaFacialUpperFaceFoldoutState.Get(
                authoringTarget, PandaFacialUpperFaceSection.EyeExpression), Is.False);
            Assert.That(PandaFacialUpperFaceFoldoutState.Get(
                authoringTarget, PandaFacialUpperFaceSection.Brow), Is.False);

            Map(PandaFacialSemanticChannels.EyeCloseL);
            Map(PandaFacialSemanticChannels.BrowUpL);
            PandaFacialUpperFaceFoldoutState.Clear(authoringTarget);
            Assert.That(PandaFacialUpperFaceFoldoutState.Get(
                authoringTarget, PandaFacialUpperFaceSection.EyeExpression), Is.True);
            Assert.That(PandaFacialUpperFaceFoldoutState.Get(
                authoringTarget, PandaFacialUpperFaceSection.Brow), Is.True);

            PandaFacialUpperFaceFoldoutState.Set(
                authoringTarget, PandaFacialUpperFaceSection.EyeExpression, false);
            PandaFacialUpperFaceFoldoutState.Set(
                authoringTarget, PandaFacialUpperFaceSection.Brow, false);
            Assert.That(PandaFacialUpperFaceFoldoutState.Get(
                authoringTarget, PandaFacialUpperFaceSection.EyeExpression), Is.False);
            Assert.That(PandaFacialUpperFaceFoldoutState.Get(
                authoringTarget, PandaFacialUpperFaceSection.Brow), Is.False);
        }

        [Test]
        public void CurrentValueReader_ReconstructsEveryMappedUpperFaceChannel()
        {
            string[] channels = EyeChannels.Concat(BrowChannels).ToArray();
            for (int i = 0; i < channels.Length; i++)
            {
                Map(channels[i]);
                float weight = (i + 1) * 3f;
                SetWeight(channels[i], weight);
                PandaFacialControllerValueState state =
                    PandaFacialControllerValueReader.ReadSingle(authoringTarget, "Control", channels[i]);

                Assert.That(state.Value, Is.EqualTo(weight / 100f).Within(0.0001f));
                Assert.That(state.MappedCount, Is.EqualTo(1));
                Assert.That(state.HasWarning, Is.False);
            }
        }

        [Test]
        public void PartialAndInvalidMapping_AreIsolatedPerChannel()
        {
            Map(PandaFacialSemanticChannels.EyeCloseL);
            PandaFacialSemanticMappingTests.AddMapping(
                authoringTarget,
                PandaFacialSemanticChannels.EyeCloseR,
                renderer,
                "MissingBlinkRight");
            SetWeight(PandaFacialSemanticChannels.EyeCloseL, 20f);

            PandaFacialControllerValueState left = PandaFacialControllerValueReader.ReadSingle(
                authoringTarget, "Eyelid Close Left", PandaFacialSemanticChannels.EyeCloseL);
            PandaFacialControllerValueState right = PandaFacialControllerValueReader.ReadSingle(
                authoringTarget, "Eyelid Close Right", PandaFacialSemanticChannels.EyeCloseR);
            PandaFacialControllerOperationReport report =
                PandaFacialControllerAnimationUtility.ApplyPreview(
                    authoringTarget,
                    PandaFacialControllerLogic.Blink(0.8f, 0.6f));

            Assert.That(left.Value, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(left.MappedCount, Is.EqualTo(1));
            Assert.That(right.InvalidCount, Is.EqualTo(1));
            Assert.That(report.AppliedCount, Is.EqualTo(1));
            Assert.That(report.InvalidCount, Is.EqualTo(1));
            Assert.That(GetWeight(PandaFacialSemanticChannels.EyeCloseL), Is.EqualTo(80f));
        }

        [Test]
        public void PairPreview_ChangesOnlyRequestedSide()
        {
            Map(PandaFacialSemanticChannels.EyeCloseL);
            Map(PandaFacialSemanticChannels.EyeCloseR);
            SetWeight(PandaFacialSemanticChannels.EyeCloseR, 45f);

            PandaFacialControllerAnimationUtility.ApplyPreview(
                authoringTarget,
                PandaFacialControllerLogic.Single(PandaFacialSemanticChannels.EyeCloseL, 0.7f));

            Assert.That(GetWeight(PandaFacialSemanticChannels.EyeCloseL), Is.EqualTo(70f));
            Assert.That(GetWeight(PandaFacialSemanticChannels.EyeCloseR), Is.EqualTo(45f));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SectionReset_ResetsAllChannelsAndUndoesAsOneOperation(bool eyeSection)
        {
            string[] channels = eyeSection ? EyeChannels : BrowChannels;
            foreach (string channel in channels)
            {
                Map(channel);
                SetWeight(channel, 55f);
            }

            PandaFacialControllerOperationReport report =
                PandaFacialControllerAnimationUtility.ApplyPreview(
                    authoringTarget,
                    eyeSection
                        ? PandaFacialControllerLogic.ResetEyeExpressions()
                        : PandaFacialControllerLogic.ResetBrows());

            Assert.That(report.AppliedCount, Is.EqualTo(12));
            Assert.That(channels.All(channel => Mathf.Approximately(GetWeight(channel), 0f)), Is.True);
            Undo.PerformUndo();
            Assert.That(channels.All(channel => Mathf.Approximately(GetWeight(channel), 55f)), Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SetAllKeys_WritesActualBlendShapesAndPreservesExistingCurves(bool eyeSection)
        {
            string[] channels = eyeSection ? EyeChannels : BrowChannels;
            foreach (string channel in channels)
                Map(channel);

            var clip = new AnimationClip();
            var unrelated = EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalPosition.x");
            AnimationUtility.SetEditorCurve(clip, unrelated, AnimationCurve.Constant(0f, 1f, 3f));
            IReadOnlyList<PandaFacialSemanticWeight> outputs = eyeSection
                ? PandaFacialControllerLogic.EyeExpressions(
                    0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f,
                    0.7f, 0.8f, 0.9f, 1f, 0.25f, 0.75f)
                : PandaFacialControllerLogic.Brows(
                    0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f,
                    0.7f, 0.8f, 0.9f, 1f, 0.25f, 0.75f);

            PandaFacialControllerOperationReport report =
                PandaFacialControllerAnimationUtility.WriteKeys(
                    authoringTarget, outputs, clip, root.transform, 0.5f);
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

            Assert.That(report.AppliedCount, Is.EqualTo(12));
            Assert.That(bindings.Count(binding => binding.propertyName.StartsWith("blendShape.")),
                Is.EqualTo(12));
            Assert.That(AnimationUtility.GetEditorCurve(clip, unrelated), Is.Not.Null);
            Assert.That(bindings.Any(binding => binding.type == typeof(PandaFacialAuthoringTarget)), Is.False);
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void PairKey_WritesBothIndependentValuesOnly()
        {
            Map(PandaFacialSemanticChannels.BrowUpL);
            Map(PandaFacialSemanticChannels.BrowUpR);
            var clip = new AnimationClip();

            PandaFacialControllerOperationReport report =
                PandaFacialControllerAnimationUtility.WriteKeys(
                    authoringTarget,
                    PandaFacialControllerLogic.Pair(
                        PandaFacialSemanticChannels.BrowUpL,
                        PandaFacialSemanticChannels.BrowUpR,
                        0.65f,
                        0.15f),
                    clip,
                    root.transform,
                    1f);
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

            Assert.That(report.AppliedCount, Is.EqualTo(2));
            Assert.That(bindings.Select(binding => binding.propertyName), Is.EquivalentTo(new[]
            {
                "blendShape." + PandaFacialSemanticChannels.BrowUpL,
                "blendShape." + PandaFacialSemanticChannels.BrowUpR
            }));
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void AuthoringTarget_HasNoUpperFaceValueOrFoldoutSerialization()
        {
            var serialized = new SerializedObject(authoringTarget);
            Assert.That(serialized.FindProperty("blinkLeft"), Is.Null);
            Assert.That(serialized.FindProperty("browUpLeft"), Is.Null);
            Assert.That(serialized.FindProperty("eyeExpressionFoldout"), Is.Null);
            Assert.That(serialized.FindProperty("browFoldout"), Is.Null);
        }

        private void Map(string semanticId)
        {
            PandaFacialSemanticMappingTests.AddMapping(
                authoringTarget,
                semanticId,
                renderer,
                semanticId);
        }

        private void SetWeight(string semanticId, float value)
        {
            renderer.SetBlendShapeWeight(mesh.GetBlendShapeIndex(semanticId), value);
        }

        private float GetWeight(string semanticId)
        {
            return renderer.GetBlendShapeWeight(mesh.GetBlendShapeIndex(semanticId));
        }

        private static Mesh CreateMesh(IEnumerable<string> names)
        {
            var result = new Mesh { name = "PandaFacialUpperFaceTestMesh" };
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
