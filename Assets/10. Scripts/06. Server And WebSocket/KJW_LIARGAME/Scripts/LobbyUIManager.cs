using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeamSH
{
    // LIARGAME_Lobby 씬 전용 UI
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    public class LobbyUIManager : MonoBehaviour
    {
        public static LobbyUIManager Instance { get; private set; }

        private LiarClient client;

        private void Awake()
        {
            Instance = this;

            joinButton.onClick.AddListener(OnJoinButtonClicked);
            gameStartButton.onClick.AddListener(() => LiarClient.Instance.RequestGameStart());

            joinGroup.SetActive(true);
            roomGroup.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            client = LiarClient.Instance;

            if (client.Me == null) return; // 아직 입장 전 - Awake의 기본값(입장 폼)을 그대로 둔다

            if (client.IsGameStarted)
            {
                // 게임 진행 중 라운드 사이에 로비 씬을 잠깐 거쳐가는 경우 - 로비 UI를 아예 보여주지 않는다.
                joinGroup.SetActive(false);
                roomGroup.SetActive(false);
            }
            else
            {
                // 이미 입장했고 아직 게임 시작 전(진짜 로비 대기 중)이면 방 대기실을 보여준다.
                ShowRoomGroup();
            }
        }

        private void Update()
        {
            // 입장 요청 중인지(IsBusy)는 네트워크 응답을 기다린다
            if (joinGroup.activeSelf)
            {
                joinButton.interactable = client.IsBusy == false;
                joinButtonLabel.text = client.IsBusy ? "들어가는 중…" : "들어가기";
            }
        }

        #region 입장 전

        [Header("입장 전")]
        [SerializeField] private GameObject joinGroup;
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_InputField roomCodeInput;
        [SerializeField] private Button joinButton;
        [SerializeField] private TextMeshProUGUI joinButtonLabel;

        private void OnJoinButtonClicked()
        {
            client.Join(roomCodeInput.text, nameInput.text);
        }

        #endregion

        #region 방 대기실

        [Header("방 대기실")]
        [SerializeField] private GameObject roomGroup;
        [SerializeField] private TextMeshProUGUI roomInfoLabel;
        [SerializeField] private Button gameStartButton;

        // 방 입장(OnWelcome) 시 LiarClient가 호출한다.
        public void ShowRoomGroup()
        {
            joinGroup.SetActive(false);
            roomGroup.SetActive(true);
            RefreshRoomInfo();
        }

        // 인원 변동(OnJoin/OnLeave) / 게임 시작(OnGameStartOK) 시 LiarClient가 호출한다.
        public void RefreshRoomInfo()
        {
            if (roomGroup.activeSelf == false) return;

            roomInfoLabel.text = $"방 {client.RoomCode}    나 : {client.Me.NickName}({client.Me.Id})    남 : {client.RemoteCount}명";
            gameStartButton.gameObject.SetActive(client.IsHost && client.IsGameStarted == false);
        }

        #endregion
    }
}
