using System;
using System.Collections.Generic;

namespace SillBill.PandaFacial
{
    internal static class PandaFacialSemanticMigration
    {
        internal const string LegacyEyeSquintL = "eye_squint_l";
        internal const string LegacyEyeSquintR = "eye_squint_r";

        internal static string MigrateId(string semanticId)
        {
            if (string.Equals(semanticId, LegacyEyeSquintL, StringComparison.Ordinal))
                return PandaFacialSemanticChannels.EyeJitoL;
            if (string.Equals(semanticId, LegacyEyeSquintR, StringComparison.Ordinal))
                return PandaFacialSemanticChannels.EyeJitoR;
            return semanticId;
        }

        internal static void MigrateMappings(List<PandaFacialSemanticMapping> mappings)
        {
            if (mappings == null || mappings.Count == 0)
                return;

            MigrateMappingsForId(mappings, LegacyEyeSquintL, PandaFacialSemanticChannels.EyeJitoL);
            MigrateMappingsForId(mappings, LegacyEyeSquintR, PandaFacialSemanticChannels.EyeJitoR);
        }

        private static void MigrateMappingsForId(
            List<PandaFacialSemanticMapping> mappings,
            string legacyId,
            string currentId)
        {
            PandaFacialSemanticMapping destination = null;
            int destinationIndex = -1;
            int earliestLegacyIndex = -1;
            for (int i = 0; i < mappings.Count; i++)
            {
                PandaFacialSemanticMapping mapping = mappings[i];
                if (mapping != null &&
                    string.Equals(mapping.SemanticId, currentId, StringComparison.Ordinal))
                {
                    destination = mapping;
                    destinationIndex = i;
                    break;
                }
            }

            for (int i = 0; i < mappings.Count; i++)
            {
                PandaFacialSemanticMapping mapping = mappings[i];
                if (mapping != null &&
                    string.Equals(mapping.SemanticId, legacyId, StringComparison.Ordinal))
                {
                    earliestLegacyIndex = i;
                    break;
                }
            }

            for (int i = 0; i < mappings.Count; i++)
            {
                PandaFacialSemanticMapping mapping = mappings[i];
                if (mapping == null ||
                    !string.Equals(mapping.SemanticId, legacyId, StringComparison.Ordinal))
                    continue;

                if (destination == null)
                {
                    mapping.SetSemanticId(currentId);
                    destination = mapping;
                    continue;
                }

                destination.MergeTargetsFrom(mapping);
                mappings.RemoveAt(i);
                i--;
            }

            if (destinationIndex >= 0 && earliestLegacyIndex >= 0 &&
                earliestLegacyIndex < destinationIndex)
            {
                int currentDestinationIndex = mappings.IndexOf(destination);
                if (currentDestinationIndex >= 0)
                {
                    mappings.RemoveAt(currentDestinationIndex);
                    mappings.Insert(Math.Min(earliestLegacyIndex, mappings.Count), destination);
                }
            }
        }
    }
}
