using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExileCore2.PoEMemory.Components;
using ExileCore2.Shared;

namespace WaystoneHighlight
{
    internal struct WaystoneItem(Map mapComponent, Mods modsComponent, RectangleF rectangleF)
    {
        public Map map = mapComponent;
        public Mods mods = modsComponent;
        public RectangleF rect = rectangleF;
    }
}
