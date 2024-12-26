using System.Windows.Forms;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using Newtonsoft.Json;

namespace WaystoneHighlight;

public class WaystoneHighlightSettings : ISettings
{
    //Mandatory setting to allow enabling/disabling your plugin
    public ToggleNode Enable { get; set; } = new ToggleNode(false);

    [Menu("Minimum map tier to highlight")]
    public RangeNode<int> MinimumTier { get; set; } = new RangeNode<int>(1, 1, 16);

    [Menu("Minimum score to highlight map for crafting")]
    public RangeNode<int> MinimumCraftHighlightScore { get; set; } = new RangeNode<int>(20, 0, 1000);

    [Menu("Minimum score to highlight map for running")]
    public RangeNode<int> MinimumRunHighlightScore { get; set; } = new RangeNode<int>(100, 0, 1000);

    [Menu("Score for +1 rare monster modifier")]
    public RangeNode<int> ScoreForExtraRareMonsterModifier { get; set; } = new RangeNode<int>(40, 0, 100);

    [Menu("Score per 1% item rarity in map")]
    public RangeNode<int> ScorePerRarity { get; set; } = new RangeNode<int>(1, 0, 100);
    [Menu("Score per 1% item quantity in map")]
    public RangeNode<int> ScorePerQuantity { get; set; } = new RangeNode<int>(4, 0, 100);

    [Menu("Banned modifiers", "List of mods you don't want to see, separated with ',' \n Locate them by alt-clicking on item and hovering over affix tier on the right")]
    public TextNode BannedModifiers { get; set; } = new TextNode(
        "unwavering, penetration"
    );
    [JsonIgnore]
    public ButtonNode ReloadBannedModifiers { get; set; } = new ButtonNode();

}