using System;
using System.Collections.Generic;

namespace SillBill.PandaFacial.Editor
{
    internal interface IPandaFacialDetectionDictionary
    {
        IReadOnlyList<string> GetAliases(string semanticId);
    }

    internal sealed class PandaFacialBuiltInDetectionDictionary : IPandaFacialDetectionDictionary
    {
        private readonly Dictionary<string, IReadOnlyList<string>> aliases =
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        internal static PandaFacialBuiltInDetectionDictionary Instance { get; } =
            new PandaFacialBuiltInDetectionDictionary();

        private PandaFacialBuiltInDetectionDictionary()
        {
            foreach (PandaFacialSemanticChannel channel in PandaFacialSemanticChannels.BuiltIn)
                aliases[channel.Id] = Array.AsReadOnly(new[] { channel.Id });

            Set(PandaFacialSemanticChannels.MouthA, "A", "Mouth_A", "Fcl_MTH_A");
            Set(PandaFacialSemanticChannels.MouthI, "I", "Mouth_I", "Fcl_MTH_I");
            Set(PandaFacialSemanticChannels.MouthU, "U", "Mouth_U", "Fcl_MTH_U");
            Set(PandaFacialSemanticChannels.MouthE, "E", "Mouth_E", "Fcl_MTH_E");
            Set(PandaFacialSemanticChannels.MouthO, "O", "Mouth_O", "Fcl_MTH_O");
            Set(PandaFacialSemanticChannels.MouthUp, "Mouth_Up", "Fcl_MTH_Up");
            Set(PandaFacialSemanticChannels.MouthDown, "Mouth_Down", "Fcl_MTH_Down");
            Set(PandaFacialSemanticChannels.EyeCloseL,
                "Eye_Close_L", "Blink_L", "Fcl_EYE_Close_L");
            Set(PandaFacialSemanticChannels.EyeCloseR,
                "Eye_Close_R", "Blink_R", "Fcl_EYE_Close_R");
        }

        public IReadOnlyList<string> GetAliases(string semanticId)
        {
            return semanticId != null && aliases.TryGetValue(semanticId, out IReadOnlyList<string> result)
                ? result
                : Array.Empty<string>();
        }

        private void Set(string semanticId, params string[] confirmedAliases)
        {
            var values = new List<string>(confirmedAliases.Length + 1) { semanticId };
            values.AddRange(confirmedAliases);
            aliases[semanticId] = values.AsReadOnly();
        }
    }
}
