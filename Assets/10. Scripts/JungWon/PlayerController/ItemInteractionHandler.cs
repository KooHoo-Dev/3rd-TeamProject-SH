using System;
using UnityEngine;

namespace TeamSH
{
    public class ItemInteractionHandler : MonoBehaviour
    {
        [field: SerializeField] public Interaction_Player Owner { get; private set; }
        
        private MartItem handItem; //손에 든 아이템
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
        }
        
        private void Update()
        {
            TryGetItem();
            TryPutDownItem();
        }
        
        public void RequestGetItem()
        {
            if (getItemRequested || handItem != null) return;
            getItemRequested = true;
        }

        public void RequestPutDownItem()
        {
            if (putDownItemRequested || handItem == null) return;
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
            LiarClient.Instance.RequestTryGetItem(itemId);
            Debug.Log($"서버에 {currentStand.Items[nearestIndex].ItemName}을 집고싶다 요청 보냄");
        }
        
        public void TryPutDownItem()
        {
            if (putDownItemRequested == false) return;
            putDownItemRequested = false;

            if (handItem == null) return; //물건 없으면 비활성화

            // 가방에 넣던가 원래자리에 돌려놓던가? 처리를 넣을지도? 일단 가방에 넣는경우만
            LiarClient.Instance.RequestTryInputItemInBag(handItem.itemDef.ItemId);
            Debug.Log($"서버에 {handItem.itemDef.ItemName}을 가방에 넣고싶다 요청보냄");
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
            handItem = box;
            Debug.Log($"아이템 집기 성공!");

            OnStartGetItem?.Invoke();
        }

        // 서버가 가방 넣기 결과를 알려줄 때 호출됨.
        public void SuccessPutDownItem(bool isSuccess)
        {
            if (handItem == null) return;

            // 실패 시에만 원래 매대 자리를 다시 활성화한다.
            // (성공 시 매대 자리를 다시 켤지 여부는 "가방에 넣은 아이템이 매대에서 영구히 사라지는가"라는
            //  게임 기획 문제이므로, 지금은 원래 테스트 코드의 동작(항상 재활성화)을 유지하려면
            //  이 조건문을 지우고 항상 RequestActiveItem()을 호출하면 됩니다. 서버팀과 확정 필요.)
            if (isSuccess == false) { handItem.RequestActiveItem(); }

            Destroy(handItem.gameObject);
            handItem = null;

            OnStartPutDownItem?.Invoke();
        }
    }
}