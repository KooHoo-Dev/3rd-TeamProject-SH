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
        
        [Header("내부 마테리얼 설정")]
        // 인스펙터에서 색을 입힐 SkinnedMeshRenderer 를 끌어다 놓는다.
        // (스크린샷 기준: Boxer-Stickman-1 / Basic-Stickman 의 SkinnedMeshRenderer)
        public SkinnedMeshRenderer skin;

        // 외부에서 확인할 수 있도록 클램된 머티리얼과 색을 노출
        public Material SkinMaterial { get; private set; }
        public Color PlayerColor { get; private set; } = Color.white;

        private void Awake()
        {
            PlayerController = GetComponent<PlayerController>();
            controller = PlayerController;

            ItemInteractionHandler = GetComponent<ItemInteractionHandler>();
        }
        
        public void Setup(User user)
        {
            User = user;
            label.SetText($"{user.NickName} ({user.Id})");
            DontDestroyOnLoad(gameObject);
        }
        
        public void ApplyColor(Color color)
        {
            PlayerColor = color;
            PlayerColorSet colorSet = LiarClient.Instance?.colorSet;
            colorSet.ApplyColor(skin, color);
            SkinMaterial = skin.material;
        }
    }
}
