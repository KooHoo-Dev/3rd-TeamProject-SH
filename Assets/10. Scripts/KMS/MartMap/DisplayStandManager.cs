using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TeamSH;

public class DisplayStandManager : MonoBehaviour
{
    public static DisplayStandManager Instance { get; private set; }
    
    public Dictionary<CategoryType, AreaNum> CategoryDic = new Dictionary<CategoryType, AreaNum>();
    // AreaData 클래스 분리로 일단 지워봄
    // List<List<DisplayStand>> AllArea = new List<List<DisplayStand>>();
    // List<DisplayStand> Area1 = new List<DisplayStand>();
    // List<DisplayStand> Area2 = new List<DisplayStand>();
    // List<DisplayStand> Area3 = new List<DisplayStand>();
    //
    // List<ItemDef> Area1ItemBuffer = new List<ItemDef>();
    // List<ItemDef> Area2ItemBuffer = new List<ItemDef>();
    // List<ItemDef> Area3ItemBuffer = new List<ItemDef>();
    //
    // List<int> Area1ItemIdBuffer = new List<int>();
    // List<int> Area2ItemIdBuffer = new List<int>();
    // List<int> Area3ItemIdBuffer = new List<int>();

    public List<AreaData> Areas = new();
    
    public bool IsReady { get; private set; } = false;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            Init();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    async void Init()
    {
        AreaData area1 = new AreaData();
        AreaData area2 = new AreaData();
        AreaData area3 = new AreaData();
        area1.AreaNum = AreaNum.Area1;
        area2.AreaNum = AreaNum.Area2;
        area3.AreaNum = AreaNum.Area3;

        Areas.Add(area1);
        Areas.Add(area2);
        Areas.Add(area3);

        SetRandomCategoryForArea();
        await UniTask.WaitUntil(() => DataManager.Instance.IsReady);
        SetItemIdBufferForAreaWithoutServer(); // 임시로 서버 없이 굴러가도록 설정해놓는 함수
        SetShuffledItemDefs();
        IsReady = true;
    }
    public async UniTask RegisterDisplayStand(DisplayStand displayStand)
    {
        await UniTask.WaitUntil(() => IsReady);
        // if (AllArea[(int)displayStand.areaNum].Contains(displayStand) == false)
        // {
        //     AllArea[(int)displayStand.areaNum].Add(displayStand);
        //
        // }
        AreaData area = Areas[(int)displayStand.areaNum];
        if (area.DisplayStands.Contains(displayStand) == false)
        {
            area.DisplayStands.Add(displayStand);
        }
        
        if (CategoryDic.ContainsValue(displayStand.areaNum))
        {
            CategoryType categoryType = CategoryDic.FirstOrDefault(x => x.Value == displayStand.areaNum).Key;
            displayStand.CategoryType = categoryType;
            SetCategoryItemInStand(displayStand); 
        }

        for (int i = 0; i < displayStand.Items.Count; i++)
        {
            Debug.Log($"[DisplayStandManager] : 매대 {displayStand.areaNum} {displayStand?.name} 등록 완료. " +
                      $"상태 : {displayStand?.CategoryType}, 가진 아이템 : {displayStand?.Items[i].ItemName}");
        }
    }
    public void UnregisterDisplayStand(DisplayStand displayStand)
    {
        // if (AllArea[(int)displayStand.areaNum].Contains(displayStand))
        // {
        //     AllArea[(int)displayStand.areaNum].Remove(displayStand);
        // }
        
        AreaData area = Areas[(int)displayStand.areaNum];
        if (area.DisplayStands.Contains(displayStand))
        {
            area.DisplayStands.Remove(displayStand);
        }
    }
    /// <summary>
    /// 포지션이 어떤 스탠드 안에 있는지 모든 스탠드를 검사하고, 있다면 해당 스탠드를 반환합니다. 없다면 null을 반환합니다.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public DisplayStand CheckWhenStend(Vector3 target)
    {
        for (int i = 0; i < Areas.Count; i++)
        {
            for (int j = 0; j < Areas[i].DisplayStands.Count; j++)
            {
                if (Areas[i].DisplayStands[j].IsStay(target))
                {
                    return Areas[i].DisplayStands[j];
                }
            }
        }
        return null;
    }
    public DisplayStand CheckWhenStend(Vector3 origin ,Vector3 target)
    {
        for (int i = 0; i < Areas.Count; i++)
        {
            for (int j = 0; j < Areas[i].DisplayStands.Count; j++)
            {
                if (Areas[i].DisplayStands[j].IsStay(target))
                {
                    return Areas[i].DisplayStands[j];
                }
            }
        }
        return null;
    }
    
