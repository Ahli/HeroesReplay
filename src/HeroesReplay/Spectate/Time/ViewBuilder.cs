﻿using Heroes.ReplayParser;
using System;
using System.Diagnostics;

namespace HeroesReplay
{
    /// <summary>
    /// A utility class used to create a 'context' for the spectator at a given time. 
    /// </summary>
    /// <remarks>
    /// I don't see a reason that Minutes will need to be supported.
    /// </remarks>
    public class ViewBuilder
    {
        private readonly int seconds;
        private readonly Stopwatch stopwatch;
        private readonly Game game;

        public ViewBuilder(Stopwatch stopwatch, Game game)
        {
            this.stopwatch = stopwatch;
            this.game = game;
        }

        private ViewBuilder(Stopwatch stopwatch, Game game, int seconds)
        {
            this.seconds = seconds;
            this.game = game;
            this.stopwatch = stopwatch;
        }

        public ViewBuilder TheNext(int seconds) => new ViewBuilder(stopwatch, game, seconds);

        public ViewSpan Seconds => new ViewSpan(stopwatch, game, TimeSpan.FromSeconds(seconds));
    }
}
