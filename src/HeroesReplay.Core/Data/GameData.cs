﻿using HeroesReplay.Core.Services.HeroesProfile;
using HeroesReplay.Core.Shared;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using static Heroes.ReplayParser.Unit;

namespace HeroesReplay.Core.Runner
{
    public class GameData : IGameData
    {
        private const string ScalingLinkIdProperty = "scalingLinkId";
        private const string HeroUnitsProperty = "heroUnits";
        private const string UnitIdProperty = "unitId";
        private const string DescriptorsProperty = "descriptors";
        private const string AttributesProperty = "attributes";

        private const string HeroData = "herodata_";
        private const string UnitData = "unitdata_";

        private const string AttributeMapBoss = "MapBoss";
        private const string AttributeMapCreature = "MapCreature";
        private const string AttributeMerc = "Merc";
        private const string AttributeStructure = "AITargetableStructure";

        private const string UnitNameLaner = "Laner";
        private const string UnitNameDefender = "Defender";
        private const string UnitNamePayload = "Payload";

        private const string HeroicTalent = "Talent";

        private readonly ILogger<GameData> logger;
        private readonly Settings settings;
        private readonly string heroesDataPath;

        public IReadOnlyDictionary<string, UnitGroup> UnitGroups { get; private set; }
        public IReadOnlyList<Map> Maps { get; private set; }
        public IReadOnlyList<Hero> Heroes { get; private set; }
        public IReadOnlyCollection<string> CoreUnits { get; private set; }
        public IReadOnlyCollection<string> BossUnits { get; private set; }

        public GameData(ILogger<GameData> logger, Settings settings)
        {
            this.logger = logger;
            this.settings = settings;
            this.heroesDataPath = settings.HeroesDataPath;
        }

        private async Task LoadHeroesAsync()
        {
            var json = await File.ReadAllTextAsync(Path.Combine(settings.AssetsPath, "Heroes.json"));

            using (JsonDocument heroJson = JsonDocument.Parse(json))
            {
                var heroes = from hero in heroJson.RootElement.EnumerateObject()
                             let name = hero.Value.GetProperty("name").GetString()
                             let altName = hero.Value.GetProperty("alt_name").GetString()
                             let type = (HeroType)Enum.Parse(typeof(HeroType), hero.Value.GetProperty("type").GetString())
                             select new Hero(name, altName, type);

                Heroes = new ReadOnlyCollection<Hero>(heroes.ToList());
            }
        }

        private async Task LoadMapsAsync()
        {
            var json = await File.ReadAllTextAsync(Path.Combine(settings.AssetsPath, "Maps.json"));

            using (var mapJson = JsonDocument.Parse(json))
            {
                Maps = new ReadOnlyCollection<Map>(
                        (from map in mapJson.RootElement.EnumerateArray()
                         let name = map.GetProperty("name").GetString()
                         let altName = map.GetProperty("short_name").GetString()
                         select new Map(name, altName)).ToList());
            }
        }

        private async Task DownloadIfEmptyAsync()
        {
            var exists = Directory.Exists(heroesDataPath);
            var release = settings.HeroesToolChest.HeroesDataReleaseUri;

            if (exists && Directory.EnumerateFiles(heroesDataPath, "*.json", SearchOption.AllDirectories).Any())
            {
                logger.LogInformation("Heroes Data exists. No need to download HeroesToolChest hero-data.");
            }
            else
            {
                logger.LogInformation($"heroes-data does not exists. Downloading files to: {heroesDataPath}");

                if (!exists)
                    Directory.CreateDirectory(heroesDataPath);

                using (var client = new HttpClient())
                {
                    var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{settings.Github.User}:{settings.Github.AccessToken}"));

                    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("HeroesReplay", "1.0"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);

                    var response = await client.GetAsync(release);
                    var json = await response.Content.ReadAsStringAsync();

                    using (var document = JsonDocument.Parse(json))
                    {
                        if (document.RootElement.TryGetProperty("name", out JsonElement commit) && document.RootElement.TryGetProperty("zipball_url", out JsonElement link))
                        {
                            var name = commit.GetString();
                            var uri = link.GetString();

                            using (var data = await client.GetStreamAsync(new Uri(uri)))
                            {
                                using (var write = File.OpenWrite(Path.Combine(heroesDataPath, name)))
                                {
                                    await data.CopyToAsync(write);
                                    await write.FlushAsync();
                                }
                            }

                            using (var reader = File.OpenRead(Path.Combine(heroesDataPath, name)))
                            {
                                ZipArchive zip = new ZipArchive(reader);
                                zip.ExtractToDirectory(heroesDataPath);
                            }
                        }
                    }
                }
            }
        }

