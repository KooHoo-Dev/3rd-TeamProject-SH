using TMPro;
using UnityEngine;

namespace TeamSH
{
    // 남의 캐릭터. 프리팹으로 만들어 두고 RoomClient 가 접속자마다 하나씩 찍어 낸다.
    // 스폰된 자리에 고정되며 움직이지 않는다.
    public class RemotePlayer : MonoBehaviour
    {
        [Header("모양")]
        [SerializeField] private TMP_Text label;

        public User User { get; private set; }

        public void Setup(User user)
        {
            User = user;
            label.text = $"{user.NickName} ({user.Id})";
        }
    }
}