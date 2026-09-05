using UnityEngine;

namespace TeamSH
{
    public class MartQuestManager : MonoBehaviour
    {
        public static MartQuestManager Instance;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void SetPlayerMartQuest(string questId)
        {
            Debug.Log($"[MartQuestManager] SetPlayerMartQuest {questId}");
            int questItemId = int.Parse(questId);
            DisplayStandManager.Instance.TryFindStandByItemId(questItemId, out DisplayStand stand, out int itemIndex);
            Debug.Log($"[MartQuestManager] 퀘스트 아이템 아이디 {itemIndex}");
            MartManager.Instance.SetMartQuestUI(stand.Items[itemIndex]);
        }
    }
}