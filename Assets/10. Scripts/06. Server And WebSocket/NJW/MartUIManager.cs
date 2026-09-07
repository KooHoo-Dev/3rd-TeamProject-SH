using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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

        [Header("마트 장르/키워드 설정")] 
        public TextMeshProUGUI genreText;
        public TextMeshProUGUI keywordText;

        [Header("매대 카테고리UI 설정")] 
        public GameObject standUI;
        public TextMeshProUGUI[] standUIText;
        
        [Header("퀘스트 UI 설정")]
        public TextMeshProUGUI questDescriptionText; //퀘스트 설명 Text;
        
        [Header("아이템 설명 UI 설정")]
        public ItemDescriptionUI itemDescriptionUI; // 아이템 설명 텍스트 활성화되면 아이템 위에 뜨게할 예정
        public TextMeshProUGUI itemCategoryText;
        public TextMeshProUGUI itemNameText;

        private void Awake()
        {
            categoryText = categorysUI.GetComponentsInChildren<TextMeshProUGUI>();
            itemText = itemsUI.GetComponentsInChildren<TextMeshProUGUI>();
            //standUIText = standUI.GetComponentsInChildren<TextMeshProUGUI>();
        }
        
        public void ApplySelectMartItemUI(bool isShow, List<MartItem> playerItems)
        {
            SetSelectedMartItemUI(playerItems);
            selectMartItemUI.SetActive(isShow);
        }

        public void ApplyItemDescriptionText(bool isShow, MartItem item)
        {
            if (isShow) itemDescriptionUI.Show(item, itemCategoryText, itemNameText);
            else        itemDescriptionUI.Hide();
        }

        private void SetSelectedMartItemUI(List<MartItem> playerItems)
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

        public void SetGenreAndKeyword(string genre, string keyWord)
        {
            genreText.SetText(genre);
            keywordText.SetText(keyWord);
        }

        public void SetMartCategoryStand()
        {
            var categoryDic = DisplayStandManager.Instance.CategoryDic;

            // 0: Area1, 1: Area2, 2: Area3
            for (int i = 0; i < standUIText.Length; i++)
            {
                AreaNum targetArea = (AreaNum)i;

                foreach (var pair in categoryDic)
                {
                    if (pair.Value == targetArea)
                    {
                        standUIText[i].SetText(pair.Key.ToString());
                        break;
                    }
                }
            }
        }

        public void SetMartQuest(ItemDef itemDef) // 마트 퀘스트 정보를 받아서 적용시키기
        {
            questDescriptionText.SetText($"{itemDef.ItemName} 선택 시 \n점수 + 1점"); //여기 +점수값 추가하기
        }
    }
}