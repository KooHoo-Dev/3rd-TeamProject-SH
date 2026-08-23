
using System.Collections.Generic;
using System.IO;
using Jay.FileIO;
using UnityEngine;

namespace Study.MiniDefence
{
    // WaveTable은 EnemyDef가 필요하기 때문에
    // 생성시 EnemyTable을 주입반드다.
    public class ItemTable
    {
        private const string TableName = "Table/category_items.tsv";

        private Dictionary<int, ItemDef> itemDefs = new();

        public int Count => itemDefs.Count;
        public ItemDef Get(int i) => itemDefs[i];
        
        public List<ItemDef> GetListAll()
        {
            List<ItemDef> newItems = new List<ItemDef>();
            foreach(var item in itemDefs)
            {
                newItems.Add(item.Value);
            }
            return newItems;
        }
        public void Load()
        {
            itemDefs.Clear();
            // Path클래스의 Combine함수를 이용해서 경로를 이어준다.
            string filePath = Path.Combine(Application.streamingAssetsPath, TableName);
            // 전달드린 TSVReader로 해당 테이블과 DTO 클래스 타입을 넘기면
            // 파싱 쌀먹을 할 수 있습니다. 유용히 쓰십쇼
            List<ItemDTO> rows = TSVReader.ReadTable<ItemDTO>(filePath);
            foreach (ItemDTO dto in rows)
            {
                itemDefs[dto.ItemId] = new ItemDef()
                {
                    ItemId = dto.ItemId,
                    CategoryType = dto.CategoryType,
                    CategoryName = dto.CategoryName,
                    ItemName = dto.ItemName,
                    PrefabKey = dto.PrefabKey,
                };
            }
        }
    }
}