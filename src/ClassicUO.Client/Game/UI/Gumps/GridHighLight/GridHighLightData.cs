using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.GameObjects;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.UI.Gumps.GridHighLight
{
    internal class GridHighlightData
    {
        private static GridHighlightData[] allConfigs;
        private readonly GridHighlightSetupEntry _entry;

        private static readonly Queue<uint> _queue = new();
        private static readonly HashSet<uint> _queuedItems = new();
        private static bool hasQueuedItems;
        private static World _worldRef;

        private readonly Dictionary<string, string> _normalizeCache = new();

        public static GridHighlightData[] AllConfigs
        {
            get
            {
                if (allConfigs != null)
                    return allConfigs;

                List<GridHighlightSetupEntry> setup = ProfileManager.CurrentProfile.GridHighlightSetup;
                allConfigs = setup.Select(entry => new GridHighlightData(entry)).ToArray();
                return allConfigs;
            }
            set => allConfigs = value;
        }

        public string Name
        {
            get => _entry.Name;
            set => _entry.Name = value;
        }

        public List<string> ItemNames
        {
            get => _entry.ItemNames;
            set => _entry.ItemNames = value;
        }

        public ushort Hue
        {
            get => _entry.Hue;
            set => _entry.Hue = value;
        }

        public Color HighlightColor
        {
            get => _entry.GetHighlightColor();
            set => _entry.SetHighlightColor(value);
        }

        public List<GridHighlightProperty> Properties
        {
            get => _entry.Properties;
            set
            {
                _entry.Properties = value;
                InvalidateCache();
            }
        }

        public bool AcceptExtraProperties
        {
            get => _entry.AcceptExtraProperties;
            set => _entry.AcceptExtraProperties = value;
        }

        public int MinimumProperty
        {
            get => _entry.MinimumProperty;
            set => _entry.MinimumProperty = value;
        }

        public int MaximumProperty
        {
            get => _entry.MaximumProperty;
            set => _entry.MaximumProperty = value;
        }

        public int MinimumMatchingProperty
        {
            get => _entry.MinimumMatchingProperty;
            set => _entry.MinimumMatchingProperty = value;
        }

        public int MaximumMatchingProperty
        {
            get => _entry.MaximumMatchingProperty;
            set => _entry.MaximumMatchingProperty = value;
        }

        public List<string> ExcludeNegatives
        {
            get => _entry.ExcludeNegatives;
            set
            {
                _entry.ExcludeNegatives = value;
                InvalidateCache();
            }
        }

        public bool Overweight
        {
            get => _entry.Overweight;
            set => _entry.Overweight = value;
        }

        public int MinimumWeight
        {
            get => _entry.MinimumWeight;
            set => _entry.MinimumWeight = value;
        }

        public int MaximumWeight
        {
            get => _entry.MaximumWeight;
            set => _entry.MaximumWeight = value;
        }

        public List<string> RequiredRarities
        {
            get => _entry.RequiredRarities;
            set
            {
                _entry.RequiredRarities = value;
                InvalidateCache();
            }
        }

        public GridHighlightSlot EquipmentSlots
        {
            get => _entry.GridHighlightSlot;
            set => _entry.GridHighlightSlot = value;
        }

        public bool LootOnMatch
        {
            get => _entry.LootOnMatch;
            set => _entry.LootOnMatch = value;
        }

        public uint DestinationContainer
        {
            get => _entry.DestinationContainer;
            set
            {
                _entry.DestinationContainer = value;
                _cachedLootEntry = null;
            }
        }

        private AutoLootManager.AutoLootConfigEntry _cachedLootEntry;

        private AutoLootManager.AutoLootConfigEntry GetLootEntry()
        {
            if (DestinationContainer == 0)
                return null;

            if (_cachedLootEntry == null || _cachedLootEntry.DestinationContainer != DestinationContainer)
            {
                _cachedLootEntry = new AutoLootManager.AutoLootConfigEntry
                {
                    DestinationContainer = DestinationContainer
                };
            }

            return _cachedLootEntry;
        }

        private List<string> _cachedNormalizedRulesExcludeNegatives;
        private HashSet<string> _cachedNormalizedRulesRequiredRarities;
        private HashSet<string> _cachedNormalizedAllRarities;
        private HashSet<string> _cachedNormalizedAllProperties;
        private HashSet<string> _cachedNormalizedAllNegatives;
        private Dictionary<string, (int MinValue, bool IsOptional)> _cachedNormalizedRulesProperties;
        private static readonly List<ItemPropertiesData> _reusableItemData = new(3);
        private static readonly List<uint> _reusableRequeueItems = new();
        private bool _cacheValid = false;

        private GridHighlightData(GridHighlightSetupEntry entry)
        {
            _entry = entry;
        }

        public void Delete()
        {
            ProfileManager.CurrentProfile.GridHighlightSetup.Remove(_entry);
            allConfigs = null;
        }

        public void Move(bool up)
        {
            List<GridHighlightSetupEntry> list = ProfileManager.CurrentProfile.GridHighlightSetup;
            int index = list.IndexOf(_entry);
            if (index == -1) return;

            if (up && index == 0) return;
            if (!up && index == list.Count - 1) return;

            list.RemoveAt(index);
            list.Insert(up ? index - 1 : index + 1, _entry);
        }

        public static void ProcessItemOpl(World world, Item item)
        {
            if (item.HighlightChecked) return;

            _worldRef = world;
            ProcessItemOpl(world, item.Serial);
        }


        public static void ProcessItemOpl(World world, uint serial)
        {
            _worldRef = world;

            if (!world.ClientFeatures.TooltipsEnabled)
                return;

            if (!_queuedItems.Add(serial))
                return;

            _queue.Enqueue(serial);
            hasQueuedItems = true;
        }

        public static void ProcessQueue(World world)
        {
            if (!hasQueuedItems)
                return;

            _worldRef = world;
            _reusableItemData.Clear();
            _reusableRequeueItems.Clear();

            for (int i = 0; i < 3 && _queue.Count > 0; i++)
            {
                uint ser = _queue.Dequeue();

                if (!world.Items.TryGetValue(ser, out Item item))
                {
                    _queuedItems.Remove(ser);
                    continue;
                }

                if (item.OnGround || item.IsMulti || item.HighlightChecked)
                {
                    _queuedItems.Remove(ser);
                    continue;
                }

                if (!world.OPL.TryGetNameAndData(ser, out _, out _))
                {
                    _reusableRequeueItems.Add(ser);
                    continue;
                }

                _queuedItems.Remove(ser);
                _reusableItemData.Add(new ItemPropertiesData(world, item));
            }

            foreach (ItemPropertiesData data in _reusableItemData)
            {
                data.item.HighlightChecked = true;
                GridHighlightData bestMatch = GetBestMatch(data);
                if (bestMatch != null)
                {
                    data.item.MatchesHighlightData = true;
                    data.item.HighlightColor = bestMatch.HighlightColor;
                    data.item.HighlightName = bestMatch.Name;

                    if (bestMatch.LootOnMatch)
                    {
                        Item root = world.Items.Get(data.item.RootContainer);
                        if (root != null && root.IsCorpse)
                        {
                            AutoLootManager.Instance.LootItem(data.item, bestMatch.GetLootEntry());
                            data.item.ShouldAutoLoot = true;
                        }
                    }
                }
            }

            foreach (uint ser in _reusableRequeueItems)
            {
                _queue.Enqueue(ser);
            }

            if (_queue.Count == 0)
            {
                hasQueuedItems = false;
                _queuedItems.Clear();
            }
        }

        public static GridHighlightData GetGridHighlightData(int index)
        {
            List<GridHighlightSetupEntry> list = ProfileManager.CurrentProfile.GridHighlightSetup;
            GridHighlightData data = index >= 0 && index < list.Count ? new GridHighlightData(list[index]) : null;

            if (data == null)
            {
                list.Add(new GridHighlightSetupEntry());
                data = new GridHighlightData(list[index]);
            }

            return data;
        }

        public static void RecheckMatchStatus()
        {
            AllConfigs = null;

            World world = _worldRef;
            if (world == null)
                return;

            foreach (KeyValuePair<uint, Item> kvp in world.Items)
            {
                Item item = kvp.Value;
                if (item.OnGround || item.IsMulti)
                    continue;

                item.MatchesHighlightData = false;
                item.HighlightName = null;
                item.HighlightColor = Color.Transparent;
                item.ShouldAutoLoot = false;
                item.HighlightChecked = false;

                ProcessItemOpl(world, kvp.Key);
            }
        }

        public bool IsMatch(ItemPropertiesData itemData) => AcceptExtraProperties
                ? IsMatchFromProperties(itemData)
                : IsMatchFromItemPropertiesData(itemData);

        public bool DoesPropertyMatch(ItemPropertiesData.SinglePropertyData property)
        {
            foreach (GridHighlightProperty rule in Properties)
            {
                string nProp = Normalize(property.Name);
                string nRule = Normalize(rule.Name);

                bool nameMatch = nProp.Equals(nRule, StringComparison.OrdinalIgnoreCase) ||
                                 nProp.IndexOf(nRule, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 Normalize(property.OriginalString).IndexOf(nRule, StringComparison.OrdinalIgnoreCase) >= 0;

                bool valueMatch = rule.MinValue == -1 || property.FirstValue >= rule.MinValue;

                if (nameMatch && valueMatch)
                    return true;
            }

            if (RequiredRarities.Any(r => Normalize(property.Name).Equals(Normalize(r), StringComparison.OrdinalIgnoreCase)))
                return true;

            return false;
        }

        public void InvalidateCache()
        {
            _cacheValid = false;
            RecheckMatchStatus();
        }

        private void EnsureCache()
        {
            if (_cacheValid) return;

            _cachedNormalizedAllRarities = new HashSet<string>(
                GridHighlightRules.RarityProperties.Select(Normalize), StringComparer.OrdinalIgnoreCase);
            _cachedNormalizedAllProperties = new HashSet<string>(
                GridHighlightRules.Properties.Concat(GridHighlightRules.SlayerProperties).Concat(GridHighlightRules.SuperSlayerProperties).Select(Normalize), StringComparer.OrdinalIgnoreCase);
            _cachedNormalizedAllNegatives = new HashSet<string>(
                GridHighlightRules.NegativeProperties.Select(Normalize), StringComparer.OrdinalIgnoreCase);

            _cachedNormalizedRulesExcludeNegatives = ExcludeNegatives.Select(Normalize).ToList();
            _cachedNormalizedRulesRequiredRarities = new HashSet<string>(
                RequiredRarities.Select(Normalize), StringComparer.OrdinalIgnoreCase);
            _cachedNormalizedRulesProperties = Properties
                .GroupBy(p => Normalize(p.Name))
                .ToDictionary(g => g.Key,
                              g =>
                              {
                                  int minValue = g.Max(x => x.MinValue);
                                  bool isOptional = g.All(x => x.IsOptional);
                                  return (minValue, isOptional);
                              },
                              StringComparer.OrdinalIgnoreCase);

            _cacheValid = true;
        }

        private bool IsMatchFromProperties(ItemPropertiesData itemData)
        {
            EnsureCache();

            if (!IsItemNameMatch(itemData.Name) || (itemData.item != null && !MatchesSlot(itemData.item.ItemData.Layer)))
                return false;

            Dictionary<string, (int MinValue, bool IsOptional)> normalizedRulesProperties = _cachedNormalizedRulesProperties;
            List<string> normalizedRulesExcludeNegatives = _cachedNormalizedRulesExcludeNegatives;
            HashSet<string> normalizedRulesRequiredRarities = _cachedNormalizedRulesRequiredRarities;

            HashSet<string> normalizedAllRarities = _cachedNormalizedAllRarities;
            HashSet<string> normalizedAllProperties = _cachedNormalizedAllProperties;

            var normalizedItemProperties = itemData.singlePropertyData
                .GroupBy(p => Normalize(p.Name))
                .ToDictionary(
                    g => g.Key,
                    g => (Original: Normalize(g.First().OriginalString), Value: g.Max(x => x.FirstValue))
                );

            bool hasRequiredRarity = normalizedRulesRequiredRarities.Count == 0;
            foreach (KeyValuePair<string, (string Original, double Value)> normalizedItemProperty in normalizedItemProperties)
            {
                string propertyName = normalizedItemProperty.Key;
                string original = normalizedItemProperty.Value.Original;

                if (Overweight && !IsWeightInRange(original, MinimumWeight, MaximumWeight))
                    return false;

                if (normalizedRulesExcludeNegatives.Any(pattern => propertyName.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0 || original.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0))
                    return false;

                if (!hasRequiredRarity && normalizedAllRarities.Contains(propertyName) && normalizedRulesRequiredRarities.Contains(propertyName))
                    hasRequiredRarity = true;
            }

            if (!hasRequiredRarity)
                return false;

            var matchedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var matchedRequiredProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, (int MinValue, bool IsOptional)> normalizedRulesProperty in normalizedRulesProperties)
            {
                string normalizedPropertyName = normalizedRulesProperty.Key;
                int propertyMinValue = normalizedRulesProperty.Value.MinValue;
                bool isPropertyOptional = normalizedRulesProperty.Value.IsOptional;

                if (!normalizedItemProperties.TryGetValue(normalizedPropertyName, out (string Original, double Value) normalizedItemProperty))
                    continue;

                if (propertyMinValue == -1 || normalizedItemProperty.Value >= propertyMinValue)
                {
                    matchedProperties.Add(normalizedPropertyName);
                    if (!isPropertyOptional)
                        matchedRequiredProperties.Add(normalizedPropertyName);
                }
            }

            if (normalizedRulesProperties.Any(p => !p.Value.IsOptional && !matchedRequiredProperties.Contains(p.Key)))
                return false;

            if (!IsMatchingCount(matchedProperties.Count, MinimumMatchingProperty, MaximumMatchingProperty))
                return false;

            var includedProps = new HashSet<string>(normalizedItemProperties.Keys.Intersect(normalizedAllProperties), StringComparer.OrdinalIgnoreCase);

            if (!IsMatchingCount(includedProps.Count, MinimumProperty, MaximumProperty))
                return false;

            return true;
        }

        private bool IsMatchFromItemPropertiesData(ItemPropertiesData itemData)
        {
            EnsureCache();

            if (!IsItemNameMatch(itemData.Name))
                return false;

            if (itemData.item != null && !MatchesSlot(itemData.item.ItemData.Layer))
                return false;

            var normalizedItemLines = itemData.singlePropertyData
                .GroupBy(p => Normalize(p.Name))
                .ToDictionary(
                    g => g.Key,
                    g => (Original: Normalize(g.First().OriginalString), Value: g.Max(x => x.FirstValue))
                );

            Dictionary<string, (int MinValue, bool IsOptional)> normalizedRulesProperties = _cachedNormalizedRulesProperties;
            List<string> normalizedRulesExcludeNegatives = _cachedNormalizedRulesExcludeNegatives;
            HashSet<string> normalizedRulesRequiredRarities = _cachedNormalizedRulesRequiredRarities;

            HashSet<string> normalizedAllRarities = _cachedNormalizedAllRarities;
            HashSet<string> normalizedAllProperties = _cachedNormalizedAllProperties;

            var itemNegatives = normalizedItemLines.Where(p =>
                normalizedRulesExcludeNegatives.Any(rule =>
                    rule.Equals(p.Key, StringComparison.OrdinalIgnoreCase))).ToList();

            var itemRarities = normalizedItemLines.Where(p =>
                normalizedRulesRequiredRarities.Any(rule =>
                    rule.Equals(p.Key, StringComparison.OrdinalIgnoreCase))).ToList();

            var itemProperties = normalizedItemLines.Where(p =>
                 normalizedAllProperties.Any(rule =>
                     rule.Equals(p.Key, StringComparison.OrdinalIgnoreCase))).ToList();

            if (!itemProperties.Any() && !itemNegatives.Any() && !itemRarities.Any())
                return false;

            if (Overweight && normalizedItemLines.Any(prop => !IsWeightInRange(prop.Value.Original, MinimumWeight, MaximumWeight)))
            {
                return false;
            }

            foreach (string normalizedRulesExcludeNegative in normalizedRulesExcludeNegatives)
            {
                if (itemProperties.Any(p => p.Key.IndexOf(normalizedRulesExcludeNegative, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    itemNegatives.Any(p => p.Key.IndexOf(normalizedRulesExcludeNegative, StringComparison.OrdinalIgnoreCase) >= 0))
                    return false;
            }

            if (normalizedRulesRequiredRarities.Count > 0)
            {
                bool hasRequired = itemRarities.Any(r =>
                    normalizedRulesRequiredRarities.Any(req =>
                        r.Key.Equals(req, StringComparison.OrdinalIgnoreCase)));

                if (!hasRequired)
                    return false;
            }

            int matchingPropertiesCount = 0;

            var filteredItemLines = normalizedItemLines
                .Where(p => normalizedAllProperties.Contains(p.Key))
                .ToList();

            var filteredNotOptionalRules = normalizedRulesProperties
                .Where(p => !p.Value.IsOptional)
                .ToList();

            var filteredOptionalRules = normalizedRulesProperties
                .Where(p => p.Value.IsOptional)
                .ToList();

            foreach (KeyValuePair<string, (string Original, double Value)> filteredItemLine in filteredItemLines)
            {
                if (!normalizedRulesProperties.TryGetValue(filteredItemLine.Key, out (int MinValue, bool IsOptional) rule))
                    return false;
            }

            foreach (KeyValuePair<string, (int MinValue, bool IsOptional)> filteredNotOptionalRule in filteredNotOptionalRules)
            {
                double minValue = filteredNotOptionalRule.Value.MinValue;

                KeyValuePair<string, (string Original, double Value)> filteredItemLine = filteredItemLines.FirstOrDefault(x => x.Key == filteredNotOptionalRule.Key);
                if (string.IsNullOrEmpty(filteredItemLine.Key) || (minValue != -1 && filteredItemLine.Value.Value < minValue))
                    return false;

                matchingPropertiesCount++;
            }

            foreach (KeyValuePair<string, (int MinValue, bool IsOptional)> filteredOptionalRule in filteredOptionalRules)
            {
                double minValue = filteredOptionalRule.Value.MinValue;

                KeyValuePair<string, (string Original, double Value)> filteredItemLine = filteredItemLines.FirstOrDefault(x => x.Key == filteredOptionalRule.Key);
                if (string.IsNullOrEmpty(filteredItemLine.Key) || (minValue != -1 && filteredItemLine.Value.Value < minValue))
                    continue;

                matchingPropertiesCount++;
            }

            if (!IsMatchingCount(matchingPropertiesCount, MinimumMatchingProperty, MaximumMatchingProperty))
                return false;

            if (!IsMatchingCount(filteredItemLines.Count, MinimumProperty, MaximumProperty))
                return false;

            return true;
        }

        public static GridHighlightData GetBestMatch(ItemPropertiesData itemData)
        {
            GridHighlightData best = null;
            double bestScore = -1;

            foreach (GridHighlightData config in AllConfigs)
            {
                if (!config.IsMatch(itemData))
                    continue;

                double score = 0;
                int totalRules = config.Properties.Count;
                int matchedRules = 0;

                foreach (ItemPropertiesData.SinglePropertyData prop in itemData.singlePropertyData)
                {
                    foreach (GridHighlightProperty rule in config.Properties)
                    {
                        string nProp = config.Normalize(prop.Name);
                        string nRule = config.Normalize(rule.Name);

                        if (nProp.Equals(nRule, StringComparison.OrdinalIgnoreCase))
                        {
                            double delta = prop.FirstValue >= rule.MinValue + 5 ? 3.0 : 2.0;
                            score += delta;
                            matchedRules++;
                        }
                        else if (nProp.IndexOf(nRule, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 config.Normalize(prop.OriginalString).IndexOf(nRule, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            score += 1.0;
                            matchedRules++;
                        }
                    }
                }

                if (totalRules > 0)
                {
                    score /= totalRules;
                }

                int requiredCount = config.Properties.Count(p => !p.IsOptional);
                if (requiredCount > 0)
                {
                    double bonus = (double)matchedRules / requiredCount * 0.2;
                    score += bonus;
                }

                double specificity = (1.0 - (config.Properties.Count(p => p.IsOptional) / (double)Math.Max(1, totalRules))) * 0.1;
                score += specificity;

                if (best == null || score > bestScore)
                {
                    best = config;
                    bestScore = score;
                }
            }

            return best;
        }

        private bool IsMatchingCount(int count, int minPropertyCount, int maxPropertyCount)
        {
            if (minPropertyCount > 0 && count < minPropertyCount)
            {
                return false;
            }
            if (maxPropertyCount > 0 && count > maxPropertyCount)
            {
                return false;
            }

            return true;
        }

        private string Normalize(string input)
        {
            input ??= string.Empty;

            if (_normalizeCache.TryGetValue(input, out string cached))
                return cached;

            string result = StripHtmlTags(input);
            _normalizeCache[input] = result;
            return result;
        }

        private string CleanItemName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;

            int index = 0;
            while (index < name.Length && char.IsDigit(name[index])) index++;
            while (index < name.Length && char.IsWhiteSpace(name[index])) index++;

            return name.Substring(index).Trim().ToLowerInvariant();
        }

        private string StripHtmlTags(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            char[] output = new char[input.Length];
            int outputIndex = 0;
            bool insideTag = false;

            foreach (char c in input)
            {
                if (c == '<') { insideTag = true; continue; }
                if (c == '>') { insideTag = false; continue; }
                if (!insideTag) output[outputIndex++] = c;
            }

            return new string(output, 0, outputIndex).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormKC);
        }

        private bool IsItemNameMatch(string itemName)
        {
            if (ItemNames.Count == 0)
                return true;

            string cleanedUpItemName = CleanItemName(itemName);
            return ItemNames.Any(name => string.Equals(cleanedUpItemName, name.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsWeightInRange(string propertyString, int minWeight, int maxWeight)
        {
            int weightIndex = propertyString.IndexOf("weight:", StringComparison.OrdinalIgnoreCase);
            if (weightIndex < 0)
                return true;

            int startIndex = weightIndex + 7;
            int endIndex = propertyString.IndexOf("stone", startIndex, StringComparison.OrdinalIgnoreCase);
            if (endIndex < 0)
                return true;

            string weightStr = propertyString.Substring(startIndex, endIndex - startIndex).Trim();
            if (!int.TryParse(weightStr, out int weight))
            {
                Log.Debug($"FAILED TO PARSE: {weightStr}");
                return true;
            }

            bool passesMin = minWeight == 0 || weight >= minWeight;
            bool passesMax = maxWeight == 0 || weight <= maxWeight;

            return passesMin && passesMax;
        }

        private bool MatchesSlot(byte layer)
        {
            if (EquipmentSlots.Other)
            {
                return true;
            }

            return layer switch
            {
                (byte)Layer.Talisman => EquipmentSlots.Talisman,
                (byte)Layer.OneHanded => EquipmentSlots.RightHand,
                (byte)Layer.TwoHanded => EquipmentSlots.LeftHand,
                (byte)Layer.Helmet => EquipmentSlots.Head,
                (byte)Layer.Earrings => EquipmentSlots.Earring,
                (byte)Layer.Necklace => EquipmentSlots.Neck,
                (byte)Layer.Torso or (byte)Layer.Tunic => EquipmentSlots.Chest,
                (byte)Layer.Shirt => EquipmentSlots.Shirt,
                (byte)Layer.Cloak => EquipmentSlots.Back,
                (byte)Layer.Robe => EquipmentSlots.Robe,
                (byte)Layer.Arms => EquipmentSlots.Arms,
                (byte)Layer.Gloves => EquipmentSlots.Hands,
                (byte)Layer.Bracelet => EquipmentSlots.Bracelet,
                (byte)Layer.Ring => EquipmentSlots.Ring,
                (byte)Layer.Waist => EquipmentSlots.Belt,
                (byte)Layer.Skirt => EquipmentSlots.Skirt,
                (byte)Layer.Legs => EquipmentSlots.Legs,
                (byte)Layer.Pants => EquipmentSlots.Legs,
                (byte)Layer.Shoes => EquipmentSlots.Footwear,

                (byte)Layer.Hair or
                (byte)Layer.Beard or
                (byte)Layer.Face or
                (byte)Layer.Mount or
                (byte)Layer.Backpack or
                (byte)Layer.ShopBuy or
                (byte)Layer.ShopBuyRestock or
                (byte)Layer.ShopSell or
                (byte)Layer.Bank or
                (byte)Layer.Invalid => false,

                _ => true
            };
        }
    }
}
