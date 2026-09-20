using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SillBill.PandaFacial.Editor.Tests
{
    internal sealed class PandaFacialAnimationUtilityTests
    {
        private GameObject root;
        private GameObject face;
        private SkinnedMeshRenderer renderer;
        private AnimationClip clip;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("CharacterRoot");
            face = new GameObject("Head");
            face.transform.SetParent(root.transform);
            renderer = face.AddComponent<SkinnedMeshRenderer>();
            clip = new AnimationClip { frameRate = 24f };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(clip);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void CreateBinding_UsesActualHierarchyAndBlendShapeName()
        {
            EditorCurveBinding binding = PandaFacialAnimationUtility.CreateBlendShapeBinding(
                root.transform,
                renderer,
                "Fcl_MTH_A");

            Assert.That(binding.path, Is.EqualTo("Head"));
            Assert.That(binding.type, Is.EqualTo(typeof(SkinnedMeshRenderer)));
            Assert.That(binding.propertyName, Is.EqualTo("blendShape.Fcl_MTH_A"));
        }

        [Test]
        public void WriteKey_PreservesUnrelatedCurvesAndUpdatesExistingKey()
        {
            EditorCurveBinding blendShapeBinding = PandaFacialAnimationUtility.CreateBlendShapeBinding(
                root.transform,
                renderer,
                "Fcl_MTH_A");
            EditorCurveBinding unrelatedBinding = EditorCurveBinding.FloatCurve(
                string.Empty,
                typeof(Transform),
                "m_LocalPosition.x");
            AnimationUtility.SetEditorCurve(clip, unrelatedBinding, AnimationCurve.Linear(0f, 1f, 1f, 2f));

            PandaFacialAnimationUtility.WriteBlendShapeKey(clip, blendShapeBinding, 1f, 25f);
            PandaFacialAnimationUtility.WriteBlendShapeKey(clip, blendShapeBinding, 1f, 75f);

            AnimationCurve blendShapeCurve = AnimationUtility.GetEditorCurve(clip, blendShapeBinding);
            AnimationCurve unrelatedCurve = AnimationUtility.GetEditorCurve(clip, unrelatedBinding);
            Assert.That(blendShapeCurve, Is.Not.Null);
            Assert.That(blendShapeCurve.length, Is.EqualTo(1));
            Assert.That(blendShapeCurve.keys[0].value, Is.EqualTo(75f));
            Assert.That(unrelatedCurve, Is.Not.Null);
            Assert.That(unrelatedCurve.length, Is.EqualTo(2));
        }

        [Test]
        public void WriteKey_CanBeUndone()
        {
            EditorCurveBinding binding = PandaFacialAnimationUtility.CreateBlendShapeBinding(
                root.transform,
                renderer,
                "Fcl_MTH_A");
            AnimationUtility.SetEditorCurve(clip, binding, new AnimationCurve(new Keyframe(0.5f, 10f)));

            int undoGroup = Undo.GetCurrentGroup();
            PandaFacialAnimationUtility.WriteBlendShapeKey(clip, binding, 0.5f, 80f);
            Undo.CollapseUndoOperations(undoGroup);
            Assert.That(AnimationUtility.GetEditorCurve(clip, binding).keys[0].value, Is.EqualTo(80f));

            Undo.PerformUndo();

            AnimationCurve restored = AnimationUtility.GetEditorCurve(clip, binding);
            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.keys[0].value, Is.EqualTo(10f));
        }
    }
}
