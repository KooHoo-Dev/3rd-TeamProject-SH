using UnityEngine;

using TMPro;
using UnityEngine;

namespace TeamSH
{
    public class ItemDescriptionUI : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;

        [Header("월드 오프셋")]
        [field: SerializeField] private float worldOffsetY = 0.4f;

        private MartItem target;
        private Camera worldCamera;

        private void Awake()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            worldCamera = Camera.main;
            gameObject.SetActive(false);
        }
        
        private void LateUpdate()
        {
            if (target == null) { Hide(); return; }

            // 추적하는 도중 다른 플레이어가 먼저 집어가서 비활성화된 경우 즉시 끈다.
            if (target.gameObject.activeSelf == false) { Hide(); return; }

            Follow();
        }

        public void Show(MartItem item, TextMeshProUGUI itemCategoryText, TextMeshProUGUI itemNameText)
        {
            if (item == null || item.itemDef == null) { Hide(); return; }

            target = item;
            itemCategoryText.SetText($"{item.itemDef.CategoryType}");
            itemNameText.SetText($"{item.itemDef.ItemName}");
            gameObject.SetActive(true);

            Follow();
        }

        public void Hide()
        {
            target = null;
            gameObject.SetActive(false);
        }

        private void Follow()
        {
            if (worldCamera == null) worldCamera = Camera.main;

            Vector3 worldPos = target.transform.position + Vector3.up * worldOffsetY;
            Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);
            
            rectTransform.position = new Vector3(screenPos.x, screenPos.y, 0f);
        }
    }
}
