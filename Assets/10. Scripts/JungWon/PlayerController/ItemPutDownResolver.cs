using UnityEngine;

namespace TeamSH
{
    public class ItemPutDownResolver : MonoBehaviour
    {
        [Header("가방 탐지 설정")]
        [Tooltip("FindItemPivot 주변에서 가방을 찾을 반경")]
        [SerializeField] private float bagDetectRadius = 0.5f;

        [Tooltip("가방 콜라이더 레이어")]
        [SerializeField] private LayerMask bagLayerMask = 0;

        private ItemInteractionHandler interaction;

        private void Awake()
        {
            interaction = GetComponent<ItemInteractionHandler>();
        }
        
        // ItemInteractionHandler.TryPutDownItem 에서 호출함
        public void Resolve(MartItem item, Vector3 checkPivot)
        {
            int itemId = item.itemDef.ItemId;
            
            // 가방 인지 확인
            Collider[] hits = Physics.OverlapSphere(checkPivot, bagDetectRadius, bagLayerMask);
            
            foreach (Collider hit in hits)
            {
                if (hit.GetComponent<BagArea>() != null)
                {
                    Debug.Log($"[ItemPutDownResolver] 가방 감지. 서버에 가방 넣기 요청. ItemId={itemId}");
                    LiarClient.Instance.RequestTryInputItemInBag(itemId);
                    return;
                }
            }
            
            // 원래 매대인지 확인
            DisplayStand currentStand = DisplayStandManager.Instance.CheckWhenStend(checkPivot);
            if (currentStand != null)
            {
                int slotIndex = currentStand.GetItemIndexById(itemId);
                if (slotIndex >= 0)
                {
                    Debug.Log($"[ItemPutDownResolver] 매대 감지. 서버에 매대에 돌려 놓기 요청. ItemId={itemId}");
                    LiarClient.Instance.RequestTryReturnItem(itemId);
                    return;
                }
            }

            Debug.Log("[ItemPutDownResolver] 주변에 원래 매대도 가방도 없어 아이템을 내려놓을 수 없다.");
        }
    }
}