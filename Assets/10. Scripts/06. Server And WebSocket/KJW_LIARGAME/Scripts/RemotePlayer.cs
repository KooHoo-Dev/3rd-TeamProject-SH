using TMPro;
using UnityEngine;

namespace TeamSH
{
    // 남의 캐릭터. 프리팹으로 만들어 두고 RoomClient 가 접속자마다 하나씩 찍어 낸다.
    //   LocalPlayer  : 입력으로 움직인다
    //   RemotePlayer : 받은 자리로 다가간다
    // 스폰된 자리에 고정되며 움직이지 않는다.
    public class RemotePlayer : Interaction_Player
    {
        [Header("내부 설정")]
        private PlayerController playerController;
        private ItemInteractionHandler itemInteractionHandler;
        public User User { get; private set; }
        public Transform pointPivot;
        [SerializeField] private TMP_Text label;
        
        [Header("리모트 설정")]
        // 위치는 안 쓰지만 점수/라이어 여부/아이템은 계속 갱신되어 들어온다.
        public PlayerState PlayerState = new PlayerState();
        // 서버가 알려 준 자리. 네트워크는 이것만 건드리고 그리는 일은 Update 가 한다.
        private Vector2 target;
        // 받은 자리로 얼마나 빨리 다가갈지. 0 이면 그대로 놓는다.
        [SerializeField] private float smooth = 10f;
        
        [Header("모양")]
        [SerializeField] private SpriteRenderer body;
        
        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            itemInteractionHandler = GetComponent<ItemInteractionHandler>();
        }
        
        public void Setup(User user)
        {
            User = user;
            PlayerState.Id = user.Id;
            label.text = $"{user.NickName} ({user.Id})";

            // 처음 나타날 때 원점에서 날아오지 않게 지금 자리를 목표로 둔다.
            target = transform.position;

            // 씬이 바껴도 삭제 X
            DontDestroyOnLoad(gameObject);
        }
        
        public void SetTarget(Vector2 position)
        {
            target = position;
        }
        
        public void SetPlayerWithScenes(string sceneName)
        {
            switch (sceneName)
            {
                case "00. MeetingRoom":
                    playerController.ApplyMeetingPlayer();
                    break;
                case "01. Mart":
                    playerController.ApplyMartPlayer();
                    break;
                default:
                    break;
            }
        }
        
        public override void PushPlayer(Vector3 hitDirection, float power)
        {
            playerController.PushPlayer(hitDirection, power);
        }

        /*
        private void Update()
        {
            if (smooth <= 0f)
            {
                Place(target);
                return;
            }

            // Lerp 에 Time.deltaTime 을 그냥 넣으면 프레임이 빠른 기기에서 더 빨리 붙는다.
            // 이 꼴로 적으면 프레임과 상관없이 같은 속도가 된다.
            float t = 1f - Mathf.Exp(-smooth * Time.deltaTime);
            Place(Vector2.Lerp(transform.position, target, t));
        }

        // 오간 것은 x와 y. z는 건드리지 않음.
        // Vector2 를 position 에 그대로 넣으면 z가 말없이 0이 됨 조심.
        // 스프라이트를 z 로 겹쳐 놓았다면 그 순간 앞뒤가 뒤집히니 조심.
        private void Place(Vector2 next)
        {
            transform.position = new Vector3(next.x, next.y, transform.position.z);
        }
        */
    }
}
