﻿namespace HeroesReplay.Core.Shared
{
    public record FeatureToggleSettings
    {
        public bool EnableTwitchClips { get; init; }
        public bool EnableMMR { get; init; }
        public bool ForceLaunch { get; init; }
        public bool SaveCaptureFailureCondition { get; init; }
        public bool UseAppDataCache { get; init; }
        public bool HideChat { get; init; }
    }
}