using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TeamSH
{
    // 내가 조종하는 캐릭터. 씬에 하나 놓고 RoomClient 에 연결한다.
    // 스폰된 자리에 고정되며 움직이지 않는다.
    public class LocalPlayer : Interaction_Player
    {
        [Header("내부 설정")]
        private PlayerController playerController;
        private ItemInteractionHandler itemInteractionHandler;
        public User User { get; private set; }
        public Transform pointPivot;
        //캐릭터마다 개별캔버스 넣을거 아니라면 오브젝트형 텍스트로 수정해서 사용해야함
        [SerializeField] private TMP_Text label; 


        [Header("모양")]
        [SerializeField] private SpriteRenderer body;
        public Material[] bodyMaterials;

        //[Header("움직임")]
        //public bool CanMove { get; set; } = true;
        //[SerializeField] private float speed = 5f;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            itemInteractionHandler = GetComponent<ItemInteractionHandler>();
        }
        
        public void Setup(User user)
        {
            User = user;
            label.SetText($"{user.NickName} ({user.Id})");
            //bodyMaterials = GetComponentsInChildren<Material>();
            // 씬이 바껴도 삭제 X
            DontDestroyOnLoad(gameObject);
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
            if (CanMove == false) return;

            Keyboard keyboard = Keyboard.current;

            float x = (keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.leftArrowKey.isPressed ? 1 : 0);
            float y = (keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.downArrowKey.isPressed ? 1 : 0);

            transform.Translate(new Vector3(x, y) * (speed * Time.deltaTime));
        }
        */
    }
}
