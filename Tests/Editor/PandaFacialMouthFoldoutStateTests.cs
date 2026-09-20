using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SillBill.PandaFacial.Editor.Tests
{
    internal sealed class PandaFacialMouthFoldoutStateTests
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
            mesh = new Mesh { name = "PandaFacialFoldoutTestMesh" };
            mesh.vertices = new[] { Vector3.zero };
            AddBlendShape("PositionLeft");
            AddBlendShape("CornerSpreadLeft");
            AddBlendShape("A");
            renderer.sharedMesh = mesh;
            PandaFacialMouthFoldoutState.Clear(authoringTarget);
        }

        [TearDown]
        public void TearDown()
        {
            PandaFacialMouthFoldoutState.Clear(authoringTarget);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void UnmappedSections_InitiallyClosed()
        {
            Assert.That(
                PandaFacialMouthFoldoutState.Get(
                    authoringTarget,
                    PandaFacialMouthSection.Position),
                Is.False);
            Assert.That(
                PandaFacialMouthFoldoutState.Get(
                    authoringTarget,
                    PandaFacialMouthSection.Corners),
                Is.False);
            Assert.That(
                PandaFacialMouthFoldoutState.Get(
                    authoringTarget,
                    PandaFacialMouthSection.Vowels),
                Is.False);
        }

        [Test]
        public void MappedSections_InitiallyOpenIndependently()
        {
            Map(PandaFacialSemanticChannels.MouthLeft, "PositionLeft");
            Map(PandaFacialSemanticChannels.MouthA, "A");

            Assert.That(
                PandaFacialMouthFoldoutState.Get(
                    authoringTarget,
                    PandaFacialMouthSection.Position),
                Is.True);
            Assert.That(
                PandaFacialMouthFoldoutState.Get(
                    authoringTarget,
                    PandaFacialMouthSection.Corners),
                Is.False);
            Assert.That(
                PandaFacialMouthFoldoutState.Get(
                    authoringTarget,
                    PandaFacialMouthSection.Vowels),
                Is.True);
        }

        [Test]
        public void UserState_TakesPriorityAfterInitialMappingEvaluation()
        {
            Assert.That(
                PandaFacialMouthFoldoutState.Get(
                    authoringTarget,
                    PandaFacialMouthSection.Corners),
                Is.False);
            PandaFacialMouthFoldoutState.Set(
                authoringTarget,
                PandaFacialMouthSection.Corners,
                true);
            Map(PandaFacialSemanticChannels.MouthSpreadL, "CornerSpreadLeft");

            Assert.That(
                PandaFacialMouthFoldoutState.Get(
                    authoringTarget,
                    PandaFacialMouthSection.Corners),
                Is.True);

            PandaFacialMouthFoldoutState.Set(
                authoringTarget,
                PandaFacialMouthSection.Corners,
                false);
            Assert.That(
                PandaFacialMouthFoldoutState.Get(
                    authoringTarget,
                    PandaFacialMouthSection.Corners),
                Is.False);
        }

        [Test]
        public void SectionStates_AreIndependentAndNotSerializedOnAuthoringTarget()
        {
            PandaFacialMouthFoldoutState.Set(
                authoringTarget,
                PandaFacialMouthSection.Position,
                true);
            PandaFacialMouthFoldoutState.Set(
                authoringTarget,
                PandaFacialMouthSection.Corners,
                false);
            PandaFacialMouthFoldoutState.Set(
                authoringTarget,
                PandaFacialMouthSection.Vowels,
                true);

            Assert.That(
                PandaFacialMouthFoldoutState.Get(authoringTarget, PandaFacialMouthSection.Position),
                Is.True);
            Assert.That(
                PandaFacialMouthFoldoutState.Get(authoringTarget, PandaFacialMouthSection.Corners),
                Is.False);
            Assert.That(
                PandaFacialMouthFoldoutState.Get(authoringTarget, PandaFacialMouthSection.Vowels),
                Is.True);

            var serializedTarget = new SerializedObject(authoringTarget);
            Assert.That(serializedTarget.FindProperty("mouthPositionFoldout"), Is.Null);
            Assert.That(serializedTarget.FindProperty("mouthCornersFoldout"), Is.Null);
            Assert.That(serializedTarget.FindProperty("aiueoFoldout"), Is.Null);
        }

        [Test]
        public void ClosingSection_DoesNotChangeBlendShapeOrAnimationData()
        {
            renderer.SetBlendShapeWeight(mesh.GetBlendShapeIndex("A"), 45f);
            var clip = new AnimationClip();
            EditorCurveBinding unrelated = EditorCurveBinding.FloatCurve(
                string.Empty,
                typeof(Transform),
                "m_LocalPosition.x");
            AnimationUtility.SetEditorCurve(clip, unrelated, AnimationCurve.Constant(0f, 1f, 3f));

            try
            {
                PandaFacialMouthFoldoutState.Set(
                    authoringTarget,
                    PandaFacialMouthSection.Vowels,
                    false);

                Assert.That(
                    renderer.GetBlendShapeWeight(mesh.GetBlendShapeIndex("A")),
                    Is.EqualTo(45f));
                Assert.That(AnimationUtility.GetCurveBindings(clip).Single(), Is.EqualTo(unrelated));
                Assert.That(
                    AnimationUtility.GetCurveBindings(clip)
                        .Any(binding => binding.type == typeof(PandaFacialAuthoringTarget)),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        private void Map(string semanticId, string blendShapeName)
        {
            PandaFacialSemanticMappingTests.AddMapping(
                authoringTarget,
                semanticId,
                renderer,
                blendShapeName);
        }

        private void AddBlendShape(string name)
        {
            mesh.AddBlendShapeFrame(
                name,
                100f,
                new[] { Vector3.right },
                new[] { Vector3.zero },
                new[] { Vector3.zero });
        }
    }
}
