using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.PoEMemory.Elements.InventoryElements;

using System.Linq;

using System.Numerics;

using System.Drawing;
using System;
using System.Collections.Generic;
using Microsoft.VisualBasic.Logging;

namespace WaystoneHighlight;

public class WaystoneHighlight : BaseSettingsPlugin<WaystoneHighlightSettings>
{
    private IngameState InGameState => GameController.IngameState;
    private List<string> BannedModifiers;

    private void ParseBannedModifiers()
    {
        BannedModifiers = Settings.BannedModifiers.Value
            .Split(',')
            .Select(x => x.Trim().ToLower())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();


    }

    public override bool Initialise()
    {
        //BannedModifiers = ParseBannedModifiers();
        Settings.ReloadBannedModifiers.OnPressed = ParseBannedModifiers;
        ParseBannedModifiers();
        return base.Initialise();
    }
    public override void Render()
    {
        IList<WaystoneItem> waystones = [];


        var stashPanel = InGameState.IngameUi.StashElement;
        var stashPanelGuild = InGameState.IngameUi.GuildStashElement;
        var inventoryPanel = InGameState.IngameUi.InventoryPanel;


        // Run if inventory panel is opened
        if (inventoryPanel.IsVisible)
        {
            // Add stash items
            if (stashPanel.IsVisible && stashPanel.VisibleStash != null)
            {
                foreach (var item in stashPanel.VisibleStash.VisibleInventoryItems)
                {
                    waystones.Add(new WaystoneItem(item.Item.GetComponent<Map>(), item.Item.GetComponent<Mods>(), item.GetClientRectCache));
                }
            } else if (stashPanelGuild.IsVisible && stashPanelGuild != null)
            {
                foreach (var item in stashPanelGuild.VisibleStash.VisibleInventoryItems)
                {
                    waystones.Add(new WaystoneItem(item.Item.GetComponent<Map>(), item.Item.GetComponent<Mods>(), item.GetClientRectCache));
                }
            }
            // Add inventory items
            var inventoryItems = GameController.IngameState.ServerData.PlayerInventories[0].Inventory.InventorySlotItems;
            foreach (var item in inventoryItems)
            {
                waystones.Add(new(item.Item.GetComponent<Map>(), item.Item.GetComponent<Mods>(), item.GetClientRect()));

            }

            foreach (var waystone in waystones)
            {

                var item = waystone.map;

                if (item == null)
                    continue;

                // Check for map tier
                if (item.Tier < Settings.MinimumTier)
                {
                    continue;
                }

                var itemMods = waystone.mods;
                var bbox = waystone.rect;

                int prefixCount = 0;
                int suffixCount = 0;

                int score = 0;

                int iiq = 0;
                int iir = 0;
                bool extraRareMod = false;
                int packSize = 0;
                int magicPackSize = 0;
                int extraPacks = 0;
                int extraMagicPack = 0;
                int extraRarePack = 0;
                int additionalPacks = 0;

                var drawColor = Color.White;
                bool hasBannedMod = false;

                // Iterate through the mods
                foreach (var mod in itemMods.ItemMods)
                {
                    // Check for banned modifiers
                    if (BannedModifiers.Count > 0)
                    {
                        foreach (var bannedMod in BannedModifiers)
                        {

                            if (mod.DisplayName.Contains(bannedMod, StringComparison.OrdinalIgnoreCase))
                            {
                                hasBannedMod = true;
                                break;
                            }
                        }
                    }

                    // Count prefixes and suffixes
                    if (mod.DisplayName.StartsWith("of", StringComparison.OrdinalIgnoreCase))
                    {
                        suffixCount++;
                    }
                    else
                    {
                        if (mod.Group != "AfflictionMapDeliriumStacks")
                        {
                            prefixCount++;
                        }
                    }

                    // Find good mods
                    switch (mod.Name)
                    {
                        case "MapDroppedItemRarityIncrease":
                            iir += mod.Values[0];
                            break;
                        case "MapDroppedItemQuantityIncrease":
                            iiq += mod.Values[0];
                            if (mod.Values.Count != 1)
                            {
                                iir += mod.Values[1];
                            }
                            break;
                        case "MapRareMonstersAdditionalModifier":
                            extraRareMod = true;
                            break;
                        case "MapPackSizeIncrease":
                            packSize += mod.Values[0];
                            break;
                        case "MapMagicPackSizeIncrease":
                            magicPackSize += mod.Values[0];
                            break;
                        case "MapTotalEffectivenessIncrease":
                            extraPacks += mod.Values[0];
                            break;
                        case "MapMagicPackIncrease":
                            extraMagicPack += mod.Values[0];
                            break;
                        case "MapMagicRarePackIncrease":
                            extraRarePack += mod.Values[0];
                            if (mod.Values.Count != 1)
                            {
                                extraMagicPack += mod.Values[1];
                            } 
                            break;
                        case "MapRarePackIncrease":
                            extraRarePack += mod.Values[0];
                            break;
                        case string s when s.StartsWith("MapMonsterAdditionalPacks"):
                            additionalPacks += mod.Values[0];
                            break;
                    }
                }

                // Sum the score
                score += iiq * Settings.ScorePerQuantity;
                score += iir * Settings.ScorePerRarity;
                score += packSize * Settings.ScorePerPackSize;
                score += magicPackSize * Settings.ScorePerMagicPackSize;
                score += extraPacks * Settings.ScorePerExtraPacksPercent;
                score += extraMagicPack * Settings.ScorePerExtraMagicPack;
                score += extraRarePack * Settings.ScorePerExtraRarePack;
                score += additionalPacks * Settings.ScorePerAdditionalPack;
                if (extraRareMod)
                {
                    score += Settings.ScoreForExtraRareMonsterModifier;
                }


                // Drawing

                // Frame
                if (hasBannedMod)
                {
                    Graphics.DrawFrame(bbox, Color.DarkRed, 1);
                }
                else
                {
                    if (score >= Settings.MinimumRunHighlightScore)
                    {
                        if (prefixCount < 3)
                        {
                            Graphics.DrawFrame(bbox, Color.Green, 2);

                        }
                        else
                        {
                            Graphics.DrawFrame(bbox, Color.LightGreen, 1);
                        }
                    }
                    else if (score >= Settings.MinimumCraftHighlightScore)
                    {
                        if (prefixCount < 3)
                        {
                            Graphics.DrawFrame(bbox, Color.Yellow, 2);

                        }
                    }
                }
                // Stats
                Graphics.DrawText(iir.ToString(), new Vector2(bbox.Left + 2, bbox.Top));
                Graphics.DrawText(iiq.ToString(), new Vector2(bbox.Left + 2, bbox.Top + 10));
                if (extraRareMod)
                {
                    Graphics.DrawText("+1", new Vector2(bbox.Left + 2, bbox.Top + 20));
                }

                // Affixes count
                Graphics.DrawText(prefixCount.ToString(), new Vector2(bbox.Right + -18, bbox.Top));
                Graphics.DrawText(suffixCount.ToString(), new Vector2(bbox.Right + -18, bbox.Top + 10));

                // Score
                Graphics.DrawText(score.ToString(), new Vector2(bbox.Left + 2, bbox.Bottom - 15));

            }
        }
    }


}