using System;
using System.Collections.Generic;

namespace SillBill.PandaFacial
{
    public enum PandaFacialSemanticGroup
    {
        LipSync,
        MouthPosition,
        MouthCorner,
        MouthWidth,
        EyeExpression,
        Brow
    }

    public readonly struct PandaFacialSemanticChannel
    {
        public PandaFacialSemanticChannel(string id, string displayName, PandaFacialSemanticGroup group)
        {
            Id = id;
            DisplayName = displayName;
            Group = group;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public PandaFacialSemanticGroup Group { get; }
    }

    public static class PandaFacialSemanticChannels
    {
        public const string MouthA = "mouth_a";
        public const string MouthI = "mouth_i";
        public const string MouthU = "mouth_u";
        public const string MouthE = "mouth_e";
        public const string MouthO = "mouth_o";

        public const string MouthLeft = "mouth_left";
        public const string MouthRight = "mouth_right";
        public const string MouthUp = "mouth_up";
        public const string MouthDown = "mouth_down";

        public const string MouthCornerUpL = "mouth_corner_up_l";
        public const string MouthCornerDownL = "mouth_corner_down_l";
        public const string MouthCornerUpR = "mouth_corner_up_r";
        public const string MouthCornerDownR = "mouth_corner_down_r";

        public const string MouthSpreadL = "mouth_spread_l";
        public const string MouthNarrowL = "mouth_narrow_l";
        public const string MouthSpreadR = "mouth_spread_r";
        public const string MouthNarrowR = "mouth_narrow_r";

        public const string EyeCloseL = "eye_close_l";
        public const string EyeCloseR = "eye_close_r";
        public const string EyeSmileL = "eye_smile_l";
        public const string EyeSmileR = "eye_smile_r";
        public const string EyeSurpriseL = "eye_surprise_l";
        public const string EyeSurpriseR = "eye_surprise_r";
        public const string EyeAngryL = "eye_angry_l";
        public const string EyeAngryR = "eye_angry_r";
        public const string EyeSadL = "eye_sad_l";
        public const string EyeSadR = "eye_sad_r";
        public const string EyeSquintL = "eye_squint_l";
        public const string EyeSquintR = "eye_squint_r";

        public const string BrowUpL = "brow_up_l";
        public const string BrowUpR = "brow_up_r";
        public const string BrowDownL = "brow_down_l";
        public const string BrowDownR = "brow_down_r";
        public const string BrowAngryL = "brow_angry_l";
        public const string BrowAngryR = "brow_angry_r";
        public const string BrowSadL = "brow_sad_l";
        public const string BrowSadR = "brow_sad_r";
        public const string BrowSmileL = "brow_smile_l";
        public const string BrowSmileR = "brow_smile_r";
        public const string BrowSeriousL = "brow_serious_l";
        public const string BrowSeriousR = "brow_serious_r";

        private static readonly PandaFacialSemanticChannel[] BuiltInChannels =
        {
            Channel(MouthA, "A", PandaFacialSemanticGroup.LipSync),
            Channel(MouthI, "I", PandaFacialSemanticGroup.LipSync),
            Channel(MouthU, "U", PandaFacialSemanticGroup.LipSync),
            Channel(MouthE, "E", PandaFacialSemanticGroup.LipSync),
            Channel(MouthO, "O", PandaFacialSemanticGroup.LipSync),

            Channel(MouthLeft, "Mouth Left", PandaFacialSemanticGroup.MouthPosition),
            Channel(MouthRight, "Mouth Right", PandaFacialSemanticGroup.MouthPosition),
            Channel(MouthUp, "Mouth Up", PandaFacialSemanticGroup.MouthPosition),
            Channel(MouthDown, "Mouth Down", PandaFacialSemanticGroup.MouthPosition),

            Channel(MouthCornerUpL, "Left Corner Up", PandaFacialSemanticGroup.MouthCorner),
            Channel(MouthCornerDownL, "Left Corner Down", PandaFacialSemanticGroup.MouthCorner),
            Channel(MouthCornerUpR, "Right Corner Up", PandaFacialSemanticGroup.MouthCorner),
            Channel(MouthCornerDownR, "Right Corner Down", PandaFacialSemanticGroup.MouthCorner),

            Channel(MouthSpreadL, "Left Spread", PandaFacialSemanticGroup.MouthWidth),
            Channel(MouthNarrowL, "Left Narrow", PandaFacialSemanticGroup.MouthWidth),
            Channel(MouthSpreadR, "Right Spread", PandaFacialSemanticGroup.MouthWidth),
            Channel(MouthNarrowR, "Right Narrow", PandaFacialSemanticGroup.MouthWidth),

            Channel(EyeCloseL, "Eyelid Close Left", PandaFacialSemanticGroup.EyeExpression),
            Channel(EyeCloseR, "Eyelid Close Right", PandaFacialSemanticGroup.EyeExpression),
            Channel(EyeSmileL, "Eye Smile Left", PandaFacialSemanticGroup.EyeExpression),
            Channel(EyeSmileR, "Eye Smile Right", PandaFacialSemanticGroup.EyeExpression),
            Channel(EyeSurpriseL, "Surprise Left", PandaFacialSemanticGroup.EyeExpression),
            Channel(EyeSurpriseR, "Surprise Right", PandaFacialSemanticGroup.EyeExpression),
            Channel(EyeAngryL, "Angry Left", PandaFacialSemanticGroup.EyeExpression),
            Channel(EyeAngryR, "Angry Right", PandaFacialSemanticGroup.EyeExpression),
            Channel(EyeSadL, "Sad Left", PandaFacialSemanticGroup.EyeExpression),
            Channel(EyeSadR, "Sad Right", PandaFacialSemanticGroup.EyeExpression),
            Channel(EyeSquintL, "Eyelid Jito Left", PandaFacialSemanticGroup.EyeExpression),
            Channel(EyeSquintR, "Eyelid Jito Right", PandaFacialSemanticGroup.EyeExpression),

            Channel(BrowUpL, "Brow Up Left", PandaFacialSemanticGroup.Brow),
            Channel(BrowUpR, "Brow Up Right", PandaFacialSemanticGroup.Brow),
            Channel(BrowDownL, "Brow Down Left", PandaFacialSemanticGroup.Brow),
            Channel(BrowDownR, "Brow Down Right", PandaFacialSemanticGroup.Brow),
            Channel(BrowAngryL, "Brow Angry Left", PandaFacialSemanticGroup.Brow),
            Channel(BrowAngryR, "Brow Angry Right", PandaFacialSemanticGroup.Brow),
            Channel(BrowSadL, "Brow Sad Left", PandaFacialSemanticGroup.Brow),
            Channel(BrowSadR, "Brow Sad Right", PandaFacialSemanticGroup.Brow),
            Channel(BrowSmileL, "Brow Smile Left", PandaFacialSemanticGroup.Brow),
            Channel(BrowSmileR, "Brow Smile Right", PandaFacialSemanticGroup.Brow),
            Channel(BrowSeriousL, "Brow Serious Left", PandaFacialSemanticGroup.Brow),
            Channel(BrowSeriousR, "Brow Serious Right", PandaFacialSemanticGroup.Brow)
        };

        private static readonly IReadOnlyList<PandaFacialSemanticChannel> ReadOnlyBuiltInChannels =
            Array.AsReadOnly(BuiltInChannels);

        public static IReadOnlyList<PandaFacialSemanticChannel> BuiltIn => ReadOnlyBuiltInChannels;

        public static bool TryGet(string id, out PandaFacialSemanticChannel channel)
        {
            for (int i = 0; i < BuiltInChannels.Length; i++)
            {
                if (string.Equals(BuiltInChannels[i].Id, id, StringComparison.Ordinal))
                {
                    channel = BuiltInChannels[i];
                    return true;
                }
            }

            channel = default;
            return false;
        }

        private static PandaFacialSemanticChannel Channel(
            string id,
            string displayName,
            PandaFacialSemanticGroup group)
        {
            return new PandaFacialSemanticChannel(id, displayName, group);
        }
    }
}
