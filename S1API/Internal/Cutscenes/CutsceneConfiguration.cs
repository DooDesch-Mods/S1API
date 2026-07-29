using System;
using UnityEngine;

namespace S1API.Internal.Cutscenes
{
    internal sealed class CutsceneConfiguration
    {
        internal string Id { get; }
        internal string Name { get; }
        internal float Duration { get; }
        internal float? Fov { get; }
        internal Vector3 InitialPosition { get; }
        internal Quaternion InitialRotation { get; }
        internal Action<global::S1API.Cutscenes.CutsceneFrame>? CameraUpdate { get; }
        internal Action? Started { get; }
        internal Action<global::S1API.Cutscenes.CutsceneEndReason>? Ended { get; }
        internal float FadeInDuration { get; }
        internal float FadeInDelay { get; }
        internal float FadeOutDuration { get; }
        internal float HoldToSkipDuration { get; }
        internal string? TitleCardText { get; }
        internal float TitleCardDuration { get; }

        internal CutsceneConfiguration(
            string id,
            string name,
            float duration,
            float? fov,
            Vector3 initialPosition,
            Quaternion initialRotation,
            Action<global::S1API.Cutscenes.CutsceneFrame>? cameraUpdate,
            Action? started,
            Action<global::S1API.Cutscenes.CutsceneEndReason>? ended,
            float fadeInDuration,
            float fadeInDelay,
            float fadeOutDuration,
            float holdToSkipDuration,
            string? titleCardText,
            float titleCardDuration)
        {
            Id = id;
            Name = name;
            Duration = duration;
            Fov = fov;
            InitialPosition = initialPosition;
            InitialRotation = initialRotation;
            CameraUpdate = cameraUpdate;
            Started = started;
            Ended = ended;
            FadeInDuration = fadeInDuration;
            FadeInDelay = fadeInDelay;
            FadeOutDuration = fadeOutDuration;
            HoldToSkipDuration = holdToSkipDuration;
            TitleCardText = titleCardText;
            TitleCardDuration = titleCardDuration;
        }
    }
}
