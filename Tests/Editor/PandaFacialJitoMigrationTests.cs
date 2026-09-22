using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SillBill.PandaFacial.Editor.Tests
{
    internal sealed class PandaFacialJitoMigrationTests
    {
        private const string LegacyEyeSquintL = "eye_squint_l";
        private const string LegacyEyeSquintR = "eye_squint_r";

        private GameObject root;
        private GameObject face;
        private GameObject overrideFace;
        private PandaFacialAuthoringTarget authoringTarget;
        private SkinnedMeshRenderer renderer;
        private SkinnedMeshRenderer overrideRenderer;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("CharacterRoot");
            face = new GameObject("Face");
            face.transform.SetParent(root.transform);
            overrideFace = new GameObject("OverrideFace");
            overrideFace.transform.SetParent(root.transform);
            authoringTarget = root.AddComponent<PandaFacialAuthoringTarget>();
            renderer = face.AddComponent<SkinnedMeshRenderer>();
            overrideRenderer = overrideFace.AddComponent<SkinnedMeshRenderer>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [TestCase(LegacyEyeSquintL,
            PandaFacialSemanticChannels.EyeJitoL)]
        [TestCase(LegacyEyeSquintR,
            PandaFacialSemanticChannels.EyeJitoR)]
        public void LegacySingleTarget_MigratesSemanticIdInPlace(
            string legacyId,
            string currentId)
        {
            PandaFacialSemanticMappingTests.AddMapping(
                authoringTarget, PandaFacialSemanticChannels.MouthA, renderer, "MouthA");
            PandaFacialSemanticMappingTests.AddMapping(
                authoringTarget, legacyId, overrideRenderer, "LegacyJito");

            authoringTarget.OnAfterDeserialize();

            Assert.That(authoringTarget.SemanticMappings.Select(mapping => mapping.SemanticId),
                Is.EqualTo(new[] { PandaFacialSemanticChannels.MouthA, currentId }));
            PandaFacialSemanticMapping migrated = authoringTarget.SemanticMappings[1];
            Assert.That(migrated.TargetRenderer, Is.SameAs(overrideRenderer));
            Assert.That(migrated.BlendShapeName, Is.EqualTo("LegacyJito"));
        }

        [Test]
        public void LegacyMultiTarget_MigrationPreservesEveryTargetSetting()
        {
            PandaFacialSemanticMappingTests.AddMapping(
                authoringTarget,
                LegacyEyeSquintL,
                overrideRenderer,
                "PrimaryJito");
            SerializedProperty mapping = MappingAt(0);
            mapping.FindPropertyRelative("primaryWeightMultiplier").floatValue = 0.65f;
            mapping.FindPropertyRelative("primaryEnabled").boolValue = false;
            AddAdditional(mapping, renderer, "CheekJito", 0.3f, true);
            mapping.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            authoringTarget.OnAfterDeserialize();

            PandaFacialSemanticMapping migrated = authoringTarget.SemanticMappings.Single();
            Assert.That(migrated.SemanticId, Is.EqualTo(PandaFacialSemanticChannels.EyeJitoL));
            Assert.That(migrated.TargetRenderer, Is.SameAs(overrideRenderer));
            Assert.That(migrated.BlendShapeName, Is.EqualTo("PrimaryJito"));
            Assert.That(migrated.PrimaryWeightMultiplier, Is.EqualTo(0.65f).Within(0.0001f));
            Assert.That(migrated.PrimaryEnabled, Is.False);
            Assert.That(migrated.AdditionalTargets, Has.Count.EqualTo(1));
            PandaFacialMappingTarget additional = migrated.AdditionalTargets[0];
            Assert.That(additional.TargetRenderer, Is.SameAs(renderer));
            Assert.That(additional.BlendShapeName, Is.EqualTo("CheekJito"));
            Assert.That(additional.WeightMultiplier, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(additional.Enabled, Is.True);
        }

        [Test]
        public void DuplicateLegacyAndCurrentMappings_PreferCurrentAndPreserveUniqueTargets()
        {
            PandaFacialSemanticMappingTests.AddMapping(
                authoringTarget,
                LegacyEyeSquintL,
                renderer,
                "DuplicateTarget");
            SerializedProperty legacy = MappingAt(0);
            legacy.FindPropertyRelative("primaryWeightMultiplier").floatValue = 0.4f;
            legacy.FindPropertyRelative("primaryEnabled").boolValue = false;
            AddAdditional(legacy, overrideRenderer, "UniqueLegacyTarget", 0.25f, true);
            legacy.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            PandaFacialSemanticMappingTests.AddMapping(
                authoringTarget,
                PandaFacialSemanticChannels.MouthA,
                renderer,
                "MouthA");
            PandaFacialSemanticMappingTests.AddMapping(
                authoringTarget,
                PandaFacialSemanticChannels.EyeJitoL,
                overrideRenderer,
                "CurrentPrimary");
            SerializedProperty current = MappingAt(2);
            AddAdditional(current, renderer, "DuplicateTarget", 0.4f, false);
            current.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            // SerializedObject application may invoke the component callback between setup steps.
            // Restore the legacy ID so this call models one payload containing both IDs.
            typeof(PandaFacialSemanticMapping)
                .GetField("semanticId", System.Reflection.BindingFlags.Instance |
                                        System.Reflection.BindingFlags.NonPublic)
                .SetValue(authoringTarget.SemanticMappings[0], LegacyEyeSquintL);

            authoringTarget.OnAfterDeserialize();

            Assert.That(authoringTarget.SemanticMappings, Has.Count.EqualTo(2));
            Assert.That(authoringTarget.SemanticMappings[1].SemanticId,
                Is.EqualTo(PandaFacialSemanticChannels.MouthA));
            PandaFacialSemanticMapping migrated = authoringTarget.SemanticMappings[0];
            Assert.That(migrated.SemanticId, Is.EqualTo(PandaFacialSemanticChannels.EyeJitoL));
            Assert.That(migrated.BlendShapeName, Is.EqualTo("CurrentPrimary"));
            Assert.That(migrated.AdditionalTargets, Has.Count.EqualTo(2));
            Assert.That(migrated.AdditionalTargets.Count(target =>
                target.TargetRenderer == renderer &&
                target.BlendShapeName == "DuplicateTarget" &&
                Mathf.Approximately(target.WeightMultiplier, 0.4f) &&
                !target.Enabled), Is.EqualTo(1));
            Assert.That(migrated.AdditionalTargets.Any(target =>
                target.TargetRenderer == overrideRenderer &&
                target.BlendShapeName == "UniqueLegacyTarget" &&
                Mathf.Approximately(target.WeightMultiplier, 0.25f) &&
                target.Enabled), Is.True);
        }

        [Test]
        public void RuntimeContractAndControllerOutputs_UseOnlyJitoIds()
        {
            string[] ids = PandaFacialSemanticChannels.BuiltIn.Select(channel => channel.Id).ToArray();
            IReadOnlyList<PandaFacialSemanticWeight> outputs =
                PandaFacialControllerLogic.EyeExpressions(
                    0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.25f, 0.75f);

            Assert.That(ids, Does.Contain(PandaFacialSemanticChannels.EyeJitoL));
            Assert.That(ids, Does.Contain(PandaFacialSemanticChannels.EyeJitoR));
            Assert.That(ids, Does.Not.Contain(LegacyEyeSquintL));
            Assert.That(ids, Does.Not.Contain(LegacyEyeSquintR));
            Assert.That(outputs.Any(output => output.SemanticId == PandaFacialSemanticChannels.EyeJitoL),
                Is.True);
            Assert.That(outputs.Any(output => output.SemanticId == PandaFacialSemanticChannels.EyeJitoR),
                Is.True);
        }

        [Test]
        public void SelectedLegacySemanticId_IsMigrated()
        {
            var serialized = new SerializedObject(authoringTarget);
            serialized.FindProperty("selectedSemanticId").stringValue =
                LegacyEyeSquintR;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            authoringTarget.OnAfterDeserialize();

            Assert.That(authoringTarget.SelectedSemanticId,
                Is.EqualTo(PandaFacialSemanticChannels.EyeJitoR));
        }

        private SerializedProperty MappingAt(int index)
        {
            var serialized = new SerializedObject(authoringTarget);
            return serialized.FindProperty("semanticMappings").GetArrayElementAtIndex(index);
        }

        private static void AddAdditional(
            SerializedProperty mapping,
            SkinnedMeshRenderer targetRenderer,
            string blendShapeName,
            float multiplier,
            bool enabled)
        {
            SerializedProperty targets = mapping.FindPropertyRelative("additionalTargets");
            int index = targets.arraySize;
            targets.InsertArrayElementAtIndex(index);
            SerializedProperty target = targets.GetArrayElementAtIndex(index);
            target.FindPropertyRelative("targetRenderer").objectReferenceValue = targetRenderer;
            target.FindPropertyRelative("blendShapeName").stringValue = blendShapeName;
            target.FindPropertyRelative("weightMultiplier").floatValue = multiplier;
            target.FindPropertyRelative("enabled").boolValue = enabled;
        }
    }

    internal sealed class PandaFacialEyelidAliasTests
    {
        private const string LegacyEyeSquintL = "eye_squint_l";
        private const string LegacyEyeSquintR = "eye_squint_r";

        private static readonly object[][] ConfirmedAliases =
        {
            new object[] { PandaFacialSemanticChannels.EyeCloseL, "Eyelid_Close_L" },
            new object[] { PandaFacialSemanticChannels.EyeCloseR, "Eyelid_Close_R" },
            new object[] { PandaFacialSemanticChannels.EyeSmileL, "Eyelid_Smile_L" },
            new object[] { PandaFacialSemanticChannels.EyeSmileR, "Eyelid_Smile_R" },
            new object[] { PandaFacialSemanticChannels.EyeSurpriseL, "Eyelid_Surprise_L" },
            new object[] { PandaFacialSemanticChannels.EyeSurpriseR, "Eyelid_Surprise_R" },
            new object[] { PandaFacialSemanticChannels.EyeAngryL, "Eyelid_Angry_L" },
            new object[] { PandaFacialSemanticChannels.EyeAngryR, "Eyelid_Angry_R" },
            new object[] { PandaFacialSemanticChannels.EyeSadL, "Eyelid_Sad_L" },
            new object[] { PandaFacialSemanticChannels.EyeSadR, "Eyelid_Sad_R" },
            new object[] { PandaFacialSemanticChannels.EyeJitoL, "Eyelid_Jito_L" },
            new object[] { PandaFacialSemanticChannels.EyeJitoR, "Eyelid_Jito_R" }
        };

        [TestCaseSource(nameof(ConfirmedAliases))]
        public void ConfirmedEyelidAliasFixture_MapsToExpectedSemantic(
            string semanticId,
            string blendShapeName)
        {
            PandaFacialSemanticChannel channel = PandaFacialSemanticChannels.BuiltIn
                .Single(candidate => candidate.Id == semanticId);
            PandaFacialDetectionEntry entry = PandaFacialAutoDetector.Detect(
                    new[] { channel },
                    new[] { blendShapeName },
                    PandaFacialBuiltInDetectionDictionary.Instance)
                .Entries.Single();

            Assert.That(entry.Status, Is.EqualTo(PandaFacialDetectionStatus.Detected));
            Assert.That(entry.DetectedBlendShape, Is.EqualTo(blendShapeName));
        }

        [Test]
        public void FullConfirmedEyelidAliasFixture_AutoDetectsAllTwelveNewSemanticMappings()
        {
            var root = new GameObject("CharacterRoot");
            var face = new GameObject("Face");
            face.transform.SetParent(root.transform);
            var target = root.AddComponent<PandaFacialAuthoringTarget>();
            var renderer = face.AddComponent<SkinnedMeshRenderer>();
            var mesh = new Mesh { name = "ConfirmedEyelidAliasFixture" };
            mesh.vertices = new[] { Vector3.zero };
            foreach (object[] alias in ConfirmedAliases)
            {
                mesh.AddBlendShapeFrame((string)alias[1], 100f, new[] { Vector3.right },
                    new[] { Vector3.zero }, new[] { Vector3.zero });
            }
            renderer.sharedMesh = mesh;
            PandaFacialSemanticMappingTests.SetDefaultRenderer(target, renderer);

            try
            {
                PandaFacialDetectionReport report = PandaFacialAutoDetectUtility.DetectAndApply(target);
                string[] expectedIds = ConfirmedAliases.Select(alias => (string)alias[0]).ToArray();

                Assert.That(report.MappedCount, Is.EqualTo(12));
                Assert.That(target.SemanticMappings.Select(mapping => mapping.SemanticId),
                    Is.EquivalentTo(expectedIds));
                Assert.That(target.SemanticMappings.All(mapping =>
                    mapping.SemanticId != LegacyEyeSquintL &&
                    mapping.SemanticId != LegacyEyeSquintR), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(mesh);
            }
        }

    }
}
