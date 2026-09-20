using System;
using System.Collections.Generic;
using UnityEngine;

namespace SillBill.PandaFacial
{
    [Serializable]
    public sealed class PandaFacialMappingTarget
    {
        [SerializeField] private SkinnedMeshRenderer targetRenderer;
        [SerializeField] private string blendShapeName;
        [SerializeField] private float weightMultiplier = 1f;
        [SerializeField] private bool enabled = true;

        public SkinnedMeshRenderer TargetRenderer => targetRenderer;
        public string BlendShapeName => blendShapeName;
        public float WeightMultiplier => Mathf.Clamp01(weightMultiplier);
        public bool Enabled => enabled;
    }

    [Serializable]
    public sealed class PandaFacialSemanticMapping : ISerializationCallbackReceiver
    {
        private const int CurrentSchemaVersion = 2;

        // These original fields remain the serialized Primary Target for v0.1.7 compatibility.
        [SerializeField] private string semanticId;
        [SerializeField] private SkinnedMeshRenderer targetRenderer;
        [SerializeField] private string blendShapeName;
        [SerializeField] private int schemaVersion = CurrentSchemaVersion;
        [SerializeField] private float primaryWeightMultiplier = 1f;
        [SerializeField] private bool primaryEnabled = true;
        [SerializeField] private List<PandaFacialMappingTarget> additionalTargets =
            new List<PandaFacialMappingTarget>();

        public string SemanticId => semanticId;
        public SkinnedMeshRenderer TargetRenderer => targetRenderer;
        public string BlendShapeName => blendShapeName;
        public int SchemaVersion => schemaVersion;
        public float PrimaryWeightMultiplier => Mathf.Clamp01(primaryWeightMultiplier);
        public bool PrimaryEnabled => primaryEnabled;
        public IReadOnlyList<PandaFacialMappingTarget> AdditionalTargets => additionalTargets;

        public void OnBeforeSerialize()
        {
            primaryWeightMultiplier = Mathf.Clamp01(primaryWeightMultiplier);
            if (additionalTargets == null)
                additionalTargets = new List<PandaFacialMappingTarget>();
        }

        public void OnAfterDeserialize()
        {
            if (schemaVersion < CurrentSchemaVersion)
            {
                primaryWeightMultiplier = 1f;
                primaryEnabled = true;
                schemaVersion = CurrentSchemaVersion;
            }

            primaryWeightMultiplier = Mathf.Clamp01(primaryWeightMultiplier);
            if (additionalTargets == null)
                additionalTargets = new List<PandaFacialMappingTarget>();
        }
    }
}
