using System.Windows.Forms;
using System.Drawing;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using Newtonsoft.Json;
using ImGuiNET;

namespace WaystoneHighlight;

public class WaystoneHighlightSettings : ISettings
{
  public ToggleNode Enable { get; set; } = new ToggleNode(false);

    [Menu("Score Features")]
    public ScoreSettings Score { get; set; } = new ScoreSettings();
    
    [Menu("Graphics, Colors, and Performance Settings")]    
    public GraphicSettings Graphics { get; set; } = new GraphicSettings();
}


[Submenu(CollapsedByDefault = false)]
public class ScoreSettings
{
    //Mandatory setting to allow enabling/disabling your plugin
    public ToggleNode Enable { get; set; } = new ToggleNode(false);

    [Menu("Minimum map tier to highlight")]
    public RangeNode<int> MinimumTier { get; set; } = new RangeNode<int>(1, 1, 16);

    [Menu("Minimum score to highlight map for crafting")]
    public RangeNode<int> MinimumCraftHighlightScore { get; set; } = new RangeNode<int>(30, 0, 1000);

    [Menu("Minimum score to highlight map for running")]
    public RangeNode<int> MinimumRunHighlightScore { get; set; } = new RangeNode<int>(160, 0, 1000);

    [Menu("Score for +1 rare monster modifier")]
    public RangeNode<int> ScoreForExtraRareMonsterModifier { get; set; } = new RangeNode<int>(30, 0, 100);

    [Menu("Score per 1% item rarity in map")]
    public RangeNode<int> ScorePerRarity { get; set; } = new RangeNode<int>(2, 0, 100);
    [Menu("Score per 1% item quantity in map")]
    public RangeNode<int> ScorePerQuantity { get; set; } = new RangeNode<int>(8, 0, 100);

    [Menu("Score per 1% pack size")]
    public RangeNode<int> ScorePerPackSize { get; set; } = new RangeNode<int>(2, 0, 100);

    [Menu("Score per 1% magic pack size")]
    public RangeNode<int> ScorePerMagicPackSize { get; set; } = new RangeNode<int>(1, 0, 100);

    [Menu("Score per 1% of extra monster packs")]
    public RangeNode<int> ScorePerExtraPacksPercent { get; set; } = new RangeNode<int>(2, 0, 100);

    [Menu("Score per 1% of extra magic monsters")]
    public RangeNode<int> ScorePerExtraMagicPack { get; set; } = new RangeNode<int>(1, 0, 100);

    [Menu("Score per 1% of increased rare monsters")]
    public RangeNode<int> ScorePerExtraRarePack { get; set; } = new RangeNode<int>(2, 0, 100);

    [Menu("Score per 1 additional pack of X monsters")]
    public RangeNode<int> ScorePerAdditionalPack { get; set; } = new RangeNode<int>(1, 0, 100);

    [Menu("Banned modifiers", "List of mods you don't want to see, separated with ',' \n Locate them by alt-clicking on item and hovering over affix tier on the right")]
    public TextNode BannedModifiers { get; set; } = new TextNode(
        "unwavering, penetration"
    );
    [JsonIgnore]
    public ButtonNode ReloadBannedModifiers { get; set; } = new ButtonNode();

}

[Submenu(CollapsedByDefault = false)]
public class GraphicSettings
{
   [Menu("Runnable Waystone Border Color", "Color of the Border of Runnable Waystones")]
    public ColorNode RunBorderColor { get; set; } = new ColorNode(Color.Green);

   [Menu("Craftable Waystone Border Color", "Color of the Border of Craftable Waystones")]
    public ColorNode CraftBorderColor { get; set; } = new ColorNode(Color.Yellow);

   [Menu("Banned Modifiers Waystone Border Color", "Color of the Border of Waystones with Banned Modifiers")]
    public ColorNode BannedBorderColor { get; set; } = new ColorNode(Color.Red);

}
