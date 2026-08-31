using System;
using UnityEngine;

namespace TeamSH
{
    public class ItemInteractionHandler : MonoBehaviour
    {
        [field: SerializeField] public Interaction_Player Owner { get; private set; }
        
        [Header("입력 요청 설정")] 
        private bool getItemRequested = false;
        private bool putDownItemRequested = false;
        
        #region 이벤트
        public event Action OnStartGetItem;
        public event Action OnStartPutDownItem;
        #endregion

        private void Awake()
        {
            Owner = GetComponentInParent<Interaction_Player>();
        }
        
        private void Update()
        {
            TryGetItem();
            ApplyPutDownItem();
        }
        
        public void RequestGetItem()
        {
            if (getItemRequested) return;
            getItemRequested = true;
        }

        public void RequestPutDownItem()
        {
            if (putDownItemRequested) return;
            putDownItemRequested = true;
        }
        
        public void TryGetItem() 
        {
            if (getItemRequested == false) return;
            getItemRequested = false;
            
            ItemDef Item = TryGetItemFromStand();
        }
        
        public ItemDef TryGetItemFromStand()
        {
            // 1. 플레이어의 현재 위치(target)를 기준으로 어떤 진열대에 들어와 있는지 확인
            DisplayStand currentStand = DisplayStandManager.Instance.CheckWhenStend(Owner.FindItemPivot.transform.position);
            
            Debug.Log($"아이템 이름 확인" + currentStand?.Items);
            // 2. 플레이어 위치에 진열대가 존재하는지 검사
            if (currentStand != null)
            {
                // 3. 해당 진열대에 배치된 아이템 가져오기
                // 수정 : 아이템을 매대에 여러개 배치하기 위해 DisplayStand에서 Item과 MartItem을 수정해서
                // ItemDef item = currentStand.Item 요 파트만 임의로 바꿔놓았습니다.
                ItemDef item = currentStand.GetNearestItem(Owner.FindItemPivot.transform.position);

                if (item != null)
                {
                    Debug.Log($"[{currentStand.GetNearestItem(Owner.FindItemPivot.transform.position)}] 카테고리의 아이템을 찾았습니다!");
                    return item;
                }
                else
                {
                    Debug.Log("진열대는 존재하지만 아이템이 비어있습니다.");
                }
            }
            else
            {
                Debug.Log("현재 위치에는 어떠한 진열대도 없습니다.");
            }
            
            return null;
        }
        
        public void ApplyPutDownItem()
        {
            if (putDownItemRequested == false) return;
            putDownItemRequested = false;
            
            //여기에서 가방 좌표 검사 코드 넣어야함
        }
    }
}