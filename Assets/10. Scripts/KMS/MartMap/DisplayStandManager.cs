using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TeamSH
{
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

        List<int> Area1ItemIdBuffer = new List<int>();
        List<int> Area2ItemIdBuffer = new List<int>();
        List<int> Area3ItemIdBuffer = new List<int>();
        List<int> Area4ItemIdBuffer = new List<int>();

        public GameObject box; //공용으로 사용될 박스 프리팹 원본
        public bool IsReady { get; private set; } = false;

        #region Unity Methods
        
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

        #endregion
        
        async void Init()
        {
            AllArea.Add(Area1);
            AllArea.Add(Area2);
            AllArea.Add(Area3);
            AllArea.Add(Area4);


            SetRandomCategoryForArea();
            await UniTask.WaitUntil(() => DataManager.Instance.IsReady);
            SetItemIdBufferForAreaWithoutServer(); // 임시로 서버 없이 굴러가도록 설정해놓는 함수
            SetShuffledItemDefs();
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

            Debug.Log(
                $"[DisplayStandManager] : 매대 {displayStand?.name} 등록 완료. 상태 : {displayStand?.CategoryType}, 가진 아이템 : {displayStand?.Item?.ItemName}");
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

        public DisplayStand CheckWhenStend(Vector3 origin, Vector3 target)
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

        // 서버에서 랜덤으로 중복되지 않게 카테고리를 주입해줘야 함
        public void SetCategoryAllArea(CategoryType[] categoryTypes)
        {
            if (categoryTypes.Length != AllArea.Count)
            {
                Debug.LogError(
                    $"[DisplayStandManager] : SetCategoryAllArea failed. categoryTypes length {categoryTypes.Length} does not match AllArea count {AllArea.Count}.");
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
                            Debug.LogWarning(
                                $"[DisplayStandManager] : MaxTryCount exceeded while trying to find a unique random category for area {i}. Using category {randomCategory}.");
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
            switch (areaNum)
            {
                case AreaNum.Area1:
                    Area1ItemIdBuffer = itemIds;
                    break;
                case AreaNum.Area2:
                    Area2ItemIdBuffer = itemIds;
                    break;
                case AreaNum.Area3:
                    Area3ItemIdBuffer = itemIds;
                    break;
                case AreaNum.Area4:
                    Area4ItemIdBuffer = itemIds;
                    break;
            }
        }

        /// <summary>
        /// 임시로 서버 없이 굴러가도록 설정해놓는 함수
        /// </summary>
        private void SetItemIdBufferForAreaWithoutServer()
        {
            for (int i = 0; i < AllArea.Count; i++)
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
            for (int i = 0; i < AllArea.Count; i++)
            {
                CategoryType categoryType = CategoryDic.FirstOrDefault(x => x.Value == (AreaNum)i).Key;

                List<ItemDef> itemDefs = DataManager.Instance.GetItemDefsByCategory(categoryType);

                List<ItemDef> shuffledItemDefs = ShuffleList(itemDefs, (AreaNum)i);
                GetItemBufferByArea((AreaNum)i).Clear();
                GetItemBufferByArea((AreaNum)i).AddRange(shuffledItemDefs);

                Debug.Log(
                    $"[DisplayStandManager] : Area {(AreaNum)i} shuffled itemDefs set. Count: {GetItemBufferByArea((AreaNum)i).Count}");
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
            switch (areaNum)
            {
                case AreaNum.Area1:
                    return Area1ItemIdBuffer;
                case AreaNum.Area2:
                    return Area2ItemIdBuffer;
                case AreaNum.Area3:
                    return Area3ItemIdBuffer;
                case AreaNum.Area4:
                    return Area4ItemIdBuffer;
                default:
                    return null;
            }
        }

        private List<ItemDef> GetItemBufferByArea(AreaNum areaNum)
        {
            switch (areaNum)
            {
                case AreaNum.Area1:
                    return Area1ItmeBuffer;
                case AreaNum.Area2:
                    return Area2ItmeBuffer;
                case AreaNum.Area3:
                    return Area3ItmeBuffer;
                case AreaNum.Area4:
                    return Area4ItmeBuffer;
                default:
                    return null;
            }
        }
    }
}
