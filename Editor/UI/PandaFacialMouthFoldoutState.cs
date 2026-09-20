using System.Collections.Generic;
using UnityEditor;

namespace SillBill.PandaFacial.Editor
{
    internal enum PandaFacialMouthSection
    {
        Position,
        Corners,
        Vowels
    }

    internal static class PandaFacialMouthFoldoutState
    {
        private const string KeyPrefix = "SillBill.PandaFacial.MouthSection.";

        private static readonly string[] PositionChannels =
        {
            PandaFacialSemanticChannels.MouthLeft,
            PandaFacialSemanticChannels.MouthRight,
            PandaFacialSemanticChannels.MouthDown,
            PandaFacialSemanticChannels.MouthUp
        };

        private static readonly string[] CornerChannels =
        {
            PandaFacialSemanticChannels.MouthNarrowL,
            PandaFacialSemanticChannels.MouthSpreadL,
            PandaFacialSemanticChannels.MouthCornerDownL,
            PandaFacialSemanticChannels.MouthCornerUpL,
            PandaFacialSemanticChannels.MouthSpreadR,
            PandaFacialSemanticChannels.MouthNarrowR,
            PandaFacialSemanticChannels.MouthCornerDownR,
            PandaFacialSemanticChannels.MouthCornerUpR
        };

        private static readonly string[] VowelChannels =
        {
            PandaFacialSemanticChannels.MouthA,
            PandaFacialSemanticChannels.MouthI,
            PandaFacialSemanticChannels.MouthU,
            PandaFacialSemanticChannels.MouthE,
            PandaFacialSemanticChannels.MouthO
        };

        internal static bool Get(PandaFacialAuthoringTarget target, PandaFacialMouthSection section)
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
            PandaFacialMouthSection section,
            bool isOpen)
        {
            SessionState.SetInt(GetKey(target, section), isOpen ? 1 : 0);
        }

        internal static int CountMapped(
            PandaFacialAuthoringTarget target,
            PandaFacialMouthSection section)
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

        internal static int GetChannelCount(PandaFacialMouthSection section)
        {
            return GetChannels(section).Count;
        }

        internal static void Clear(PandaFacialAuthoringTarget target)
        {
            foreach (PandaFacialMouthSection section in
                     System.Enum.GetValues(typeof(PandaFacialMouthSection)))
            {
                SessionState.EraseInt(GetKey(target, section));
            }
        }

        private static IReadOnlyList<string> GetChannels(PandaFacialMouthSection section)
        {
            switch (section)
            {
                case PandaFacialMouthSection.Position:
                    return PositionChannels;
                case PandaFacialMouthSection.Corners:
                    return CornerChannels;
                case PandaFacialMouthSection.Vowels:
                    return VowelChannels;
                default:
                    return PositionChannels;
            }
        }

        private static string GetKey(
            PandaFacialAuthoringTarget target,
            PandaFacialMouthSection section)
        {
            int instanceId = target != null ? target.GetInstanceID() : 0;
            return KeyPrefix + instanceId + "." + section;
        }
    }
}
