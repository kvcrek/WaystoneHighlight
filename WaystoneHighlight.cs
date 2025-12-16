using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using System.Linq;
using System.Numerics;
using System.Drawing;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using RectangleF = ExileCore2.Shared.RectangleF;
using ExileCore2.Shared.Nodes;
using System.Windows.Forms;
using ExileCore2.PoEMemory;
using ItemFilterLibrary;

namespace WaystoneHighlight
{
    public class WaystoneHighlight : BaseSettingsPlugin<WaystoneHighlightSettings>
    {
        private IngameState InGameState => GameController.IngameState;
        private List<string> BannedModifiers;

        // Parse banned modifiers from settings.
        private void ParseBannedModifiers()
        {
            BannedModifiers = Settings.Score.BannedModifiers.Value
                .Split(',')
                .Select(x => x.Trim().ToLower())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        public override bool Initialise()
        {
            // Reload banned modifiers when the setting is pressed.
            Settings.Score.ReloadBannedModifiers.OnPressed = ParseBannedModifiers;
            ParseBannedModifiers();
            return base.Initialise();
        }

        public override void Render()
        {
            var waystones = new List<WaystoneItem>();

            var stashPanel = InGameState.IngameUi.StashElement;
            var guildStashPanel = InGameState.IngameUi.GuildStashElement;
            var inventoryPanel = InGameState.IngameUi.InventoryPanel;

            bool isQuadTab = false;

            if (inventoryPanel.IsVisible)
            {
                // ----- Normal Stash -----
                if (stashPanel != null && stashPanel.VisibleStash != null && stashPanel.VisibleStash.IsVisible)
                {
                    if (stashPanel.VisibleStash.TotalBoxesInInventoryRow == 24)
                        isQuadTab = true;

                    if (stashPanel.VisibleStash.VisibleInventoryItems != null)
                    {
                        foreach (var item in stashPanel.VisibleStash.VisibleInventoryItems)
                        {
                            if (item?.Item == null)
                                continue;

                            var baseComp = item.Item.GetComponent<Base>();
                            var mapComp = item.Item.GetComponent<Map>();
                            var modsComp = item.Item.GetComponent<Mods>();
                            if (baseComp == null || mapComp == null || modsComp == null)
                                continue;

                            var modsData = CreateModsData(modsComp);
                            waystones.Add(new WaystoneItem(baseComp, mapComp, modsData, item.GetClientRectCache, ItemLocation.Stash));
                        }
                    }
                    else
                    {
                        // ----- Map Stash -----
                        Element openTab = getOpenMapTierStash();
                        if (openTab != null)
                        {
                            int stashTier = (int)openTab.IndexInParent;
                            var wsElements = getWaystonesInMapStash(stashTier);
                            if (wsElements != null)
                            {
                                foreach (var element in wsElements)
                                {
                                    if (!element.IsVisible)
                                        continue;

                                    var baseComp = element.Entity.GetComponent<Base>();
                                    var mapComp = element.Entity.GetComponent<Map>();
                                    var modsComp = element.Entity.GetComponent<Mods>();
                                    if (baseComp == null || mapComp == null || modsComp == null)
                                        continue;

                                    var modsData = CreateModsData(modsComp);
                                    waystones.Add(new WaystoneItem(baseComp, mapComp, modsData, element.GetClientRectCache, ItemLocation.Stash));
                                }
                            }
                        }
                    }
                }
                // ----- Guild Stash -----
                else if (guildStashPanel != null && guildStashPanel.VisibleStash != null && guildStashPanel.VisibleStash.IsVisible)
                {
                    if (guildStashPanel.VisibleStash.TotalBoxesInInventoryRow == 24)
                        isQuadTab = true;

                    if (guildStashPanel.VisibleStash.VisibleInventoryItems != null)
                    {
                        foreach (var item in guildStashPanel.VisibleStash.VisibleInventoryItems)
                        {
                            if (item?.Item == null)
                                continue;

                            var baseComp = item.Item.GetComponent<Base>();
                            var mapComp = item.Item.GetComponent<Map>();
                            var modsComp = item.Item.GetComponent<Mods>();
                            if (baseComp == null || mapComp == null || modsComp == null)
                                continue;

                            // For guild stash, we still use the full mods data.
                            var modsData = CreateModsData(modsComp);
                            waystones.Add(new WaystoneItem(baseComp, mapComp, modsData, item.GetClientRectCache, ItemLocation.Stash));
                        }
                    }
                }

                // ----- Inventory Items -----
                var inventoryItems = GameController.IngameState.ServerData.PlayerInventories[0].Inventory.InventorySlotItems;
                foreach (var item in inventoryItems)
                {
                    if (item.Item == null)
                        continue;

                    var baseComp = item.Item.GetComponent<Base>();
                    var mapComp = item.Item.GetComponent<Map>();
                    var modsComp = item.Item.GetComponent<Mods>();
                    if (baseComp == null || mapComp == null || modsComp == null)
                        continue;

                    var modsData = CreateModsData(modsComp);
                    waystones.Add(new WaystoneItem(baseComp, mapComp, modsData, item.GetClientRect(), ItemLocation.Inventory));
                }

                // Process waystones: calculate score and draw overlays.
                foreach (var waystone in waystones)
                {
                    var mapItem = waystone.map;
                    if (mapItem == null || mapItem.Tier < Settings.Score.MinimumTier)
                        continue;

                    // Define bbox as the item's rectangle.
                    var bbox = waystone.rect;

                    // Use ModsData properties to get prefix and suffix counts.
                    int prefixCount = waystone.mods.Prefixes?.Count() ?? 0;
                    int suffixCount = waystone.mods.Suffixes?.Count() ?? 0;

                    int score = 0, iiq = 0, iir = 0;
                    bool extraRareMod = false;
                    int packSize = 0, magicPackSize = 0, extraPacks = 0, extraMagicPack = 0, extraRarePack = 0, additionalPacks = 0;
                    bool hasBannedMod = false;
                    bool isCorrupted = waystone.baseComponent.isCorrupted;

                    foreach (var mod in waystone.mods.ItemMods)
                    {
                        // Check for banned modifiers.
                        if (BannedModifiers.Count > 0 &&
                            BannedModifiers.Any(banned => mod.DisplayName.Contains(banned, StringComparison.OrdinalIgnoreCase)))
                        {
                            hasBannedMod = true;
                            break;
                        }

                        
                        if (mod.ModRecord?.StatNames != null)
                        {
                            for (int i = 0; i < mod.ModRecord.StatNames.Length && i < mod.Values.Count; i++)
                            {
                                var statKey = mod.ModRecord.StatNames[i]?.Key;
                                if (statKey == null) continue;

                                if (statKey.Contains("map_item_drop_quantity", StringComparison.OrdinalIgnoreCase))
                                    iiq += mod.Values[i];
                                else if (statKey.Contains("map_item_drop_rarity", StringComparison.OrdinalIgnoreCase))
                                    iir += mod.Values[i];
                                else if (statKey.Contains("map_number_of_rare_packs", StringComparison.OrdinalIgnoreCase))
                                    extraRarePack += mod.Values[i];
                                else if (statKey.Contains("map_rare_monster_num_additional_modifiers", StringComparison.OrdinalIgnoreCase))
                                    extraRareMod = true;
                                else if (statKey.Contains("map_pack_size_+%", StringComparison.OrdinalIgnoreCase))
                                    packSize += mod.Values[i];
                                else if (statKey.Contains("map_number_of_magic_packs", StringComparison.OrdinalIgnoreCase))
                                    magicPackSize += mod.Values[i];
                                else if (statKey.Contains("map_monster_tre_+%", StringComparison.OrdinalIgnoreCase))
                                    extraPacks += mod.Values[i];
                                else if (statKey.Contains("map_monster_additional", StringComparison.OrdinalIgnoreCase))
                                    additionalPacks += mod.Values[i];
                            }
                        }
                    }

                    score += iiq * Settings.Score.ScorePerQuantity;
                    score += iir * Settings.Score.ScorePerRarity;
                    score += packSize * Settings.Score.ScorePerPackSize;
                    score += magicPackSize * Settings.Score.ScorePerMagicPackSize;
                    score += extraPacks * Settings.Score.ScorePerExtraPacksPercent;
                    score += extraMagicPack * Settings.Score.ScorePerExtraMagicPack;
                    score += extraRarePack * Settings.Score.ScorePerExtraRarePack;
                    score += additionalPacks * Settings.Score.ScorePerAdditionalPack;
                    if (extraRareMod)
                        score += Settings.Score.ScoreForExtraRareMonsterModifier;

                    // Draw highlight overlay based on banned mods and score.
                    if (hasBannedMod)
                    {
                        switch (Settings.Graphics.BannedHightlightStyle)
                        {
                            case 1:
                                DrawBorderHighlight(bbox, Settings.Graphics.BannedHighlightColor, Settings.Graphics.BorderHighlight.BannedBorderThickness);
                                break;
                            case 2:
                                DrawBoxHighlight(bbox, Settings.Graphics.BannedHighlightColor, Settings.Graphics.BoxHighlight.BannedBoxRounding.Value);
                                break;
                        }
                    }
                    else if (score >= Settings.Score.MinimumCraftHighlightScore)
                    {
                        if (prefixCount < 3 && !isCorrupted)
                        {
                            switch (Settings.Graphics.CraftHightlightStyle)
                            {
                                case 1:
                                    DrawBorderHighlight(bbox, Settings.Graphics.CraftHighlightColor, Settings.Graphics.BorderHighlight.CraftBorderThickness.Value);
                                    break;
                                case 2:
                                    DrawBoxHighlight(bbox, Settings.Graphics.CraftHighlightColor, Settings.Graphics.BoxHighlight.CraftBoxRounding.Value);
                                    break;
                            }
                        }
                        else if (score >= Settings.Score.MinimumRunHighlightScore)
                        {
                            switch (Settings.Graphics.RunHightlightStyle)
                            {
                                case 1:
                                    DrawBorderHighlight(bbox, Settings.Graphics.RunHighlightColor, Settings.Graphics.BorderHighlight.RunBorderThickness.Value);
                                    break;
                                case 2:
                                    DrawBoxHighlight(bbox, Settings.Graphics.RunHighlightColor, Settings.Graphics.BoxHighlight.RunBoxRounding.Value);
                                    break;
                            }
                        }
                    }

                    // Draw stats (score and prefix/suffix counts) if the item is in inventory or a non-quad-tab stash.
                    if (waystone.location == ItemLocation.Inventory || (waystone.location == ItemLocation.Stash && !isQuadTab))
                    {
                        using (Graphics.SetTextScale(Settings.Graphics.FontSize.QRFontSizeMultiplier))
                        {
                            Graphics.DrawText(iir.ToString(), new Vector2(bbox.Left + 5, bbox.Top), ExileCore2.Shared.Enums.FontAlign.Left);
                            Graphics.DrawText(iiq.ToString(), new Vector2(bbox.Left + 5, bbox.Top + 2 + (10 * Settings.Graphics.FontSize.QRFontSizeMultiplier)), ExileCore2.Shared.Enums.FontAlign.Left);
                            if (extraRareMod)
                                Graphics.DrawText("+1", new Vector2(bbox.Left + 5, bbox.Top + 4 + (20 * Settings.Graphics.FontSize.QRFontSizeMultiplier)), ExileCore2.Shared.Enums.FontAlign.Left);
                        }
                        using (Graphics.SetTextScale(Settings.Graphics.FontSize.PrefSuffFontSizeMultiplier))
                        {
                            Graphics.DrawText(prefixCount.ToString(), new Vector2(bbox.Right - 5, bbox.Top), ExileCore2.Shared.Enums.FontAlign.Right);
                            Graphics.DrawText(suffixCount.ToString(), new Vector2(bbox.Right - 5, bbox.Top + 2 + (10 * Settings.Graphics.FontSize.PrefSuffFontSizeMultiplier)), ExileCore2.Shared.Enums.FontAlign.Right);
                        }
                        using (Graphics.SetTextScale(Settings.Graphics.FontSize.ScoreFontSizeMultiplier))
                        {
                            Graphics.DrawText(score.ToString(), new Vector2(bbox.Left + 5, bbox.Bottom - 5 - (15 * Settings.Graphics.FontSize.ScoreFontSizeMultiplier)), ExileCore2.Shared.Enums.FontAlign.Left);
                        }
                    }
                }
            }
        }

        // Helper method to create a full ModsData instance from a Mods component.
        private ItemData.ModsData CreateModsData(Mods modsComponent)
        {
            return new ItemData.ModsData(
                modsComponent.ItemMods,
                modsComponent.EnchantedMods,
                modsComponent.ExplicitMods,
                modsComponent.CorruptionImplicitMods,
                modsComponent.ImplicitMods,
                modsComponent.SynthesisMods);
        }

        // Draw a border highlight around the given rectangle.
        private void DrawBorderHighlight(RectangleF rect, ColorNode color, int thickness)
        {
            int scale = thickness - 1;
            int innerX = (int)rect.X + 1 + (int)(0.5 * scale);
            int innerY = (int)rect.Y + 1 + (int)(0.5 * scale);
            int innerWidth = (int)rect.Width - 1 - scale;
            int innerHeight = (int)rect.Height - 1 - scale;
            RectangleF scaledFrame = new RectangleF(innerX, innerY, innerWidth, innerHeight);
            Graphics.DrawFrame(scaledFrame, color, thickness);
        }

        // Draw a box highlight around the given rectangle.
        private void DrawBoxHighlight(RectangleF rect, ColorNode color, int rounding)
        {
            int innerX = (int)rect.X + 1 + (int)(0.5 * rounding);
            int innerY = (int)rect.Y + 1 + (int)(0.5 * rounding);
            int innerWidth = (int)rect.Width - 1 - rounding;
            int innerHeight = (int)rect.Height - 1 - rounding;
            RectangleF scaledBox = new RectangleF(innerX, innerY, innerWidth, innerHeight);
            Graphics.DrawBox(scaledBox, color, rounding);
        }

        // Return the currently open map-tier stash tab.
        private Element getOpenMapTierStash()
        {
            foreach (var stashTab in InGameState.IngameUi.StashElement.VisibleStash.Parent.Children[0].Children[1].Children)
            {
                if (stashTab.IsVisible)
                    return stashTab;
            }
            return null;
        }

        // Return a list of elements representing waystones in the map stash for the specified tier.
        private IList<Element> getWaystonesInMapStash(int waystoneTier)
        {
            try
            {
                Element tabInsideTier = getTabInsideTier(waystoneTier);
                if (tabInsideTier == null)
                {
                    LogMessage("Returning Null Tab: ");
                    return null;
                }
                LogMessage("Tab Inside tier: " + (int)tabInsideTier.IndexInParent);
                int tabIndex = (int)tabInsideTier.IndexInParent;

                return InGameState.IngameUi.StashElement.VisibleStash.Parent
                    .Children[0]
                    .Children[1]
                    .Children[waystoneTier]
                    .Children[0]
                    .Children[1]
                    .Children[tabIndex]
                    .Children[0]
                    .Children;
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        // Return the currently visible tab within the specified waystone tier.
        private Element getTabInsideTier(int waystoneTier)
        {
            try
            {
                foreach (var tab in InGameState.IngameUi.StashElement.VisibleStash.Parent
                    .Children[0]
                    .Children[1]
                    .Children[waystoneTier]
                    .Children[0]
                    .Children[1]
                    .Children)
                {
                    if (tab.IsVisible)
                        return tab;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
            return null;
        }
    }
}