using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SillBill.PandaFacial.Editor.Tests
{
    internal sealed class PandaFacialTimelineContextTests
    {
        [Test]
        public void CreateContext_UsesTrackBindingAndClipLocalTime()
        {
            GameObject root = new GameObject("CharacterRoot");
            TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            AnimationClip animationClip = new AnimationClip();

            try
            {
                animationClip.SetCurve(
                    string.Empty,
                    typeof(Transform),
                    "m_LocalPosition.x",
                    AnimationCurve.Linear(0f, 0f, 4f, 0f));
                Animator animator = root.AddComponent<Animator>();
                PlayableDirector director = root.AddComponent<PlayableDirector>();
                AnimationTrack track = timeline.CreateTrack<AnimationTrack>(null, "Facial Animation");
                TimelineClip timelineClip = track.CreateClip<AnimationPlayableAsset>();
                ((AnimationPlayableAsset)timelineClip.asset).clip = animationClip;
                timelineClip.start = 2d;
                timelineClip.duration = 2d;
                timelineClip.timeScale = 2d;
                timelineClip.clipIn = 0.25d;

                director.playableAsset = timeline;
                director.SetGenericBinding(track, animator);

                bool success = PandaFacialTimelineContextProvider.TryCreateContext(
                    director,
                    timelineClip,
                    2.5d,
                    out PandaFacialTimelineContext context,
                    out string error);

                Assert.That(success, Is.True, error);
                Assert.That(context.AnimationClip, Is.SameAs(animationClip));
                Assert.That(context.AnimationRoot, Is.SameAs(root.transform));
                Assert.That(context.SequenceTime, Is.EqualTo(2.5d));
                Assert.That(context.AnimationTime, Is.EqualTo(1.25d).Within(0.000001d));
            }
            finally
            {
                Object.DestroyImmediate(animationClip);
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FindContext_UsesOnlyClipAtPlayheadBoundToRenderer()
        {
            GameObject root = new GameObject("CharacterRoot");
            GameObject face = new GameObject("Face");
            face.transform.SetParent(root.transform);
            TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            AnimationClip animationClip = new AnimationClip();

            try
            {
                Animator animator = root.AddComponent<Animator>();
                SkinnedMeshRenderer renderer = face.AddComponent<SkinnedMeshRenderer>();
                PlayableDirector director = root.AddComponent<PlayableDirector>();
                AnimationTrack track = timeline.CreateTrack<AnimationTrack>(null, "Facial Animation");
                TimelineClip timelineClip = track.CreateClip<AnimationPlayableAsset>();
                ((AnimationPlayableAsset)timelineClip.asset).clip = animationClip;
                timelineClip.start = 1d;
                timelineClip.duration = 2d;
                director.playableAsset = timeline;
                director.SetGenericBinding(track, animator);

                bool success = PandaFacialTimelineContextProvider.TryFindUnambiguousContext(
                    director,
                    timeline,
                    renderer,
                    1.5d,
                    out PandaFacialTimelineContext context,
                    out string error);

                Assert.That(success, Is.True, error);
                Assert.That(context.TimelineClip, Is.SameAs(timelineClip));
                Assert.That(context.AnimationTime, Is.EqualTo(0.5d).Within(0.000001d));
            }
            finally
            {
                Object.DestroyImmediate(animationClip);
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FindContext_RecognizesInfiniteClipAndAppliesItsTimeOffset()
        {
            GameObject root = new GameObject("CharacterRoot");
            GameObject face = new GameObject("Face");
            face.transform.SetParent(root.transform);
            TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();

            try
            {
                Animator animator = root.AddComponent<Animator>();
                SkinnedMeshRenderer renderer = face.AddComponent<SkinnedMeshRenderer>();
                PlayableDirector director = root.AddComponent<PlayableDirector>();
                AnimationTrack track = timeline.CreateTrack<AnimationTrack>(null, "Facial Animation");
                track.CreateInfiniteClip("Recorded");

                var serializedTrack = new SerializedObject(track);
                serializedTrack.FindProperty("m_InfiniteClipTimeOffset").doubleValue = 0.25d;
                serializedTrack.ApplyModifiedPropertiesWithoutUndo();

                director.playableAsset = timeline;
                director.SetGenericBinding(track, animator);

                bool success = PandaFacialTimelineContextProvider.TryFindUnambiguousContext(
                    director,
                    timeline,
                    renderer,
                    1.25d,
                    out PandaFacialTimelineContext context,
                    out string error);

                Assert.That(success, Is.True, error);
                Assert.That(context.TimelineClip, Is.Null);
                Assert.That(context.AnimationClip, Is.SameAs(track.infiniteClip));
                Assert.That(context.AnimationTime, Is.EqualTo(1d).Within(0.000001d));
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void WritableClip_AcceptsTimelineEmbeddedRecordedClip()
        {
            string path = "Assets/__PandaFacialRecordedClipTest_" +
                          System.Guid.NewGuid().ToString("N") + ".playable";
            TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            GameObject root = new GameObject("CharacterRoot");
            GameObject face = new GameObject("Face");
            face.transform.SetParent(root.transform);

            try
            {
                AssetDatabase.CreateAsset(timeline, path);
                AnimationTrack track = timeline.CreateTrack<AnimationTrack>(null, "Facial Animation");
                TimelineClip recorded = track.CreateRecordableClip("Recorded");
                AnimationClip animationClip = ((AnimationPlayableAsset)recorded.asset).clip;
                AssetDatabase.SaveAssets();

                Animator animator = root.AddComponent<Animator>();
                SkinnedMeshRenderer renderer = face.AddComponent<SkinnedMeshRenderer>();
                PlayableDirector director = root.AddComponent<PlayableDirector>();
                director.playableAsset = timeline;
                director.SetGenericBinding(track, animator);

                Assert.That(AssetDatabase.IsSubAsset(animationClip), Is.True);
                Assert.That(
                    PandaFacialTimelineContextProvider.IsWritableAnimationClip(animationClip),
                    Is.True);

                bool success = PandaFacialTimelineContextProvider.TryFindUnambiguousContext(
                    director,
                    timeline,
                    renderer,
                    0.5d,
                    out PandaFacialTimelineContext context,
                    out string error);
                Assert.That(success, Is.True, error);
                Assert.That(context.AnimationClip, Is.SameAs(animationClip));
                Assert.That(context.TimelineClip, Is.SameAs(recorded));
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
                Object.DestroyImmediate(root);
            }
        }
    }
}
