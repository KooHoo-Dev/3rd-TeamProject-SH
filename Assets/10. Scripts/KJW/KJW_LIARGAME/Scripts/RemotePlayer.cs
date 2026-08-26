using TMPro;
using UnityEngine;

namespace KJW_LIARGAME.Scripts
{
    // 남의 캐릭터. 프리팹으로 만들어 두고 RoomClient 가 접속자마다 하나씩 찍어 낸다.
    // 스폰된 자리에 고정되며 움직이지 않는다.
    public class RemotePlayer : MonoBehaviour
    {
        [Header("모양")]
        [SerializeField] private TMP_Text label;

        public User User { get; private set; }

        #region LiarState

        public PlayerState PlayerState
            = new PlayerState() {Id = "ID2", IsLiar = false, Items = new string[3]{"야구공", "물고기", "돈"}};


        #endregion

        public void Setup(User user)
        {
            User = user;
            PlayerState.Id = user.Id;
            label.text = $"{user.NickName} ({user.Id})";
        }
    }
}