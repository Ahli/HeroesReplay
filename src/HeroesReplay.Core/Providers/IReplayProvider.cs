﻿using HeroesReplay.Core.Shared;

using System.Threading.Tasks;

namespace HeroesReplay.Core.Providers
{
    public interface IReplayProvider
    {
        Task<StormReplay?> TryLoadReplayAsync();
    }
}