using System.Collections.Generic;
using UnityEditor;

namespace SillBill.PandaFacial.Editor
{
    internal enum PandaFacialUpperFaceSection
    {
        EyeExpression,
        Brow
    }

    internal static class PandaFacialUpperFaceFoldoutState
    {
        private const string KeyPrefix = "SillBill.PandaFacial.UpperFaceSection.";

        private static readonly string[] EyeExpressionChannels =
        {
            PandaFacialSemanticChannels.EyeCloseL,
            PandaFacialSemanticChannels.EyeCloseR,
            PandaFacialSemanticChannels.EyeSmileL,
            PandaFacialSemanticChannels.EyeSmileR,
            PandaFacialSemanticChannels.EyeSurpriseL,
            PandaFacialSemanticChannels.EyeSurpriseR,
            PandaFacialSemanticChannels.EyeAngryL,
            PandaFacialSemanticChannels.EyeAngryR,
            PandaFacialSemanticChannels.EyeSadL,
            PandaFacialSemanticChannels.EyeSadR,
            PandaFacialSemanticChannels.EyeSquintL,
            PandaFacialSemanticChannels.EyeSquintR
        };

        private static readonly string[] BrowChannels =
        {
            PandaFacialSemanticChannels.BrowUpL,
            PandaFacialSemanticChannels.BrowUpR,
            PandaFacialSemanticChannels.BrowDownL,
            PandaFacialSemanticChannels.BrowDownR,
            PandaFacialSemanticChannels.BrowAngryL,
            PandaFacialSemanticChannels.BrowAngryR,
            PandaFacialSemanticChannels.BrowSadL,
            PandaFacialSemanticChannels.BrowSadR,
            PandaFacialSemanticChannels.BrowSmileL,
            PandaFacialSemanticChannels.BrowSmileR,
            PandaFacialSemanticChannels.BrowSeriousL,
            PandaFacialSemanticChannels.BrowSeriousR
        };

        internal static bool Get(
            PandaFacialAuthoringTarget target,
            PandaFacialUpperFaceSection section)
        {
            string key = GetKey(target, section);
            int stored = SessionState.GetInt(key, -1);
            if (stored >= 0)
                return stored == 1;

            bool initialValue = CountMapped(target, section) > 0;
            SessionState.SetInt(key, initialValue ? 1 : 0);
            return initialValue;
        }

        internal static void Set(
            PandaFacialAuthoringTarget target,
            PandaFacialUpperFaceSection section,
            bool isOpen)
        {
            SessionState.SetInt(GetKey(target, section), isOpen ? 1 : 0);
        }

        internal static int CountMapped(
            PandaFacialAuthoringTarget target,
            PandaFacialUpperFaceSection section)
        {
            IReadOnlyList<string> channels = GetChannels(section);
            int mapped = 0;
            for (int i = 0; i < channels.Count; i++)
            {
                if (PandaFacialMappingResolver.Resolve(target, channels[i]).IsMapped)
                    mapped++;
            }
            return mapped;
        }

        internal static int GetChannelCount(PandaFacialUpperFaceSection section)
        {
            return GetChannels(section).Count;
        }

        internal static IReadOnlyList<string> GetChannels(PandaFacialUpperFaceSection section)
        {
            return section == PandaFacialUpperFaceSection.Brow
                ? BrowChannels
                : EyeExpressionChannels;
        }

        internal static void Clear(PandaFacialAuthoringTarget target)
        {
            foreach (PandaFacialUpperFaceSection section in
                     System.Enum.GetValues(typeof(PandaFacialUpperFaceSection)))
            {
                SessionState.EraseInt(GetKey(target, section));
            }
        }

        private static string GetKey(
            PandaFacialAuthoringTarget target,
            PandaFacialUpperFaceSection section)
        {
            int instanceId = target != null ? target.GetInstanceID() : 0;
            return KeyPrefix + instanceId + "." + section;
        }
    }
}
