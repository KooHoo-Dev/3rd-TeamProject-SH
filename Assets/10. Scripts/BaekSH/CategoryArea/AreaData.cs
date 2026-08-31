using System.Collections.Generic;

namespace TeamSH
{
    /// <summary>
    /// DisplayStandManager의 Area별로 반복되는 구조를 피하기 위해 별도로 짬
    /// </summary>
    public class AreaData
    {
        public AreaNum AreaNum;
        public List<DisplayStand> DisplayStands = new();
        public List<ItemDef> ItemBuffers = new();
        public List<int> ItemIdBuffer = new();
    }
}