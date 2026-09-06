using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static TeamSH.Protocol; 

namespace TeamSH
{
    // 내가 조종하는 캐릭터. 씬에 하나 놓고 RoomClient 에 연결한다.
    // 스폰된 자리에 고정되며 움직이지 않는다.
    public class LocalPlayer : Interaction_Player
    {
        [Header("내부 설정")]
        public PlayerController PlayerController {get; private set;}
        public ItemInteractionHandler ItemInteractionHandler {get; private set;}
        
        public User User { get; private set; }
        public Transform pointPivot;
        //캐릭터마다 개별캔버스 넣을거 아니라면 오브젝트형 텍스트로 수정해서 사용해야함
        [SerializeField] private TMP_Text label; 
        
        [Header("모양")]
        [SerializeField] private SpriteRenderer body;
        public Material[] bodyMaterials;
        

        private void Awake()
        {
            PlayerController = GetComponent<PlayerController>();
            controller = PlayerController;

            ItemInteractionHandler = GetComponent<ItemInteractionHandler>();
        }
        
        public void Setup(User user, Color color)
        {
            User = user;
            label.SetText($"{user.NickName} ({user.Id})");
            
            Debug.Log($"[LocalPlayer]{user.Id},{user.NickName} - {color}");
            DontDestroyOnLoad(gameObject);
        }
    }
}
