﻿using Heroes.ReplayParser;
using Heroes.ReplayParser.MPQFiles;

using HeroesReplay.Core.Shared;

using System;

namespace HeroesReplay.Core
{
    public class AbilityDetector : IAbilityDetector
    {
        public bool IsAbility(Replay replay, GameEvent gameEvent, AbilityDetection abilityDetection)
        {
            int abilityLink = GetAbilityLink(gameEvent.data);

            if (abilityDetection.CmdIndex.HasValue)
            {
                if (abilityDetection.CmdIndex.Value != GetAbilityCmdIndex(gameEvent.data))
                {
                    return false;
                }
            }

            foreach (var abilityBuild in abilityDetection.AbilityBuilds)
            {
                if (abilityLink != abilityBuild.AbilityLink) return false;

                if (abilityBuild.GreaterEqualBuild.HasValue && abilityBuild.LessThanBuild.HasValue)
                {
                    if (abilityBuild.GreaterEqualBuild.Value >= replay.ReplayBuild && abilityBuild.LessThanBuild.Value < replay.ReplayBuild)
                    {
                        return true;
                    }
                }
                else if (abilityBuild.GreaterEqualBuild.HasValue && !abilityBuild.LessThanBuild.HasValue)
                {
                    if (abilityBuild.GreaterEqualBuild.Value >= replay.ReplayBuild)
                    {
                        return true;
                    }
                }
                else if (abilityBuild.LessThanBuild.HasValue && !abilityBuild.GreaterEqualBuild.HasValue)
                {
                    if (abilityBuild.LessThanBuild.Value < replay.ReplayBuild)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private int GetAbilityLink(TrackerEventStructure structure) => Convert.ToInt32(structure?.array[1]?.array[0]?.unsignedInt.GetValueOrDefault());

        private int GetAbilityCmdIndex(TrackerEventStructure trackerEvent) => Convert.ToInt32(trackerEvent.array[1]?.array[1]?.unsignedInt.GetValueOrDefault());
    }
}