    /// <summary>
    ///  원점 (origin)에서 목표(target)까지의 레이를 쏴서, 어떤 스탠드와 충돌하는지 검사합니다. 충돌한다면 해당 스탠드를 반환합니다. 없다면 null을 반환합니다.
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public DisplayStand CheckWhenStandRay(Vector3 origin, Vector3 target)
    {
        for (int i = 0; i < Areas.Count; i++)
        {
            for (int j = 0; j < Areas[i].DisplayStands.Count; j++)
            {
                if (Areas[i].DisplayStands[j].RayCheck(origin, target))
                {
                    return Areas[i].DisplayStands[j];
                }
            }
        }
        return null;
    }
    public void ClearAllDisplayStand()
    {
        for (int i = 0; i < Areas.Count; i++)
        {
            Areas[i].DisplayStands.Clear();
        }
    }
    /// <summary>
    /// 없다면 null을 반환합니다.
    /// </summary>
    /// <param name="categoryType"></param>
    /// <returns></returns>
    public List<DisplayStand> GetDisplayStandByCategory(CategoryType categoryType)
    {
        if (CategoryDic.ContainsKey(categoryType))
        {
            return Areas[(int)CategoryDic[categoryType]].DisplayStands;
        }

        return null;
    }
    // 서버에서 랜덤으로 중복되지 않게 카테고리를 주입해줘야 함
    public void SetCategoryAllArea(CategoryType[] categoryTypes)
    {
        if (categoryTypes.Length != Areas.Count)
        {
            Debug.LogError
                ($"[DisplayStandManager] : SetCategoryAllArea failed. " +
                 $"categoryTypes length {categoryTypes.Length} does not match AllArea count {Areas.Count}.");
            return;
        }
        for (int i = 0; i < categoryTypes.Length; i++)
        {
            CategoryDic[categoryTypes[i]] = (AreaNum)i;
        }
    }
    // 이 로직은 쓸 수 없음
    private void SetRandomCategoryForArea()
    {
        List<CategoryType> OldCategoryTypes = new List<CategoryType>();
        Debug.Log($"[DisplayStandManager] : Setting random categories for {Areas.Count} areas.");

        for (int i = 0; i < Areas.Count; i++)
        {
            CategoryType randomCategory = (CategoryType)Random.Range(1, (int)CategoryType.None);
            if (CategoryDic.ContainsKey(randomCategory))
            {
                int MaxTryCount = 10;
                while (true)
                {
                    if (OldCategoryTypes.Contains(randomCategory) == false)
                    {

                        break;
                    }
                    if (MaxTryCount <= 0)
                    {
                        Debug.LogWarning($"[DisplayStandManager] : MaxTryCount exceeded while trying to find a unique random category for area {i}. Using category {randomCategory}.");
                        break;
                    }
                    randomCategory = (CategoryType)Random.Range(1, (int)CategoryType.None);
                    MaxTryCount--;
                }
            }
            OldCategoryTypes.Add(randomCategory);

            CategoryDic[randomCategory] = (AreaNum)i;

        }
    }
    // 서버에서 랜덤으로 중복되지 않게 아이템을 주입해줘야 함
    // 생각하고 있는 방식은 구역마다 로드된 아이템 갯수만큼
    // 서버에서 List<int>(index 0,1,2...)를 만들어서, 랜덤으로 섞은 후,
    // 이 클래스의 아이템 버퍼의 인덱스로 사용해 순서대로 아이템을 주입하는 방식
    public void SetItemIdBufferForArea(AreaNum areaNum, List<int> itemIds)
    {
        // switch (areaNum)
        // {
        //     case AreaNum.Area1:
        //         Area1ItemIdBuffer = itemIds;
        //         break;
        //     case AreaNum.Area2:
        //         Area2ItemIdBuffer = itemIds;
        //         break;
        //     case AreaNum.Area3:
        //         Area3ItemIdBuffer = itemIds;
        //         break;
        // }
        AreaData area = Areas[(int)areaNum];
        area.ItemIdBuffer = itemIds;
    }

