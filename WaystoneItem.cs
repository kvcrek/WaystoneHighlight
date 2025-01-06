using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExileCore2.PoEMemory.Components;
using ExileCore2.Shared;

namespace WaystoneHighlight
{
    internal enum ItemLocation
    {
        Inventory = 0,
        Stash = 1
    }
    internal struct WaystoneItem(Base baseComponent, Map mapComponent, Mods modsComponent, RectangleF rectangleF, ItemLocation location)
    {
        public Base baseComponent = baseComponent;
        public Map map = mapComponent;
        public Mods mods = modsComponent;
        public RectangleF rect = rectangleF;
        public ItemLocation location = location;
    }
}
