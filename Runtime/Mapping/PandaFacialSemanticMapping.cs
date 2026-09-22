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

        internal static PandaFacialMappingTarget Create(
            SkinnedMeshRenderer renderer,
            string name,
            float multiplier,
            bool isEnabled)
        {
            return new PandaFacialMappingTarget
            {
                targetRenderer = renderer,
                blendShapeName = name,
                weightMultiplier = multiplier,
                enabled = isEnabled
            };
        }

        internal PandaFacialMappingTarget Copy()
        {
            return new PandaFacialMappingTarget
            {
                targetRenderer = targetRenderer,
                blendShapeName = blendShapeName,
                weightMultiplier = weightMultiplier,
                enabled = enabled
            };
        }

        internal bool HasSameConfiguration(PandaFacialMappingTarget other)
        {
            return other != null &&
                   ReferenceEquals(targetRenderer, other.targetRenderer) &&
                   string.Equals(blendShapeName, other.blendShapeName, StringComparison.Ordinal) &&
                   weightMultiplier.Equals(other.weightMultiplier) &&
                   enabled == other.enabled;
        }
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
        public IReadOnlyList<PandaFacialMappingTarget> AdditionalTargets =>
            additionalTargets != null
                ? (IReadOnlyList<PandaFacialMappingTarget>)additionalTargets
                : Array.Empty<PandaFacialMappingTarget>();

        internal void SetSemanticId(string value)
        {
            semanticId = value;
        }

        internal void MergeTargetsFrom(PandaFacialSemanticMapping source)
        {
            if (source == null)
                return;

            AddTargetIfUnique(source.CreatePrimaryTarget());
            IReadOnlyList<PandaFacialMappingTarget> sourceAdditional = source.AdditionalTargets;
            for (int i = 0; i < sourceAdditional.Count; i++)
                AddTargetIfUnique(sourceAdditional[i]);
        }

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

        private PandaFacialMappingTarget CreatePrimaryTarget()
        {
            return PandaFacialMappingTarget.Create(
                targetRenderer,
                blendShapeName,
                primaryWeightMultiplier,
                primaryEnabled);
        }

        private void AddTargetIfUnique(PandaFacialMappingTarget candidate)
        {
            if (candidate == null || CreatePrimaryTarget().HasSameConfiguration(candidate))
                return;
            if (additionalTargets == null)
                additionalTargets = new List<PandaFacialMappingTarget>();
            for (int i = 0; i < additionalTargets.Count; i++)
            {
                if (additionalTargets[i] != null &&
                    additionalTargets[i].HasSameConfiguration(candidate))
                    return;
            }
            additionalTargets.Add(candidate.Copy());
        }

    }
}
