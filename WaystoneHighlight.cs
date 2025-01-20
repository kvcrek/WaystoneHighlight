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
using ExileCore2.Shared.Enums;
using static ItemFilterLibrary.ItemData;

namespace WaystoneHighlight;

public class WaystoneHighlight : BaseSettingsPlugin<WaystoneHighlightSettings>
{
    private IngameState InGameState => GameController?.IngameState;
    private List<string> BannedModifiers;

    private void ParseBannedModifiers()
    {
        if (Settings == null || Settings.Score == null) return;
        if (Settings.Score.BannedModifiers?.Value == null)
        {
            BannedModifiers = new List<string>();
            return;
        }
        BannedModifiers = Settings.Score.BannedModifiers.Value
            .Split(',')
            .Select(x => x.Trim().ToLower())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    public override bool Initialise()
    {
        if (Settings?.Score?.ReloadBannedModifiers == null)
            return false;
        Settings.Score.ReloadBannedModifiers.OnPressed = ParseBannedModifiers; 
        ParseBannedModifiers();
        return base.Initialise();
    }

    public override void Render()
    {
        if (InGameState?.IngameUi == null) return;

        IList<WaystoneItem> waystones = new List<WaystoneItem>();

        var stashPanel = InGameState.IngameUi.StashElement;
        var stashPanelGuild = InGameState.IngameUi.GuildStashElement;
        var inventoryPanel = InGameState.IngameUi.InventoryPanel;
        if (inventoryPanel == null) return;

        bool isQuadTab = false;

        if (inventoryPanel.IsVisible)
        {
            if (stashPanel != null && stashPanel.IsVisible && stashPanel.VisibleStash != null)
            {
                if (stashPanel.VisibleStash.TotalBoxesInInventoryRow == 24)
                {
                    isQuadTab = true;
                }
                var stashItems = stashPanel.VisibleStash.VisibleInventoryItems;
                if (stashItems != null)
                {
                    foreach (var item in stashItems)
                    {
                        if (item?.Item == null) continue;
                        var modsComponent = item.Item.GetComponent<Mods>();
                        if (modsComponent != null)
                        {
                            var modsData = new ModsData(
                                modsComponent.ItemMods,
                                modsComponent.EnchantedMods,
                                modsComponent.ExplicitMods,
                                modsComponent.CorruptionImplicitMods,
                                modsComponent.ImplicitMods,
                                modsComponent.SynthesisMods,
                                modsComponent.CrucibleMods
                            );
                            waystones.Add(new WaystoneItem(
                                item.Item.GetComponent<Base>(),
                                item.Item.GetComponent<Map>(),
                                modsComponent,
                                item.GetClientRectCache,
                                ItemLocation.Stash,
                                modsData
                            ));
                        }
                    }
                }
            }
            else if (stashPanelGuild != null && stashPanelGuild.IsVisible && stashPanelGuild.VisibleStash != null)
            {
                if (stashPanelGuild.VisibleStash.TotalBoxesInInventoryRow == 24)
                {
                    isQuadTab = true;
                }
                var guildStashItems = stashPanelGuild.VisibleStash.VisibleInventoryItems;
                if (guildStashItems != null)
                {
                    foreach (var item in guildStashItems)
                    {
                        if (item?.Item == null) continue;
                        var modsComponent = item.Item.GetComponent<Mods>();
                        if (modsComponent != null)
                        {
                            var modsData = new ModsData(
                                modsComponent.ItemMods,
                                null,
                                null,
                                null,
                                null,
                                null,
                                null
                            );
                            waystones.Add(new WaystoneItem(
                                item.Item.GetComponent<Base>(),
                                item.Item.GetComponent<Map>(),
                                modsComponent,
                                item.GetClientRectCache,
                                ItemLocation.Stash,
                                modsData
                            ));
                        }
                    }
                }
            }

            var playerInventories = InGameState.ServerData?.PlayerInventories;
            if (playerInventories != null && playerInventories.Count > 0)
            {
                var inventory = playerInventories[0]?.Inventory;
                if (inventory?.InventorySlotItems != null)
                {
                    foreach (var item in inventory.InventorySlotItems)
                    {
                        if (item?.Item == null) continue;
                        var modsComponent = item.Item.GetComponent<Mods>();
                        if (modsComponent != null)
                        {
                            var modsData = new ModsData(
                                modsComponent.ItemMods,
                                modsComponent.EnchantedMods,
                                modsComponent.ExplicitMods,
                                modsComponent.CorruptionImplicitMods,
                                modsComponent.ImplicitMods,
                                modsComponent.SynthesisMods,
                                modsComponent.CrucibleMods
                            );
                            waystones.Add(new WaystoneItem(
                                item.Item.GetComponent<Base>(),
                                item.Item.GetComponent<Map>(),
                                modsComponent,
                                item.GetClientRect(),
                                ItemLocation.Inventory,
                                modsData
                            ));
                        }
                    }
                }
            }

            foreach (var waystone in waystones)
            {
                if (waystone.baseComponent == null ||
                    waystone.mods == null ||
                    waystone.modsData == null)
                {
                    continue;
                }

                var itemMap = waystone.map;
                if (itemMap == null) continue;
                if (Settings?.Score == null) continue;

                if (itemMap.Tier < Settings.Score.MinimumTier)
                {
                    continue;
                }

                var itemMods = waystone.mods;
                var bbox = waystone.rect;

                // 改进点：OpenPrefixCount / OpenSuffixCount 如果出现 -1，则直接用 ModsData.Prefixes.Count / ModsData.Suffixes.Count
                int prefixCount = waystone.modsData.Prefixes?.Count() ?? 0;
                int suffixCount = waystone.modsData.Suffixes?.Count() ?? 0;

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

                bool hasBannedMod = false;
                bool isCorrupted = waystone.baseComponent.isCorrupted;
                bool isRare = itemMods.ItemRarity == ItemRarity.Rare;

                if (itemMods.ItemMods != null)
                {
                    foreach (var mod in itemMods.ItemMods)
                    {
                        if (mod == null) continue;
                        if (BannedModifiers != null && BannedModifiers.Count > 0 && mod.DisplayName != null)
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

                        if (mod.Values == null || mod.Values.Count == 0) continue;

                        switch (mod.Name)
                        {
                            case "MapDroppedItemRarityIncrease":
                                iir += mod.Values[0];
                                break;
                            case "MapDroppedItemQuantityIncrease":
                                iiq += mod.Values[0];
                                if (mod.Values.Count > 1)
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
                                if (mod.Values.Count > 1)
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
                {
                    score += Settings.Score.ScoreForExtraRareMonsterModifier;
                }

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
                else
                {
                    if (score >= Settings.Score.MinimumCraftHighlightScoreAboveRare && isRare && prefixCount < 3 && !isCorrupted)
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
                    else if (score >= Settings.Score.MinimumCraftHighlightScore && !isRare && !isCorrupted)
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

                // 在此处显示 prefixCount / suffixCount 到界面上
                if (waystone.location == ItemLocation.Inventory || (waystone.location == ItemLocation.Stash && !isQuadTab))
                {

                    // Stats
                    // SetTextScale doesn't scale well we need to change origin point or add x:y placement modifications depending on scale
                    using (Graphics.SetTextScale(Settings.Graphics.FontSize.QRFontSizeMultiplier))
                    {
                        Graphics.DrawText(iir.ToString(), new Vector2(bbox.Left + 5, bbox.Top), ExileCore2.Shared.Enums.FontAlign.Left);
                        Graphics.DrawText(iiq.ToString(), new Vector2(bbox.Left + 5, bbox.Top + 2 + (10 * Settings.Graphics.FontSize.QRFontSizeMultiplier)), ExileCore2.Shared.Enums.FontAlign.Left);
                        if (extraRareMod)
                        {
                            Graphics.DrawText("+1", new Vector2(bbox.Left + 5, bbox.Top + 4 + (20 * Settings.Graphics.FontSize.QRFontSizeMultiplier)), ExileCore2.Shared.Enums.FontAlign.Left);
                        }
                    }

                    // Affixes count
                    // SetTextScale doesn't scale well we need to change origin point or add x:y placement modifications depending on scale
                    using (Graphics.SetTextScale(Settings.Graphics.FontSize.PrefSuffFontSizeMultiplier))
                    {
                        Graphics.DrawText(prefixCount.ToString(), new Vector2(bbox.Right - 5, bbox.Top), ExileCore2.Shared.Enums.FontAlign.Right);
                        Graphics.DrawText(suffixCount.ToString(), new Vector2(bbox.Right - 5, bbox.Top + 2 + (10 * Settings.Graphics.FontSize.PrefSuffFontSizeMultiplier)), ExileCore2.Shared.Enums.FontAlign.Right);
                    }

                    // Score
                    // SetTextScale doesn't scale well we need to change origin point or add x:y placement modifications depending on scale
                    using (Graphics.SetTextScale(Settings.Graphics.FontSize.ScoreFontSizeMultiplier))
                    {
                        Graphics.DrawText(score.ToString(), new Vector2(bbox.Left + 5, bbox.Bottom - 5 - (15 * Settings.Graphics.FontSize.ScoreFontSizeMultiplier)), ExileCore2.Shared.Enums.FontAlign.Left);
                    }
                }
            }
        }
    }

    private void DrawBorderHighlight(RectangleF rect, ColorNode color, int thickness)
    {
        if (rect == null || color == null) return;
        int scale = thickness - 1;
        int innerX = (int)rect.X + 1 + (int)(0.5 * scale);
        int innerY = (int)rect.Y + 1 + (int)(0.5 * scale);
        int innerWidth = (int)rect.Width - 1 - scale;
        int innerHeight = (int)rect.Height - 1 - scale;
        RectangleF scaledFrame = new RectangleF(innerX, innerY, innerWidth, innerHeight);
        Graphics.DrawFrame(scaledFrame, color, thickness);
    }

    private void DrawBoxHighlight(RectangleF rect, ColorNode color, int rounding)
    {
        if (rect == null || color == null) return;
        int innerX = (int)rect.X + 1 + (int)(0.5 * rounding);
        int innerY = (int)rect.Y + 1 + (int)(0.5 * rounding);
        int innerWidth = (int)rect.Width - 1 - rounding;
        int innerHeight = (int)rect.Height - 1 - rounding;
        RectangleF scaledBox = new RectangleF(innerX, innerY, innerWidth, innerHeight);
        Graphics.DrawBox(scaledBox, color, rounding);
    }
}