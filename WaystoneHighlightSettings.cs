using System.Windows.Forms;
using System.Drawing;
using ExileCore2;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using Newtonsoft.Json;

namespace WaystoneHighlight;

public class WaystoneHighlightSettings : ISettings
{
  public ToggleNode Enable { get; set; } = new ToggleNode(false);

    [Menu("Score Features")]
    public ScoreSettings Score { get; set; } = new ScoreSettings();
    
    [Menu("Graphics, Colors, and Font Settings")]    
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

    [Menu("Minimum score to highlight map for crafting (above rare)", "Minimum score to highlight map for crafting when prefix count is 3 or more and item is not corrupted")]
    public RangeNode<int> MinimumCraftHighlightScoreAboveRare { get; set; } = new RangeNode<int>(50, 0, 1000);

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
public class GraphicSettings {
    //HIGHLIGHT COLOR
    [Menu("Runnable Waystone Highlight Color", "Highlight color for Runnable Waystones")]
    public ColorNode RunHighlightColor { get; set; } = new ColorNode(Color.Green);

    [Menu("Craftable Waystone Highlight Color", "Highlight color for Craftable Waystones")]
    public ColorNode CraftHighlightColor { get; set; } = new ColorNode(Color.Yellow);

    [Menu("Banned Modifiers Waystone Highlight Color", "Highlight color for Waystones with Banned Modifiers")]
    public ColorNode BannedHighlightColor { get; set; } = new ColorNode(Color.Red);

    //HIGHLIGHT STYLE
    [Menu("Runnable Waystone Highlight Style", "1 = Border; 2 = Filled box/dot")]
    public RangeNode<int> RunHightlightStyle { get; set; } = new RangeNode<int>(1, 1, 2);
    [Menu("Craftable Waystone Highlight Style", "1 = Broder; 2 = Filled box/dot")]
    public RangeNode<int> CraftHightlightStyle { get; set; } = new RangeNode<int>(1, 1, 2);
    [Menu("Banned Waystone Highlight Style", "1 = Border; 2 = Filled box/dot")]
    public RangeNode<int> BannedHightlightStyle { get; set; } = new RangeNode<int>(1, 1, 2);

    [Menu("Border Highlight Thickness Settings")]
    public BorderHighlightSettings BorderHighlight { get; set; } = new BorderHighlightSettings();
    [Menu("Box Highlight Rounding Settings")]
    public BoxHighlightSettings BoxHighlight { get; set; } = new BoxHighlightSettings();
    [Menu("Font Size Settings")]
    public FontSizeSettings FontSize { get; set; } = new FontSizeSettings();
}

[Submenu(CollapsedByDefault = true)]
public class BorderHighlightSettings {
    //BORDER THICKNESS
    [Menu("Runnable Waystone Border Thickness", "Thickness of the Border of Runnable Waystones")]
    public RangeNode<int> RunBorderThickness { get; set; } = new RangeNode<int>(1, 1, 5);

    [Menu("Craftable Waystone Border Thickness", "Thickness of the Border of Craftable Waystones")]
    public RangeNode<int> CraftBorderThickness { get; set; } = new RangeNode<int>(1, 1, 5);

    [Menu("Banned Modifiers Waystone Border Thickness", "Thickness of the Border of Waystones with Banned Modifiers")]
    public RangeNode<int> BannedBorderThickness { get; set; } = new RangeNode<int>(1, 1, 5);
}

[Submenu(CollapsedByDefault = true)]
public class BoxHighlightSettings {
    //BOX ROUNDING
    [Menu("Runnable Waystone Box Rounding", "Rounding of the Box for Runnable Waystones")]
    public RangeNode<int> RunBoxRounding { get; set; } = new RangeNode<int>(1, 1, 60);

    [Menu("Craftable Waystone Box Rounding", "Rounding of the Box for Craftable Waystones")]
    public RangeNode<int> CraftBoxRounding { get; set; } = new RangeNode<int>(1, 1, 60);

    [Menu("Banned Modifiers Waystone Box Rounding", "Rounding of the Box for Waystones with Banned Modifiers")]
    public RangeNode<int> BannedBoxRounding { get; set; } = new RangeNode<int>(1, 1, 60);
}

[Submenu(CollapsedByDefault = true)]
public class FontSizeSettings {
    //FONT SIZE (Needs modifications as text scales from origin point and doesn't change position accordingly)
    [Menu("Waystone Quantity+Rarity Font Size", "Size of the global font for Waystone Scores")]
    public RangeNode<float> QRFontSizeMultiplier { get; set; } = new RangeNode<float>(1.0f, 0.5f, 2f);

    [Menu("Waystone Score Font Size", "Size of the global font for Waystone Scores")]
    public RangeNode<float> ScoreFontSizeMultiplier { get; set; } = new RangeNode<float>(1.0f, 0.5f, 2f);

    [Menu("Waystone Prefix+Suffix Font Size", "Size of the global font for Affix Count")]
    public RangeNode<float> PrefSuffFontSizeMultiplier { get; set; } = new RangeNode<float>(1.0f, 0.5f, 2f);
}
