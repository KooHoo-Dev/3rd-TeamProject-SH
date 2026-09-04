using System;
using NUnit.Framework;
using UnityEngine;

namespace TeamSH
{
    public class ItemInteractionHandler : MonoBehaviour
    {
        [field: SerializeField] public Interaction_Player Owner { get; private set; }
        private ItemPutDownResolver putDownResolver;

        public MartItem HandItem { get; private set; } //손에 든 아이템
        public Transform itemBonePivot; // 머리좌표
        
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
            putDownResolver = GetComponent<ItemPutDownResolver>();
        }
        
        private void Update()
        {
            TryGetItem();
            TryPutDownItem();
        }
        
        public void RequestGetItem()
        {
            if (getItemRequested || HandItem != null) return;
            getItemRequested = true;
        }

        public void RequestPutDownItem()
        {
            if (putDownItemRequested || HandItem == null) return;
            putDownItemRequested = true;
        }

        public void TryGetItem()
        {
            if (getItemRequested == false) return;
            getItemRequested = false;

            DisplayStand currentStand =
                DisplayStandManager.Instance.CheckWhenStend(Owner.FindItemPivot.position);
            if (currentStand == null)
            {
                Debug.Log("현재 위치에 진열대가 없거나, 물건이 없습니다.");
                return;
            }

            int nearestIndex =
                currentStand.GetNearestActiveItemIndex(Owner.FindItemPivot.position);
            if (nearestIndex < 0)
            {
                Debug.Log("진열대에 남은 물건이 없습니다.");
                return;
            }

            int itemId = currentStand.Items[nearestIndex].ItemId;
            LiarClient.Instance.RequestTryGetItem(itemId); // 주석 해제 + 새 메서드 연결
            Debug.Log($"서버에 {currentStand.Items[nearestIndex].ItemName}을 집고싶다 요청 보냄");
        }
        
        public void TryPutDownItem()
        {
            if (putDownItemRequested == false) return;
            putDownItemRequested = false;

            if (HandItem == null) return; //물건 없으면 비활성화

            putDownResolver.Resolve(HandItem, Owner.FindItemPivot.position);
            
            // 가방에 넣던가 원래자리에 돌려놓던가? 처리를 넣을지도? 일단 가방에 넣는경우만
            //LiarClient.Instance.RequestTryInputItemInBag(HandItem.itemDef.ItemId);
            //Debug.Log($"서버에 {HandItem.itemDef.ItemName}을 가방에 넣고싶다 요청보냄");
        }
        
        // 서버가 아이템 집기 성공시 호출
        public void SuccessGetItem(int itemId)
        {
            if (DisplayStandManager.Instance.TryFindStandByItemId(itemId, out DisplayStand stand, out int index) == false)
            {
                Debug.LogWarning($"[ItemInteraction] itemId {itemId}에 해당하는 매대를 찾지 못했습니다.");
                return;
            }

            MartItem box = stand.RequestCreateBox(Owner.ItemHeadPivot.position, index);

            box.transform.SetParent(itemBonePivot);
            HandItem = box;
            Debug.Log($"아이템 집기 성공!");

            OnStartGetItem?.Invoke();
        }

        // 서버가 가방 넣기 결과를 알려줄 때 호출됨.
        public void SuccessPutDownItemInBag(bool isSuccess, string changedItemId = null)
        {
            if (HandItem == null) return;
            
            if (isSuccess == false)
            {
                //HandItem.RequestActiveItem();
                Debug.Log($"[ItemInteractionHandler] 아이템 가방에 넣기 실패");
                return;
            }
            
            if (string.IsNullOrEmpty(changedItemId) == false)
            {
                if (int.TryParse(changedItemId, out int itemId))
                {
                    DisplayStandManager.Instance.TryFindStandByItemId(itemId, 
                    out DisplayStand itemStand, out int index);

                    itemStand.RequestActiveItem(index);
                    
                    Owner.SelectedObjects.RemoveAll(x => x.itemDef.ItemId == index);
                    Owner.SelectedObjects.Add(HandItem);
                }
                // 아이템 교체 내용 넣기
                Debug.Log($"[아이템 교체] 기존에 가방에 있던 아이템(id:{changedItemId})이 교체되었습니다.");
            }
            else
            {
                Owner.SelectedObjects.Add(HandItem);
            }
            
            ClearHandItem();

            OnStartPutDownItem?.Invoke();
        }

        public void SuccesReturnItemInStand(bool isSuccess)
        {
            if (isSuccess == false) return;
            
            HandItem.RequestActiveItem();

            ClearHandItem();

            OnStartPutDownItem?.Invoke();
        }
        
        public void ClearHandItem()
        {
            if (HandItem == null) return;
            Destroy(HandItem.gameObject);
            HandItem = null;
        }
    }
}