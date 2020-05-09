﻿using System;
using System.Threading.Tasks;
using Heroes.ReplayParser;
using HeroesReplay.Core.Processes;
using HeroesReplay.Core.Shared;

namespace HeroesReplay.Core.Spectator
{
    public class GameStateTool
    {
        private readonly HeroesOfTheStorm heroesOfTheStorm;

        public GameStateTool(HeroesOfTheStorm heroesOfTheStorm)
        {
            this.heroesOfTheStorm = heroesOfTheStorm;
        }

        public async Task<StormState> GetStateAsync(Replay replay, StormState currentState)
        {
            TimeSpan? elapsed = await heroesOfTheStorm.TryGetTimerAsync();

            if (elapsed != null && elapsed != TimeSpan.Zero)
            {
                TimeSpan next = elapsed.Value.RemoveNegativeOffset();

                if (next <= TimeSpan.Zero) return new StormState(next, GameState.StartOfGame);
                if (next > currentState.Timer) return new StormState(next, GameState.Running);
                if (next < currentState.Timer) return new StormState(next, GameState.Paused);
                return new StormState(next, currentState.State);
            }

            if (currentState.IsNearEnd(replay.ReplayLength) && await heroesOfTheStorm.TryGetMatchAwardsAsync(replay.GetMatchAwards()))
            {
                return new StormState(currentState.Timer, GameState.EndOfGame);
            }

            return currentState;
        }
    }
}