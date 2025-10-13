using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomMapGenerator.TileInfo;

namespace RandomMapGenerator
{
    public class AbstractMapMember
    {
        public string MapUnitName { get; set; } = "empty";
        public bool IsOnMap { get; set; } = false;
        public bool Placed { get; set; } = false;
        public bool NWConnected { get; set; } = false;
        public bool NEConnected { get; set; } = false;
        public bool SEConnected { get; set; } = false;
        public bool SWConnected { get; set; } = false;
        public int Entropy { get; set; } = 50;
        public bool IsAllOnVisibleMap { get; set; } = false;
        public bool PlayerLocationHasTiberium { get; set; } = false;
        public AbstractMapUnit GetAbstractMapUnit()
        {
            var absMapUnit = new AbstractMapUnit();
            foreach (var pAbsMapUnit in WorkingMap.AbstractMapUnitList)
            {
                if (MapUnitName == pAbsMapUnit.MapUnitName)
                {
                    absMapUnit = pAbsMapUnit;
                }
            }
            return absMapUnit;
        }
    }
}
