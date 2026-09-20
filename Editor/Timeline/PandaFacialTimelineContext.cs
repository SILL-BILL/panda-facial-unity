using UnityEditor.Timeline;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System;
using System.Collections.Generic;

namespace SillBill.PandaFacial.Editor
{
    internal readonly struct PandaFacialTimelineContext
    {
        internal PandaFacialTimelineContext(
            PlayableDirector director,
            AnimationClip animationClip,
            Transform animationRoot,
            TimelineClip timelineClip,
            double sequenceTime,
            double animationTime)
        {
            Director = director;
            AnimationClip = animationClip;
            AnimationRoot = animationRoot;
            TimelineClip = timelineClip;
            SequenceTime = sequenceTime;
            AnimationTime = animationTime;
        }

        internal PlayableDirector Director { get; }
        internal AnimationClip AnimationClip { get; }
        internal Transform AnimationRoot { get; }
        internal TimelineClip TimelineClip { get; }
        internal double SequenceTime { get; }
        internal double AnimationTime { get; }
    }

    internal static class PandaFacialTimelineContextProvider
    {
        internal static bool TryGetContext(
            SkinnedMeshRenderer targetRenderer,
            out PandaFacialTimelineContext context,
            out string error)
        {
            PlayableDirector director = TimelineEditor.inspectedDirector;
            if (director == null)
            {
                context = default;
                error = "Open a Timeline with a bound PlayableDirector.";
                return false;
            }

            TimelineClip[] selectedClips = TimelineEditor.selectedClips;
            if (selectedClips != null && selectedClips.Length > 1)
            {
                context = default;
                error = "Select exactly one Animation clip in the Timeline.";
                return false;
            }

            if (selectedClips != null && selectedClips.Length == 1)
                return TryCreateContext(director, selectedClips[0], director.time, out context, out error);

            return TryFindUnambiguousContext(
                director,
                TimelineEditor.inspectedAsset,
                targetRenderer,
                director.time,
                out context,
                out error);
        }

        internal static bool TryFindUnambiguousContext(
            PlayableDirector director,
            TimelineAsset timelineAsset,
            SkinnedMeshRenderer targetRenderer,
            double sequenceTime,
            out PandaFacialTimelineContext context,
            out string error)
        {
            context = default;
            if (timelineAsset == null || targetRenderer == null)
            {
                error = "Select one Animation clip in Timeline, or place the playhead over one clip bound to this renderer.";
                return false;
            }

            var candidates = new List<PandaFacialTimelineContext>();
            foreach (TrackAsset outputTrack in timelineAsset.GetOutputTracks())
            {
                if (!(outputTrack is AnimationTrack animationTrack))
                    continue;

                foreach (TimelineClip clip in animationTrack.GetClips())
                {
                    if (sequenceTime < clip.start || sequenceTime > clip.end)
                        continue;

                    if (!TryCreateContext(director, clip, sequenceTime, out PandaFacialTimelineContext candidate, out _))
                        continue;

                    if (targetRenderer.transform == candidate.AnimationRoot ||
                        targetRenderer.transform.IsChildOf(candidate.AnimationRoot))
                    {
                        candidates.Add(candidate);
                    }
                }

                if (!animationTrack.inClipMode && animationTrack.infiniteClip != null &&
                    TryCreateInfiniteContext(
                        director,
                        animationTrack,
                        sequenceTime,
                        out PandaFacialTimelineContext infiniteContext,
                        out _) &&
                    (targetRenderer.transform == infiniteContext.AnimationRoot ||
                     targetRenderer.transform.IsChildOf(infiniteContext.AnimationRoot)))
                {
                    candidates.Add(infiniteContext);
                }
            }

            if (candidates.Count == 1)
            {
                context = candidates[0];
                error = null;
                return true;
            }

            error = candidates.Count == 0
                ? "No writable Animation clip bound to this renderer exists at the current Timeline time."
                : "Multiple Animation clips bound to this renderer overlap the current Timeline time. Select one clip explicitly.";
            return false;
        }

