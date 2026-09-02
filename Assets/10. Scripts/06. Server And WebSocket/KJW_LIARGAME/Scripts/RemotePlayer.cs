using TMPro;
using UnityEngine;
using static TeamSH.Protocol; 

namespace TeamSH
{
    // 남의 캐릭터. 프리팹으로 만들어 두고 RoomClient 가 접속자마다 하나씩 찍어 낸다.
    //   LocalPlayer  : 입력으로 움직인다
    //   RemotePlayer : 받은 자리로 다가간다
    // 스폰된 자리에 고정되며 움직이지 않는다.
    public class RemotePlayer : Interaction_Player
    {
        [Header("내부 설정")]
        private RemotePlayerController remoteController;
        //private ItemInteractionHandler itemInteractionHandler;
        public User User { get; private set; }
        public Transform pointPivot;
        [SerializeField] private TMP_Text label;
        
        [Header("리모트 설정")]
        // 위치는 안 쓰지만 점수/라이어 여부/아이템은 계속 갱신되어 들어온다.
        public PlayerState PlayerState = new PlayerState();

        // 받은 자리로 얼마나 빨리 다가갈지. 0 이면 그대로 놓는다.
        [SerializeField] private float smooth = 10f;
        
        [Header("모양")]
        [SerializeField] private SpriteRenderer body;
        
        private void Awake()
        {
            remoteController = GetComponent<RemotePlayerController>();
            controller = remoteController;
            //itemInteractionHandler = GetComponent<ItemInteractionHandler>();
        }
        
        public void SetTarget(Vector3 position) => remoteController.SetTarget(position, smooth);
        
        public void Teleport(Vector3 position) => remoteController.Teleport(position);
        
        public void Setup(User user)
        {
            User = user;
            PlayerState.Id = user.Id;
            label.text = $"{user.NickName} ({user.Id})";

            // 처음 나타날 때 원점에서 날아오지 않게 지금 자리를 목표로 둔다.
            //target = transform.position;

            // 씬이 바껴도 삭제 X
            DontDestroyOnLoad(gameObject);
        }
    }
}
