using System.Collections.Generic;
using Study.Utilities;
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
    
        public void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void Update() //테스트용 인풋
        {
            bool isQinput = SimpleInput.GetKey(Key.Q);
            RequestSelectMartItemUI(isQinput, player.SelectedObjects);
        }
        
        public void RequestSelectMartItemUI(bool isShow, List<MartItem> playerItems)
        {
            martUIManager.ApplySelectMartItemUI(isShow, playerItems);
        }

        public void RequestMartQuestUI(bool isShow, MartItem item)
        {
            martUIManager.ApplyMartQuestUI(isShow, item);
        }

        public void RequestItemDescription(bool isShow, MartItem item)
        {
            martUIManager.ApplyItemDescriptionText(isShow, item);
        }
        
        
    }
}