        private async Task LoadUnitsAsync()
        {
            var unitGroups = new Dictionary<string, UnitGroup>();
            var ignoreUnits = settings.HeroesToolChest.IgnoreUnits.ToList();
            var bossUnits = new HashSet<string>();
            var coreUnits = new HashSet<string>();

            var files = Directory
                .GetFiles(heroesDataPath, "*.json", SearchOption.AllDirectories)
                .Where(x => x.Contains(HeroData) || x.Contains(UnitData))
                .OrderByDescending(x => x.Contains(HeroData))
                .ThenBy(x => x.Contains(UnitData));

            foreach (var file in files)
            {
                using (var document = JsonDocument.Parse(File.ReadAllText(file), new JsonDocumentOptions() { AllowTrailingCommas = true }))
                {
                    foreach (var o in document.RootElement.EnumerateObject())
                    {
                        if (o.Value.TryGetProperty(ScalingLinkIdProperty, out JsonElement value) && settings.HeroesToolChest.ScalingLinkId.Equals(value.GetString()))
                        {
                            coreUnits.Add(o.Name.Contains("-") ? o.Name.Split('-')[1] : o.Name);
                        }

                        if (o.Value.TryGetProperty(UnitIdProperty, out var unitId))
                        {
                            var descriptors = new List<string>();

                            if (o.Value.TryGetProperty(DescriptorsProperty, out var descriptorsElement))
                            {
                                descriptors.AddRange(descriptorsElement.EnumerateArray().Select(x => x.GetString()));
                            }

                            unitGroups[unitId.GetString()] = UnitGroup.Hero;

                            if (o.Value.TryGetProperty(HeroUnitsProperty, out var heroUnits))
                            {
                                foreach (var hu in heroUnits.EnumerateArray())
                                {
                                    foreach (var huo in hu.EnumerateObject())
                                    {
                                        unitGroups[huo.Name] = UnitGroup.Hero;
                                    }
                                }
                            }
                        }
                        else
                        {
                            var name = o.Name.Contains("-") ? o.Name.Split('-')[1] : o.Name;
                            var map = o.Name.Contains("-") ? o.Name.Split('-')[0] : string.Empty;

                            List<string> attributes = new List<string>();
                            List<string> descriptors = new List<string>();

                            if (o.Value.TryGetProperty(AttributesProperty, out var attributesElement))
                                attributes.AddRange(attributesElement.EnumerateArray().Select(x => x.GetString()));

                            if (o.Value.TryGetProperty(DescriptorsProperty, out var descriptorsElement))
                                descriptors.AddRange(descriptorsElement.EnumerateArray().Select(x => x.GetString()));

                            if (attributes.Contains(AttributeMapBoss) && name.EndsWith(UnitNameDefender) && !ignoreUnits.Any(i => name.Contains(i)))
                            {
                                bossUnits.Add(name);
                                unitGroups[name] = UnitGroup.MercenaryCamp;
                                continue;
                            }

                            if (attributes.Contains(AttributeMapBoss) && name.EndsWith(UnitNameLaner) && !ignoreUnits.Any(i => name.Contains(i)))
                            {
                                unitGroups[name] = UnitGroup.MercenaryCamp;
                                continue;
                            }

                            if (attributes.Count == 1 && attributes.Contains(AttributeMerc) && !ignoreUnits.Any(i => name.Contains(i)))
                            {
                                unitGroups[name] = UnitGroup.MercenaryCamp;
                                continue;
                            }

                            // Beacons
                            // Gems on Tomb
                            if (name.EndsWith("CaptureBeacon") ||
                                name.EndsWith("ControlBeacon") ||
                                name.StartsWith("ItemSoulPickup") ||
                                name.Equals("ItemCannonball") ||
                                name.StartsWith("SoulCage") ||
                                name.Equals("DocksTreasureChest") ||
                                name.Equals("DocksPirateCaptain"))
                            {
                                unitGroups[name] = UnitGroup.MapObjective;
                                continue;
                            }

                            if ((attributes.Contains(AttributeMapCreature) || attributes.Contains(AttributeMapBoss)) && 
                                                                           !(name.EndsWith(UnitNameLaner) || name.EndsWith(UnitNameDefender)) && 
                                                                           !ignoreUnits.Any(i => name.Contains(i)))
                            {
                                unitGroups[name] = UnitGroup.MapObjective;
                                continue;
                            }

                            if (name.Contains(UnitNamePayload) && "hanamuradata".Contains(map) && !ignoreUnits.Any(i => name.Contains(i)))
                            {
                                unitGroups[name] = UnitGroup.MapObjective;
                                continue;
                            }

                            if (attributes.Contains("Heroic") && descriptors.Contains("PowerfulLaner") && !ignoreUnits.Any(i => name.Contains(i)))
                            {
                                unitGroups[name] = UnitGroup.MapObjective;
                                continue;
                            }

                            if (attributes.Contains(AttributeStructure) && !ignoreUnits.Any(i => name.Contains(i)))
                            {
                                unitGroups[name] = UnitGroup.Structures;
                                continue;
                            }

                            if (attributes.Count == 1 && attributes.Contains("Minion") && name.EndsWith("Minion") && !ignoreUnits.Any(i => name.Contains(i)))
                            {
                                unitGroups[name] = UnitGroup.Minions;
                                continue;
                            }

                            if (unitGroups.FirstOrDefault(c => o.Name.Contains(c.Key)).Key != null)
                            {
                                unitGroups[name] = name.Contains(HeroicTalent) ? UnitGroup.HeroTalentSelection : UnitGroup.HeroAbilityUse;
                                continue;
                            }

                            if (!unitGroups.ContainsKey(name))
                            {
                                unitGroups[name] = UnitGroup.Miscellaneous;
                                continue;
                            }
                        }
                    }
                }
            }

            this.UnitGroups = new ReadOnlyDictionary<string, UnitGroup>(unitGroups);
            this.BossUnits = new ReadOnlyCollection<string>(bossUnits.ToList());
            this.CoreUnits = new ReadOnlyCollection<string>(coreUnits.ToList());
        }

        public UnitGroup GetUnitGroup(string name)
        {
            if (UnitGroups == null)
                throw new InvalidOperationException("The data must be loaded before getting a unit group.");

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return UnitGroups.ContainsKey(name) ? UnitGroups[name] : UnitGroup.Unknown;
        }

        public async Task LoadDataAsync()
        {
            await DownloadIfEmptyAsync();
            await LoadUnitsAsync();
            await LoadMapsAsync();
            await LoadHeroesAsync();
        }
    }
}