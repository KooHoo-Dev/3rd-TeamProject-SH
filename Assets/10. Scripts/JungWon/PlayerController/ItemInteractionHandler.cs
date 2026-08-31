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
            
            // 1. 플레이어의 현재 위치(target)를 기준으로 어떤 진열대에 들어와 있는지 확인
            DisplayStand currentStand = DisplayStandManager.Instance.CheckWhenStend(Owner.FindItemPivot.position);
            if (currentStand == null)
            {
                Debug.Log("현재 위치에 진열대가 없거나, 물건이 없습니다.");
                return;
            }
            
            MartItem box = currentStand.RequestCreateBox(Owner.ItemHeadPivot.position);
            box.transform.parent = itemBonePivot;
            handItem = box;
            OnStartGetItem.Invoke();
            Debug.Log("물건을 찾았습니다." +
                      $"{box.itemDef.ItemId}\n" +
                      $"{box.itemDef.ItemName}\n" +
                      $"{box.itemDef.CategoryName}\n" +
                      $"{box.itemDef.CategoryType}\n");
        }
        
        public void TryPutDownItem()
        {
            if (putDownItemRequested == false) return;
            putDownItemRequested = false;

            //여기에서 가방 좌표 검사 코드 넣어야함
            handItem.RequestActiveItem();
            Destroy(handItem.gameObject);
            handItem = null;
            OnStartPutDownItem.Invoke();
            Debug.Log("물건을 가방에 넣음");
        }
    }
}