    /// <summary>
    /// 임시로 서버 없이 굴러가도록 설정해놓는 함수
    /// </summary>
    private void SetItemIdBufferForAreaWithoutServer()
    {
        for (int i = 0; i < Areas.Count; i++)
        {
            CategoryType categoryType = CategoryDic.FirstOrDefault(x => x.Value == (AreaNum)i).Key;
            List<ItemDef> itemDefs = DataManager.Instance.GetItemDefsByCategory(categoryType);
            List<int> itemIdBuffer = new List<int>();
            for (int j = 0; j < itemDefs.Count; j++)
            {
                itemIdBuffer.Add(j);
            }
            SetItemIdBufferForArea((AreaNum)i, itemIdBuffer);
        }
    }

    private void SetShuffledItemDefs()
    {
        for (int i = 0; i < Areas.Count; i++)
        {

            CategoryType categoryType = CategoryDic.FirstOrDefault(x => x.Value == (AreaNum)i).Key;
            
            List<ItemDef> itemDefs = DataManager.Instance.GetItemDefsByCategory(categoryType);

            List<ItemDef> shuffledItemDefs = ShuffleList(itemDefs, (AreaNum)i);
            GetItemBufferByArea((AreaNum)i).Clear();
            GetItemBufferByArea((AreaNum)i).AddRange(shuffledItemDefs);

            Debug.Log($"[DisplayStandManager] : Area {(AreaNum)i} shuffled itemDefs set. Count: {GetItemBufferByArea((AreaNum)i).Count}");
        }
    }
    private void SetCategoryItemInStand(DisplayStand displayStand)
    {
        AreaData area = Areas[(int)displayStand.areaNum];

        // 3은 아이템 개수 하드코딩. 추후 수정
        for (int i = 0; i < 3; i++)
        {
            if (area.ItemBuffers.Count <= 0) break;
            displayStand.SetItem(area.ItemBuffers[0]);
            area.ItemBuffers.RemoveAt(0);
        }
        
        // switch (displayStand.areaNum)
        // {
        //     case AreaNum.Area1:
        //     {
        //         if (Area1ItemBuffer.Count <= 0) break;
        //         displayStand.SetItem(Area1ItemBuffer[0]);
        //         Area1ItemBuffer.RemoveAt(0);
        //         break;
        //     }
        //         
        //     case AreaNum.Area2:
        //         if (Area2ItemBuffer.Count > 0)
        //         {
        //             displayStand.SetItem(Area2ItemBuffer[0]);
        //             Area2ItemBuffer.RemoveAt(0);
        //         }
        //         break;
        //     case AreaNum.Area3:
        //         if (Area3ItemBuffer.Count > 0)
        //         {
        //             displayStand.SetItem(Area3ItemBuffer[0]);
        //             Area3ItemBuffer.RemoveAt(0);
        //         }
        //         break;
        // }
    }
    // 이 로직은 서버에서 랜덤으로 섞은 아이템 ID 버퍼를 기반으로, 해당 구역의 아이템 정의 리스트를 섞는 역할을 합니다.
    private List<ItemDef> ShuffleList(List<ItemDef> list, AreaNum areaNum)
    {
        ItemDef temp = new ItemDef();
        List<ItemDef> shuffledList = new List<ItemDef>(list);
        List<int> idBuffer = GetIdBufferByArea(areaNum);
        for (int i = 0; i < idBuffer.Count; i++)
        {
           temp = shuffledList[idBuffer[i]];
            shuffledList[idBuffer[i]] = shuffledList[i];
            shuffledList[i] = temp;
        }
        return shuffledList;
    }
    private List<int> GetIdBufferByArea(AreaNum areaNum)
    {
        AreaData area = Areas[(int)areaNum];
        return area.ItemIdBuffer;
        // switch (areaNum)
        // {
        //     case AreaNum.Area1:
        //         return Area1ItemIdBuffer;
        //     case AreaNum.Area2:
        //         return Area2ItemIdBuffer;
        //     case AreaNum.Area3:
        //         return Area3ItemIdBuffer;
        //     default:
        //         return null;
        // }
    }
    private List<ItemDef> GetItemBufferByArea(AreaNum areaNum)
    {
        AreaData area = Areas[(int)areaNum];
        return area.ItemBuffers;
        // switch (areaNum)
        // {
        //     case AreaNum.Area1:
        //         return Area1ItemBuffer;
        //     case AreaNum.Area2:
        //         return Area2ItemBuffer;
        //     case AreaNum.Area3:
        //         return Area3ItemBuffer;
        //     default:
        //         return null;
        // }
    }
}
