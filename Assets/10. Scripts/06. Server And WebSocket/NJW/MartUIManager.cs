using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeamSH
{
    public class MartUIManager : MonoBehaviour
    {
        [Header("선택한 아이템 UI 설정")]
        public GameObject selectMartItemUI; //내가 선택한 아이템 UI

        public GameObject categorysUI;
        public GameObject itemsUI;
        private TextMeshProUGUI[] categoryText; //카테고리 텍스트 배열
        private TextMeshProUGUI[] itemText; // 아이템 텍스트 배열
        
        [Header("퀘스트 UI 설정")]
        public GameObject martQuestUI; //퀘스트 UI
        
        [Header("아이템 UI 설정")]
        public TMP_Text itemDescriptionText; // 아이템 설명 텍스트 활성화되면 아이템 위에 뜨게할 예정

        private void Awake()
        {
            categoryText = categorysUI.GetComponentsInChildren<TextMeshProUGUI>();
            itemText = itemsUI.GetComponentsInChildren<TextMeshProUGUI>();
        }
        
        public void ApplySelectMartItemUI(bool isShow, List<MartItem> playerItems)
        {
            SetMartItemUI(playerItems);
            selectMartItemUI.SetActive(isShow);
        }

        public void ApplyMartQuestUI(bool isShow, MartItem item)
        {
            SetMartQuest(item);
            martQuestUI.SetActive(isShow);
        }

        public void ApplyItemDescriptionText(bool isShow, MartItem item)
        {
            SetItemDescriptionText(item);
            itemDescriptionText.gameObject.SetActive(isShow);
        }

        private void SetMartItemUI(List<MartItem> playerItems)
        {
            //여기 본인이 잡은 아이템 설명 UI 설정변경 넣어야함
            for (int i = 0; i < categoryText.Length; i++)
            {
                categoryText[i].SetText(DisplayStandManager.Instance.categoryTypes[i].ToString());
            }
            
            for (int i = 0; i < itemText.Length; i++)
            {
                itemText[i].SetText("없음");
                for (int j = 0; j < playerItems.Count; j++)
                {
                    if (categoryText[i].text == playerItems[j].itemDef.CategoryType.ToString())
                    {
                        itemText[i].SetText(playerItems[j].itemDef.ItemName);
                    }
                }
            }
        }

        public void SetMartQuest(MartItem item) // 마트 퀘스트 정보를 받아서 적용시키기
        {
            
        }

        private void SetItemDescriptionText(MartItem item)
        {
            
        }
    }
}