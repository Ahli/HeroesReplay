﻿
using System;
using System.Collections.Generic;

namespace HeroesReplay.Core.Shared
{
    public record HeroesProfileApiSettings
    {
        public Uri BaseUri { get; init; }
        public Uri OpenApiBaseUri { get; init; }
        public string ApiKey { get; init; }
        public string AwsAccessKey { get; init; }
        public string AwsSecretKey { get; init; }
        public IEnumerable<string> GameTypes { get; init; }
        public string S3Bucket { get; init; }
        public int MinReplayId { get; init; }
        public bool EnableMMR { get; init; }
    }
}