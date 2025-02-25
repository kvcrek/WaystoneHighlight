using ExileCore2.PoEMemory.Components;
using ExileCore2.Shared;
using ItemFilterLibrary;

namespace WaystoneHighlight
{
    internal enum ItemLocation
    {
        Inventory = 0,
        Stash = 1
    }

    internal struct WaystoneItem
    {
        public Base baseComponent;
        public Map map;
        public ItemData.ModsData mods;
        public RectangleF rect;
        public ItemLocation location;

        public WaystoneItem(Base baseComponent, Map mapComponent, ItemData.ModsData modsData, RectangleF rectangleF, ItemLocation location)
        {
            this.baseComponent = baseComponent;
            this.map = mapComponent;
            this.mods = modsData;
            this.rect = rectangleF;
            this.location = location;
        }
    }
}