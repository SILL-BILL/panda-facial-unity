using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SillBill.PandaFacial.Editor.Tests
{
    internal sealed class PandaFacialAuthoringTargetTests
    {
        [Test]
        public void AuthoringTarget_DoesNotSerializeFacialValue()
        {
            GameObject gameObject = new GameObject("Panda Facial Test");
            try
            {
                var target = gameObject.AddComponent<PandaFacialAuthoringTarget>();
                var serializedTarget = new SerializedObject(target);

                Assert.That(serializedTarget.FindProperty("defaultFaceRenderer"), Is.Not.Null);
                Assert.That(serializedTarget.FindProperty("targetRenderer"), Is.Not.Null);
                Assert.That(serializedTarget.FindProperty("blendShapeName"), Is.Not.Null);
                Assert.That(serializedTarget.FindProperty("selectedSemanticId"), Is.Not.Null);
                Assert.That(serializedTarget.FindProperty("semanticMappings"), Is.Not.Null);
                Assert.That(serializedTarget.FindProperty("authoringValue"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
