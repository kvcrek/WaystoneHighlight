using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExileCore2.PoEMemory.Components;
using ExileCore2.Shared;
using ItemFilterLibrary; // 添加引用

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
        public Mods mods;
        public RectangleF rect;
        public ItemLocation location;
        public ItemData.ModsData modsData; // 添加 ModsData 属性

        public WaystoneItem(Base baseComponent, Map mapComponent, Mods modsComponent, RectangleF rectangleF, ItemLocation location, ItemData.ModsData modsData)
        {
            this.baseComponent = baseComponent;
            this.map = mapComponent;
            this.mods = modsComponent;
            this.rect = rectangleF;
            this.location = location;
            this.modsData = modsData; // 初始化 ModsData 属性
        }
    }
}