using TMPro;
using UnityEngine;

namespace TeamSH
{
    // 내가 조종하는 캐릭터. 씬에 하나 놓고 RoomClient 에 연결한다.
    // 스폰된 자리에 고정되며 움직이지 않는다.
    public class LocalPlayer : MonoBehaviour
    {
        [Header("모양")]
        [SerializeField] private TMP_Text label;

        public Material[] bodyMaterials;
        public Transform pointPivot;

        public void Setup(User user)
        {
            label.text = $"{user.NickName} ({user.Id})";
            //bodyMaterials = GetComponentsInChildren<Material>();
            // 씬이 바껴도 삭제 X
            DontDestroyOnLoad(gameObject);
        }
    }
}