        internal static bool TryCreateContext(
            PlayableDirector director,
            TimelineClip timelineClip,
            double sequenceTime,
            out PandaFacialTimelineContext context,
            out string error)
        {
            context = default;

            if (director == null)
            {
                error = "The Timeline has no PlayableDirector.";
                return false;
            }

            if (timelineClip == null)
            {
                error = "No Timeline clip is selected.";
                return false;
            }

            AnimationPlayableAsset animationPlayable = timelineClip.asset as AnimationPlayableAsset;
            AnimationClip animationClip = animationPlayable != null ? animationPlayable.clip : null;
            if (animationClip == null)
            {
                error = "The selected Timeline clip does not reference an AnimationClip.";
                return false;
            }

            if (!IsWritableAnimationClip(animationClip))
            {
                error = "The selected AnimationClip is imported or read-only. Select a writable .anim or Timeline Recorded clip.";
                return false;
            }

            if (sequenceTime < timelineClip.start || sequenceTime > timelineClip.end)
            {
                error = "Move the Timeline playhead inside the selected clip.";
                return false;
            }

            TrackAsset track = timelineClip.GetParentTrack();
            UnityEngine.Object binding = track != null ? director.GetGenericBinding(track) : null;
            Transform animationRoot = GetBindingTransform(binding);
            if (animationRoot == null)
            {
                error = "Bind the selected Animation Track to an Animator or GameObject.";
                return false;
            }

            double animationTime = timelineClip.ToLocalTime(sequenceTime);
            if (animationTime < 0d)
            {
                error = "The current Timeline time maps before the AnimationClip start.";
                return false;
            }

            context = new PandaFacialTimelineContext(
                director,
                animationClip,
                animationRoot,
                timelineClip,
                sequenceTime,
                animationTime);
            error = null;
            return true;
        }

        internal static bool TryCreateInfiniteContext(
            PlayableDirector director,
            AnimationTrack track,
            double sequenceTime,
            out PandaFacialTimelineContext context,
            out string error)
        {
            context = default;
            if (director == null)
            {
                error = "The Timeline has no PlayableDirector.";
                return false;
            }
            if (track == null || track.inClipMode || track.infiniteClip == null)
            {
                error = "The Animation Track has no Infinite Clip.";
                return false;
            }
            if (!IsWritableAnimationClip(track.infiniteClip))
            {
                error = "The Infinite Clip is imported or read-only.";
                return false;
            }

            Transform animationRoot = GetBindingTransform(director.GetGenericBinding(track));
            if (animationRoot == null)
            {
                error = "Bind the Animation Track to an Animator or GameObject.";
                return false;
            }

            double animationTime = sequenceTime - GetInfiniteClipTimeOffset(track);
            if (animationTime < 0d)
            {
                error = "The current Timeline time maps before the Infinite Clip start.";
                return false;
            }

            context = new PandaFacialTimelineContext(
                director,
                track.infiniteClip,
                animationRoot,
                null,
                sequenceTime,
                animationTime);
            error = null;
            return true;
        }

        internal static bool IsWritableAnimationClip(AnimationClip animationClip)
        {
            if (animationClip == null)
                return false;

            string clipPath = AssetDatabase.GetAssetPath(animationClip);
            if (string.IsNullOrEmpty(clipPath))
                return true;
            if (clipPath.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
                return true;

            return AssetDatabase.IsSubAsset(animationClip) &&
                   AssetDatabase.LoadMainAssetAtPath(clipPath) is TimelineAsset;
        }

        private static double GetInfiniteClipTimeOffset(AnimationTrack track)
        {
            var serializedTrack = new SerializedObject(track);
            SerializedProperty offset = serializedTrack.FindProperty("m_InfiniteClipTimeOffset");
            return offset != null ? offset.doubleValue : 0d;
        }

        private static Transform GetBindingTransform(UnityEngine.Object binding)
        {
            if (binding is Animator animator)
                return animator.transform;
            if (binding is GameObject gameObject)
                return gameObject.transform;
            if (binding is Component component)
                return component.transform;
            return null;
        }
    }
}
