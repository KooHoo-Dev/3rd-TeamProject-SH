using System.Collections.Generic;
using UnityEngine;

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

        // 데이터 매니저가 다 준비되고 실행해야함!!
        public void AddItems(List<int> items)
        {
            Debug.Log($"[AreaData AddItems] : {items.Count} items");
            DataManager dataManager = DataManager.Instance;
            // List<ItemDef> itemDefs = new(); ?
            for (int i = 0; i < items.Count; i++)
            {
                ItemIdBuffer.Add(items[i]);
                ItemDef itemDef = dataManager.GetItemDef(items[i]);
                Debug.Log($"[AreaData AddItems] : {itemDef.ItemName} / {itemDef.CategoryType}");
                ItemBuffers.Add(itemDef);
            }
        }
    }
}