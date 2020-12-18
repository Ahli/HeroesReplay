﻿using System;
using System.Collections.Generic;

namespace HeroesReplay.Core.Shared
{
    public record HeroesToolChestSettings
    {
        public string HeroesDataPath { get; init; }

        public Uri HeroesDataReleaseUri { get; init; }

        public IEnumerable<string> IgnoreUnits { get; init; }

        public IEnumerable<string> ObjectivesStartsWithUnits { get; init; }

        public IEnumerable<string> ObjectivesEndsWithUnits { get; init; }

        public IEnumerable<string> ObjectivesContains { get; init; }

        public IEnumerable<string> CaptureNames { get; init; }

        public string ScalingLinkId { get; init; }
    }
}