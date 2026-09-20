using System.Collections.Generic;
using UnityEngine;

namespace SillBill.PandaFacial
{
    /// <summary>
    /// Stores Editor authoring selections only. It does not evaluate facial animation at runtime.
    /// The component can be removed after authoring because keys are written to BlendShape curves.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Panda Facial/Authoring Target")]
    public sealed class PandaFacialAuthoringTarget : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer defaultFaceRenderer;
        [SerializeField] private SkinnedMeshRenderer targetRenderer;
        [SerializeField] private string blendShapeName;
        [SerializeField] private string selectedSemanticId = PandaFacialSemanticChannels.MouthA;
        [SerializeField] private List<PandaFacialSemanticMapping> semanticMappings =
            new List<PandaFacialSemanticMapping>();

        public SkinnedMeshRenderer DefaultFaceRenderer => defaultFaceRenderer;
        public SkinnedMeshRenderer TargetRenderer => targetRenderer;
        public string BlendShapeName => blendShapeName;
        public string SelectedSemanticId => selectedSemanticId;
        public IReadOnlyList<PandaFacialSemanticMapping> SemanticMappings => semanticMappings;
    }
}
