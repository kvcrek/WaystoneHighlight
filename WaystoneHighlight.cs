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


namespace WaystoneHighlight;

public class WaystoneHighlight : BaseSettingsPlugin<WaystoneHighlightSettings>
{
    private IngameState InGameState => GameController.IngameState;
    private List<string> BannedModifiers;

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
        //BannedModifiers = ParseBannedModifiers();
        Settings.Score.ReloadBannedModifiers.OnPressed = ParseBannedModifiers;
        ParseBannedModifiers();
        return base.Initialise();
    }
    public override void Render()
    {
        IList<WaystoneItem> waystones = [];

        // Run if inventory panel is opened
        if (InGameState.IngameUi.InventoryPanel.IsVisible)
        {
            // Add map stash items
            if (InGameState.IngameUi.StashElement.VisibleStash.IsVisible)
            {
                if (InGameState.IngameUi.StashElement.VisibleStash.VisibleInventoryItems == null)
                {

                    int stashTier = (int)getOpenMapTierStash().IndexInParent;
                 
                    if (getWaystonesInMapStash(stashTier) != null)
                    {
                        foreach (var item in getWaystonesInMapStash(stashTier))
                        {
                            {
                                if (item.IsVisible)
                                {
                                    waystones.Add(new WaystoneItem(item.Entity.GetComponent<Base>(), item.Entity.GetComponent<Map>(), item.Entity.GetComponent<Mods>(), item.GetClientRectCache, ItemLocation.Stash));
                                }
                            }
                        }

                    }
                }



                // Add inventory items
                var inventoryItems = GameController.IngameState.ServerData.PlayerInventories[0].Inventory.InventorySlotItems;


                foreach (var item in inventoryItems)
                {
                    waystones.Add(new(item.Item.GetComponent<Base>(), item.Item.GetComponent<Map>(), item.Item.GetComponent<Mods>(), item.GetClientRect(), ItemLocation.Inventory));
                }

                foreach (var waystone in waystones)
                {

                    var item = waystone.map;

                    if (item == null)
                        continue;

                    // Check for map tier
                    if (item.Tier < Settings.Score.MinimumTier)
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
                    bool isCorrupted = waystone.baseComponent.isCorrupted;

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
                                if (mod.Values.Count > 0)
                                {
                                    iir += mod.Values[0];
                                }
                                break;
                            case "MapDroppedItemQuantityIncrease":
                                if (mod.Values.Count > 0)
                                {
                                    iiq += mod.Values[0];
                                    if (mod.Values.Count > 1)
                                    {
                                        iir += mod.Values[1];
                                    }
                                }
                                break;
                            case "MapRareMonstersAdditionalModifier":
                                extraRareMod = true;
                                break;
                            case "MapPackSizeIncrease":
                                if (mod.Values.Count > 0)
                                {
                                    packSize += mod.Values[0];
                                }
                                break;
                            case "MapMagicPackSizeIncrease":
                                if (mod.Values.Count > 0)
                                {
                                    magicPackSize += mod.Values[0];
                                }
                                break;
                            case "MapTotalEffectivenessIncrease":
                                if (mod.Values.Count > 0)
                                {
                                    extraPacks += mod.Values[0];
                                }
                                break;
                            case "MapMagicPackIncrease":
                                if (mod.Values.Count > 0)
                                {
                                    extraMagicPack += mod.Values[0];
                                }
                                break;
                            case "MapMagicRarePackIncrease":
                                if (mod.Values.Count > 0)
                                {
                                    extraRarePack += mod.Values[0];
                                }
                                if (mod.Values.Count > 1)
                                {
                                    extraMagicPack += mod.Values[1];
                                }
                                break;
                            case "MapRarePackIncrease":
                                if (mod.Values.Count > 0)
                                {
                                    extraRarePack += mod.Values[0];
                                }
                                break;
                            case string s when s.StartsWith("MapMonsterAdditionalPacks"):
                                if (mod.Values.Count > 0)
                                {
                                    additionalPacks += mod.Values[0];
                                }
                                break;
                        }
                    }

                    // Sum the score
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


                    // Drawing

                    // Frame
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
                        if (score >= Settings.Score.MinimumCraftHighlightScore)
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
                    }

                    if (waystone.location == ItemLocation.Inventory || (waystone.location == ItemLocation.Stash))
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
    }

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

    private void DrawBoxHighlight(RectangleF rect, ColorNode color, int rounding)
    {
        int innerX = (int)rect.X + 1 + (int)(0.5 * rounding);
        int innerY = (int)rect.Y + 1 + (int)(0.5 * rounding);
        int innerWidth = (int)rect.Width - 1 - rounding;
        int innerHeight = (int)rect.Height - 1 - rounding;
        RectangleF scaledBox = new RectangleF(innerX, innerY, innerWidth, innerHeight);
        Graphics.DrawBox(scaledBox, color, rounding);
    }

    private Element getOpenMapTierStash()
    {
        foreach (var stashTab in InGameState.IngameUi.StashElement.VisibleStash.Parent.Children[0].Children[1].Children) {

            if (stashTab.IsVisible)
            {
                return stashTab;
            }
        }
        return null;
    }

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
            LogMessage("Tab Inside tier: "+ (int)tabInsideTier.IndexInParent);
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
                {
                    return tab;
                }
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }

        return null;
    }



}
