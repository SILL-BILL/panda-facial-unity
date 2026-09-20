using System;
using System.Collections.Generic;
using System.Text;

namespace SillBill.PandaFacial.Editor
{
    internal enum PandaFacialDetectionStatus
    {
        Detected,
        Unmapped,
        Ambiguous,
        SkippedExisting
    }

    internal enum PandaFacialDetectionMatchKind
    {
        None,
        Exact,
        CaseInsensitive,
        Normalized
    }

    internal sealed class PandaFacialDetectionEntry
    {
        internal PandaFacialDetectionEntry(
            string semanticId,
            PandaFacialDetectionStatus status,
            PandaFacialDetectionMatchKind matchKind,
            IReadOnlyList<string> candidates)
        {
            SemanticId = semanticId;
            Status = status;
            MatchKind = matchKind;
            Candidates = candidates;
        }

        internal string SemanticId { get; }
        internal PandaFacialDetectionStatus Status { get; }
        internal PandaFacialDetectionMatchKind MatchKind { get; }
        internal IReadOnlyList<string> Candidates { get; }
        internal string DetectedBlendShape =>
            Status == PandaFacialDetectionStatus.Detected && Candidates.Count == 1
                ? Candidates[0]
                : null;
    }

    internal sealed class PandaFacialDetectionReport
    {
        private readonly List<PandaFacialDetectionEntry> entries =
            new List<PandaFacialDetectionEntry>();

        internal IReadOnlyList<PandaFacialDetectionEntry> Entries => entries;
        internal string Error { get; set; }
        internal int MappedCount => Count(PandaFacialDetectionStatus.Detected);
        internal int UnmappedCount => Count(PandaFacialDetectionStatus.Unmapped);
        internal int AmbiguousCount => Count(PandaFacialDetectionStatus.Ambiguous);
        internal int SkippedExistingCount => Count(PandaFacialDetectionStatus.SkippedExisting);

        internal void Add(PandaFacialDetectionEntry entry)
        {
            entries.Add(entry);
        }

        private int Count(PandaFacialDetectionStatus status)
        {
            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Status == status)
                    count++;
            }
            return count;
        }
    }

    internal static class PandaFacialAutoDetector
    {
        internal static PandaFacialDetectionReport Detect(
            IReadOnlyList<PandaFacialSemanticChannel> channels,
            IReadOnlyList<string> blendShapeNames,
            IPandaFacialDetectionDictionary dictionary,
            ISet<string> existingSemanticIds = null)
        {
            var report = new PandaFacialDetectionReport();
            if (channels == null || blendShapeNames == null || dictionary == null)
                return report;

            for (int i = 0; i < channels.Count; i++)
            {
                PandaFacialSemanticChannel channel = channels[i];
                if (existingSemanticIds != null && existingSemanticIds.Contains(channel.Id))
                {
                    report.Add(Entry(channel.Id, PandaFacialDetectionStatus.SkippedExisting,
                        PandaFacialDetectionMatchKind.None, Array.Empty<string>()));
                    continue;
                }

                IReadOnlyList<string> aliases = dictionary.GetAliases(channel.Id);
                List<string> candidates = FindMatches(
                    blendShapeNames, aliases, PandaFacialDetectionMatchKind.Exact);
                PandaFacialDetectionMatchKind kind = PandaFacialDetectionMatchKind.Exact;
                if (candidates.Count == 0)
                {
                    kind = PandaFacialDetectionMatchKind.CaseInsensitive;
                    candidates = FindMatches(blendShapeNames, aliases, kind);
                }
                if (candidates.Count == 0)
                {
                    kind = PandaFacialDetectionMatchKind.Normalized;
                    candidates = FindMatches(blendShapeNames, aliases, kind);
                }

                if (candidates.Count == 1)
                {
                    report.Add(Entry(channel.Id, PandaFacialDetectionStatus.Detected,
                        kind, candidates.AsReadOnly()));
                }
                else if (candidates.Count > 1)
                {
                    report.Add(Entry(channel.Id, PandaFacialDetectionStatus.Ambiguous,
                        kind, candidates.AsReadOnly()));
                }
                else
                {
                    report.Add(Entry(channel.Id, PandaFacialDetectionStatus.Unmapped,
                        PandaFacialDetectionMatchKind.None, Array.Empty<string>()));
                }
            }

            return report;
        }

        internal static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            var builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character == '_' || character == '-' || char.IsWhiteSpace(character))
                    continue;
                builder.Append(char.ToLowerInvariant(character));
            }
            return builder.ToString();
        }

        private static List<string> FindMatches(
            IReadOnlyList<string> blendShapeNames,
            IReadOnlyList<string> aliases,
            PandaFacialDetectionMatchKind kind)
        {
            var matches = new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (int nameIndex = 0; nameIndex < blendShapeNames.Count; nameIndex++)
            {
                string name = blendShapeNames[nameIndex];
                for (int aliasIndex = 0; aliasIndex < aliases.Count; aliasIndex++)
                {
                    if (!Matches(name, aliases[aliasIndex], kind) || !unique.Add(name))
                        continue;
                    matches.Add(name);
                    break;
                }
            }
            return matches;
        }

        private static bool Matches(
            string blendShapeName,
            string alias,
            PandaFacialDetectionMatchKind kind)
        {
            switch (kind)
            {
                case PandaFacialDetectionMatchKind.Exact:
                    return string.Equals(blendShapeName, alias, StringComparison.Ordinal);
                case PandaFacialDetectionMatchKind.CaseInsensitive:
                    return string.Equals(blendShapeName, alias, StringComparison.OrdinalIgnoreCase);
                case PandaFacialDetectionMatchKind.Normalized:
                    return string.Equals(Normalize(blendShapeName), Normalize(alias),
                        StringComparison.Ordinal);
                default:
                    return false;
            }
        }

        private static PandaFacialDetectionEntry Entry(
            string semanticId,
            PandaFacialDetectionStatus status,
            PandaFacialDetectionMatchKind kind,
            IReadOnlyList<string> candidates)
        {
            return new PandaFacialDetectionEntry(semanticId, status, kind, candidates);
        }
    }
}
