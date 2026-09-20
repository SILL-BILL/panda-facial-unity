using System;
using UnityEngine;

namespace SillBill.PandaFacial
{
    [Serializable]
    public sealed class PandaFacialSemanticMapping
    {
        [SerializeField] private string semanticId;
        [SerializeField] private SkinnedMeshRenderer targetRenderer;
        [SerializeField] private string blendShapeName;

        public string SemanticId => semanticId;
        public SkinnedMeshRenderer TargetRenderer => targetRenderer;
        public string BlendShapeName => blendShapeName;
    }
}
