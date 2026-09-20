using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SillBill.PandaFacial.Editor.Tests
{
    internal sealed class PandaFacialMouthControllerUITests
    {
        private static readonly string[] SemanticIds =
        {
            PandaFacialSemanticChannels.MouthLeft,
            PandaFacialSemanticChannels.MouthRight,
            PandaFacialSemanticChannels.MouthDown,
            PandaFacialSemanticChannels.MouthUp,
            PandaFacialSemanticChannels.MouthCornerDownL,
            PandaFacialSemanticChannels.MouthCornerUpL,
            PandaFacialSemanticChannels.MouthCornerDownR,
            PandaFacialSemanticChannels.MouthCornerUpR,
            PandaFacialSemanticChannels.MouthNarrowL,
            PandaFacialSemanticChannels.MouthNarrowR,
            PandaFacialSemanticChannels.MouthSpreadL,
            PandaFacialSemanticChannels.MouthSpreadR,
            PandaFacialSemanticChannels.MouthA,
            PandaFacialSemanticChannels.MouthI,
            PandaFacialSemanticChannels.MouthU,
            PandaFacialSemanticChannels.MouthE,
            PandaFacialSemanticChannels.MouthO
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
            mesh = CreateMesh(SemanticIds);
            renderer.sharedMesh = mesh;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void MouthPositionXY_UsesCombinedControllerLogicOutputs()
        {
            IReadOnlyList<PandaFacialSemanticWeight> outputs =
                PandaFacialControllerLogic.MouthPosition(0.5f, 0.4f);

            Assert.That(outputs.Count, Is.EqualTo(4));
            AssertWeight(outputs, PandaFacialSemanticChannels.MouthLeft, 0f);
            AssertWeight(outputs, PandaFacialSemanticChannels.MouthRight, 50f);
            AssertWeight(outputs, PandaFacialSemanticChannels.MouthDown, 0f);
            AssertWeight(outputs, PandaFacialSemanticChannels.MouthUp, 40f);
        }

        [Test]
        public void CurrentValueReader_ReconstructsPositionAndReportsOpposingConflict()
        {
            Map(PandaFacialSemanticChannels.MouthLeft);
            Map(PandaFacialSemanticChannels.MouthRight);
            Map(PandaFacialSemanticChannels.MouthDown);
            Map(PandaFacialSemanticChannels.MouthUp);
            SetWeight(PandaFacialSemanticChannels.MouthRight, 50f);
            SetWeight(PandaFacialSemanticChannels.MouthUp, 40f);

            PandaFacialControllerVector2State state =
                PandaFacialControllerValueReader.ReadMouthPosition(authoringTarget);

            Assert.That(state.Value.x, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(state.Value.y, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(state.X.HasWarning, Is.False);
            Assert.That(state.Y.HasWarning, Is.False);

            SetWeight(PandaFacialSemanticChannels.MouthLeft, 60f);
            SetWeight(PandaFacialSemanticChannels.MouthRight, 20f);
            state = PandaFacialControllerValueReader.ReadMouthPosition(authoringTarget);

            Assert.That(state.Value.x, Is.EqualTo(-0.4f).Within(0.0001f));
            Assert.That(state.X.HasConflict, Is.True);
            Assert.That(state.X.HasWarning, Is.True);
        }

        [Test]
        public void CurrentValueReader_WidthUsesAverageOfAvailableSides()
        {
            Map(PandaFacialSemanticChannels.MouthNarrowL);
            Map(PandaFacialSemanticChannels.MouthNarrowR);
            Map(PandaFacialSemanticChannels.MouthSpreadL);
            Map(PandaFacialSemanticChannels.MouthSpreadR);
            SetWeight(PandaFacialSemanticChannels.MouthSpreadL, 40f);
            SetWeight(PandaFacialSemanticChannels.MouthSpreadR, 60f);

            PandaFacialControllerValueState state =
                PandaFacialControllerValueReader.ReadMouthWidth(authoringTarget);

            Assert.That(state.Value, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(state.HasWarning, Is.False);
        }

        [Test]
        public void CurrentValueReader_ReconstructsCornerScreenCoordinates()
        {
            Map(PandaFacialSemanticChannels.MouthNarrowL);
            Map(PandaFacialSemanticChannels.MouthSpreadL);
            Map(PandaFacialSemanticChannels.MouthCornerDownL);
            Map(PandaFacialSemanticChannels.MouthCornerUpL);
            Map(PandaFacialSemanticChannels.MouthNarrowR);
            Map(PandaFacialSemanticChannels.MouthSpreadR);
            Map(PandaFacialSemanticChannels.MouthCornerDownR);
            Map(PandaFacialSemanticChannels.MouthCornerUpR);
            SetWeight(PandaFacialSemanticChannels.MouthSpreadL, 70f);
            SetWeight(PandaFacialSemanticChannels.MouthCornerUpL, 40f);
            SetWeight(PandaFacialSemanticChannels.MouthSpreadR, 60f);
            SetWeight(PandaFacialSemanticChannels.MouthCornerDownR, 20f);

            PandaFacialControllerVector2State left =
                PandaFacialControllerValueReader.ReadMouthCornerLeftXY(authoringTarget);
            PandaFacialControllerVector2State right =
                PandaFacialControllerValueReader.ReadMouthCornerRightXY(authoringTarget);

            Assert.That(left.Value.x, Is.EqualTo(0.7f).Within(0.0001f));
            Assert.That(left.Value.y, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(right.Value.x, Is.EqualTo(-0.6f).Within(0.0001f));
            Assert.That(right.Value.y, Is.EqualTo(-0.2f).Within(0.0001f));
        }

        [Test]
        public void MirrorOff_LeftCornerOutputDoesNotChangeRightCorner()
        {
            Map(PandaFacialSemanticChannels.MouthNarrowL);
            Map(PandaFacialSemanticChannels.MouthSpreadL);
            Map(PandaFacialSemanticChannels.MouthCornerDownL);
            Map(PandaFacialSemanticChannels.MouthCornerUpL);
            Map(PandaFacialSemanticChannels.MouthNarrowR);
            Map(PandaFacialSemanticChannels.MouthSpreadR);
            Map(PandaFacialSemanticChannels.MouthCornerDownR);
            Map(PandaFacialSemanticChannels.MouthCornerUpR);
            SetWeight(PandaFacialSemanticChannels.MouthNarrowR, 25f);
            SetWeight(PandaFacialSemanticChannels.MouthCornerDownR, 35f);

            PandaFacialControllerAnimationUtility.ApplyPreview(
                authoringTarget,
                PandaFacialControllerLogic.MouthCornerLeft(0.5f, 0.4f));

            Assert.That(GetWeight(PandaFacialSemanticChannels.MouthSpreadL), Is.EqualTo(50f));
            Assert.That(GetWeight(PandaFacialSemanticChannels.MouthCornerUpL), Is.EqualTo(40f));
            Assert.That(GetWeight(PandaFacialSemanticChannels.MouthNarrowR), Is.EqualTo(25f));
            Assert.That(GetWeight(PandaFacialSemanticChannels.MouthCornerDownR), Is.EqualTo(35f));
        }

        [Test]
        public void PartialAndInvalidMapping_AreReportedWithoutBlockingValidUIOutput()
        {
            Map(PandaFacialSemanticChannels.MouthLeft);
            PandaFacialSemanticMappingTests.AddMapping(
                authoringTarget,
                PandaFacialSemanticChannels.MouthRight,
                renderer,
                "DeletedShape");
            SetWeight(PandaFacialSemanticChannels.MouthLeft, 25f);

            PandaFacialControllerVector2State state =
                PandaFacialControllerValueReader.ReadMouthPosition(authoringTarget);
            PandaFacialControllerOperationReport report =
                PandaFacialControllerAnimationUtility.ApplyPreview(
                    authoringTarget,
                    PandaFacialControllerLogic.MouthPosition(-0.5f, 0f));

            Assert.That(state.X.Value, Is.EqualTo(-0.25f).Within(0.0001f));
            Assert.That(state.X.MappedCount, Is.EqualTo(1));
            Assert.That(state.X.InvalidCount, Is.EqualTo(1));
            Assert.That(state.Y.UnmappedCount, Is.EqualTo(2));
            Assert.That(report.AppliedCount, Is.EqualTo(1));
            Assert.That(report.InvalidCount, Is.EqualTo(1));
            Assert.That(report.UnmappedCount, Is.EqualTo(2));
            Assert.That(GetWeight(PandaFacialSemanticChannels.MouthLeft), Is.EqualTo(50f));
        }

        [Test]
        public void ResetMouth_ResetsAllMappedChannelsAndUndoesAsOneOperation()
        {
            foreach (string semanticId in SemanticIds)
            {
                Map(semanticId);
                SetWeight(semanticId, 50f);
            }

            int undoGroup = Undo.GetCurrentGroup();
            PandaFacialControllerOperationReport report =
                PandaFacialControllerAnimationUtility.ApplyPreview(
                    authoringTarget,
                    PandaFacialControllerLogic.ResetMouth());
            Undo.CollapseUndoOperations(undoGroup);

            Assert.That(report.AppliedCount, Is.EqualTo(17));
            Assert.That(SemanticIds.All(id => Mathf.Approximately(GetWeight(id), 0f)), Is.True);

            Undo.PerformUndo();

            Assert.That(SemanticIds.All(id => Mathf.Approximately(GetWeight(id), 50f)), Is.True);
        }

        [Test]
        public void IndividualResetOutputs_AreNeutralAndDoNotStoreAuthoringValues()
        {
            Assert.That(
                PandaFacialControllerLogic.MouthPosition(0f, 0f).All(output => output.Weight == 0f),
                Is.True);
            Assert.That(
                PandaFacialControllerLogic.MouthCornerLeft(0f).All(output => output.Weight == 0f),
                Is.True);
            Assert.That(
                PandaFacialControllerLogic.MouthCornerRight(0f).All(output => output.Weight == 0f),
                Is.True);
            Assert.That(
                PandaFacialControllerLogic.MouthWidth(0f).All(output => output.Weight == 0f),
                Is.True);
            Assert.That(
                PandaFacialControllerLogic.Vowels(0f, 0f, 0f, 0f, 0f)
                    .All(output => output.Weight == 0f),
                Is.True);
            Assert.That(
                new SerializedObject(authoringTarget).FindProperty("authoringValue"),
                Is.Null);
        }

        [Test]
        public void ComponentAndEditorDefaults_UseReviewedUserFacingNames()
        {
            var menu = (AddComponentMenu)System.Attribute.GetCustomAttribute(
                typeof(PandaFacialAuthoringTarget),
                typeof(AddComponentMenu));
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.componentMenu, Is.EqualTo("Panda Facial/Authoring Target"));
            Assert.That(
                ObjectNames.NicifyVariableName(nameof(PandaFacialAuthoringTarget)),
                Is.EqualTo("Panda Facial Authoring Target"));

            UnityEditor.Editor customEditor = UnityEditor.Editor.CreateEditor(authoringTarget);
            try
            {
                FieldInfo debugField = customEditor.GetType().GetField(
                    "debugFoldout",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(debugField, Is.Not.Null);
                Assert.That(debugField.GetValue(customEditor), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(customEditor);
            }

            Assert.That(
                PandaFacialSemanticChannels.BuiltIn
                    .Single(channel => channel.Id == PandaFacialSemanticChannels.MouthCornerUpL)
                    .DisplayName,
                Is.EqualTo("Left Corner Up"));
            Assert.That(
                PandaFacialSemanticChannels.BuiltIn
                    .All(channel => channel.DisplayName != channel.Id),
                Is.True);
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

        private static void AssertWeight(
            IReadOnlyList<PandaFacialSemanticWeight> outputs,
            string semanticId,
            float expected)
        {
            Assert.That(
                outputs.Single(output => output.SemanticId == semanticId).Weight,
                Is.EqualTo(expected).Within(0.0001f));
        }

        private static Mesh CreateMesh(IEnumerable<string> names)
        {
            var result = new Mesh { name = "PandaFacialMouthUITestMesh" };
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
