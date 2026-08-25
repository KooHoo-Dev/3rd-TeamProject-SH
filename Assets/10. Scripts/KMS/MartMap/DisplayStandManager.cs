using Study.MiniDefence;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class DisplayStandManager : MonoBehaviour
{
    public static DisplayStandManager Instance { get; private set; }
    List<List<DisplayStand>> AllArea = new List<List<DisplayStand>>();
    public Dictionary<CategoryType, AreaNum> CategoryDic = new Dictionary<CategoryType, AreaNum>();
    List<DisplayStand> Area1 = new List<DisplayStand>();
    List<DisplayStand> Area2 = new List<DisplayStand>();
    List<DisplayStand> Area3 = new List<DisplayStand>();
    List<DisplayStand> Area4 = new List<DisplayStand>();

    List<ItemDef> Area1ItmeBuffer = new List<ItemDef>();
    List<ItemDef> Area2ItmeBuffer = new List<ItemDef>();
    List<ItemDef> Area3ItmeBuffer = new List<ItemDef>();
    List<ItemDef> Area4ItmeBuffer = new List<ItemDef>();
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

        AllArea.Add(Area1);
        AllArea.Add(Area2);
        AllArea.Add(Area3);
        AllArea.Add(Area4);
        SetRandomCategoryForArea();
        await UniTask.WaitUntil(() => DataManager.Instance.IsReady);
        SetItemDefs();
        IsReady = true;
    }
    public async UniTask RegisterDisplayStand(DisplayStand displayStand)
    {
        await UniTask.WaitUntil(() => IsReady);
        if (AllArea[(int)displayStand.areaNum].Contains(displayStand) == false)
        {
            AllArea[(int)displayStand.areaNum].Add(displayStand);

        }
        if (CategoryDic.ContainsValue(displayStand.areaNum))
        {
            CategoryType categoryType = CategoryDic.FirstOrDefault(x => x.Value == displayStand.areaNum).Key;
            displayStand.CategoryType = categoryType;

            SetCategoryItemInStend(displayStand); 
        } 
        Debug.Log($"[DisplayStandManager] : 매대 {displayStand?.name} 등록 완료. 상태: {displayStand?.CategoryType}, 가진 아이템{displayStand?.Item?.ItemName}");
    }
    public void UnregisterDisplayStand(DisplayStand displayStand)
    {
        if (AllArea[(int)displayStand.areaNum].Contains(displayStand))
        {
            AllArea[(int)displayStand.areaNum].Remove(displayStand);
        }
    }
    /// <summary>
    /// 포지션이 어떤 스탠드 안에 있는지 모든 스탠드를 검사하고, 있다면 해당 스탠드를 반환합니다. 없다면 null을 반환합니다.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public DisplayStand CheckWhenStend(Vector3 target)
    {
        for (int i = 0; i < AllArea.Count; i++)
        {
            for (int j = 0; j < AllArea[i].Count; j++)
            {
                if (AllArea[i][j].IsStay(target))
                {
                    return AllArea[i][j];
                }
            }
        }
        return null;
    }
    public DisplayStand CheckWhenStend(Vector3 origin ,Vector3 target)
    {
        for (int i = 0; i < AllArea.Count; i++)
        {
            for (int j = 0; j < AllArea[i].Count; j++)
            {
                if (AllArea[i][j].IsStay(target))
                {
                    return AllArea[i][j];
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
    public DisplayStand CheckWhenStendRay(Vector3 origin, Vector3 target)
    {
        for (int i = 0; i < AllArea.Count; i++)
        {
            for (int j = 0; j < AllArea[i].Count; j++)
            {
                if (AllArea[i][j].RayCheck(origin, target))
                {
                    return AllArea[i][j];
                }
            }
        }
        return null;
    }
    public void ClearAllDisplayStand()
    {
        for (int i = 0; i < AllArea.Count; i++)
        {
            AllArea[i].Clear();
        }
    }
    /// <summary>
    /// 없다면 null을 반환합니다.
    /// </summary>
    /// <param name="categoryType"></param>
    /// <returns></returns>
    public List<DisplayStand> GetDisplayStendByCategory(CategoryType categoryType)
    {
        if (CategoryDic.ContainsKey(categoryType))
        {
            return AllArea[(int)CategoryDic[categoryType]];
        }

        return null;
    }
    private void SetRandomCategoryForArea()
    {
        List<CategoryType> OldCategoryTypes = new List<CategoryType>();
        Debug.Log($"[DisplayStandManager] : Setting random categories for {AllArea.Count} areas.");

        for (int i = 0; i < AllArea.Count; i++)
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
    private void SetItemDefs()
    {
        for (int i = 0; i < AllArea.Count; i++)
        {

            CategoryType categoryType = CategoryDic.FirstOrDefault(x => x.Value == (AreaNum)i).Key;
            
            List<ItemDef> itemDefs = DataManager.Instance.GetItemDefsByCategory(categoryType);

            List<ItemDef> shuffledItemDefs = ShuffleList(itemDefs);
            switch ((AreaNum)i)
            {
                case AreaNum.Area1:
                    Area1ItmeBuffer = shuffledItemDefs;
                    break;
                case AreaNum.Area2:
                    Area2ItmeBuffer = shuffledItemDefs;
                    break;
                case AreaNum.Area3:
                    Area3ItmeBuffer = shuffledItemDefs;
                    break;
                case AreaNum.Area4:
                    Area4ItmeBuffer = shuffledItemDefs;
                    break;
            }

        }
    }
    private void SetCategoryItemInStend(DisplayStand displayStand)
    {
        switch (displayStand.areaNum)
        {
            case AreaNum.Area1:
                if (Area1ItmeBuffer.Count > 0)
                {
                    displayStand.SetItem(Area1ItmeBuffer[0]);
                    Area1ItmeBuffer.RemoveAt(0);
                }
                break;
            case AreaNum.Area2:
                if (Area2ItmeBuffer.Count > 0)
                {
                    displayStand.SetItem(Area2ItmeBuffer[0]);
                    Area2ItmeBuffer.RemoveAt(0);
                }
                break;
            case AreaNum.Area3:
                if (Area3ItmeBuffer.Count > 0)
                {
                    displayStand.SetItem(Area3ItmeBuffer[0]);
                    Area3ItmeBuffer.RemoveAt(0);
                }
                break;
            case AreaNum.Area4:
                if (Area4ItmeBuffer.Count > 0)
                {
                    displayStand.SetItem(Area4ItmeBuffer[0]);
                    Area4ItmeBuffer.RemoveAt(0);

                }
                break;
        }
    }
    private List<T> ShuffleList<T>(List<T> list)
    {
        int random1, random2;
        T temp;

        for (int i = 0; i < list.Count; ++i)
        {
            random1 = Random.Range(0, list.Count);
            random2 = Random.Range(0, list.Count);

            temp = list[random1];
            list[random1] = list[random2];
            list[random2] = temp;
        }

        return list;
    }
}
