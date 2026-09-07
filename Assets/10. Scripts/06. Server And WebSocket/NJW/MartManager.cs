using System.Collections.Generic;
using Study.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TeamSH
{
    public class MartManager : MonoBehaviour
    {
        public static MartManager Instance;
        public MartUIManager martUIManager;
        public Transform[] userSpawnPoints;
        public LocalPlayer player => LiarClient.Instance.Local; //임시 테스트용
        private bool isQInput;
    
        public void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void Update() //테스트용 인풋
        {
            if (SimpleInput.GetKeyDown(Key.Q))
            {
                if (isQInput) isQInput = false;
                else if(isQInput == false) isQInput = true;
            }
            RequestSelectMartItemUI(isQInput, player.SelectedItems);
        }
        
        public void SetMartQuestUI(ItemDef item)
        {
            martUIManager.SetMartQuest(item);
        }

        public void SetGenreAndKeyword(string genre, string keyWord) => martUIManager.SetGenreAndKeyword(genre, keyWord);

        public void SetMartCategoryStand() => martUIManager.SetMartCategoryStand();
        
        public void RequestSelectMartItemUI(bool isShow, List<MartItem> playerItems)
        {
            martUIManager.ApplySelectMartItemUI(isShow, playerItems);
        }
        
        public void RequestItemDescription(bool isShow, MartItem item)
        {
            martUIManager.ApplyItemDescriptionText(isShow, item);
        }
    }
}