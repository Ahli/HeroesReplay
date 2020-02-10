using System;
using System.Linq;
using HeroesReplay.Core.Analyzer;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HeroesReplay.Tests
{
    public class AnalyzerTests : IClassFixture<StormReplayFixture>
    {
        private readonly StormReplayAnalyzer analyzer;
        private readonly StormReplayFixture fixture;

        public AnalyzerTests(StormReplayFixture fixture)
        {
            this.fixture = fixture;
            analyzer = new StormReplayAnalyzer(new NullLogger<StormReplayAnalyzer>());
        }

        [Fact]
        public void ShouldHave10AliveHeroes()
        {
            AnalyzerResult analyzerResult = analyzer.Analyze(fixture.StormReplay.Replay, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1));

            Assert.Equal(10, analyzerResult.Alive.Count());
        }
    }
}
