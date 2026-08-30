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
        public Transform pointPivot;

        // 위치는 안 쓰지만 점수/라이어 여부/아이템은 계속 갱신되어 들어온다.
        public PlayerState PlayerState = new PlayerState();

        public void Setup(User user)
        {
            User = user;
            PlayerState.Id = user.Id;
            label.text = $"{user.NickName} ({user.Id})";

            // 씬이 바껴도 삭제 X
            DontDestroyOnLoad(gameObject);
        }
    }
}