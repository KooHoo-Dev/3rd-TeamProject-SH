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
            if (Instance == null)
            {
                Instance = this;

                joinButton.onClick.AddListener(OnJoinButtonClicked);
                gameStartButton.onClick.AddListener(() => LiarClient.Instance.RequestGameStart());
                readyButton.onClick.AddListener(OnReadyButtonClicked);
;
                
                gameStartButton.gameObject.SetActive(false);
                readyButton.gameObject.SetActive(false);
                joinGroup.SetActive(true);
                roomGroup.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }

        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            client = LiarClient.Instance;

            if (client.Me == null) return; // 아직 입장 전 - Awake의 기본값(입장 폼)을 그대로 둔다
            
                ShowRoomGroup();


        }

        private void Update()
        {
            // 입장 요청 중인지(IsBusy)는 네트워크 응답을 기다린다
            if (joinGroup.activeSelf)
            {
                joinButton.interactable = client.IsBusy == false;
                joinButtonLabel.text = client.IsBusy ? "들어가는 중…" : "Enter";
            }
            
        }

        #region 입장 전

        [Header("입장 전")]
        [SerializeField] private GameObject joinGroup;
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_InputField roomCodeInput;
        [SerializeField] private Button joinButton;
        [SerializeField] private TextMeshProUGUI joinButtonLabel;
   
        #endregion

        #region 방 대기실

        [Header("방 대기실")]
        [SerializeField] private GameObject roomGroup;
        [SerializeField] private TextMeshProUGUI roomInfoLabel;
        [SerializeField] private Button gameStartButton;
        [SerializeField] private Button readyButton;
        [SerializeField] private TMP_Text readyText;
        [SerializeField] private TMP_Text readyCountText;

        private void OnJoinButtonClicked()
        {
            client.Join(roomCodeInput.text, nameInput.text);
        }

        public void OnReadyButtonClicked()
        {
            readyText.gameObject.SetActive(true);
            readyButton.gameObject.SetActive(false);
            client.RequestReadyMessage();
        }

        public void ActivateGameStartButton(bool isActive)
        {
            gameStartButton.GetComponentInChildren<TextMeshProUGUI>().text =
                (isActive == false) ? $"게임 준비 안됨" : "게임 시작";
            gameStartButton.interactable = isActive;
        }
        public void UpdateReadyCountUI(int readyCount, int allMemberCount)
        {
            readyCountText.text = (client.userOrder.Count < 3)? $"현재 인원이 3명 미만입니다. 3명 이상이 필요합니다." : $"준비 {readyCount} /{allMemberCount}명 ";
        }
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
       

            if (client.IsGameStarted)
            {
                gameStartButton.gameObject.SetActive(false);
                readyText.gameObject.SetActive(false);
                readyCountText.gameObject.SetActive(false);
                readyButton.gameObject.SetActive(false);
            }
            else
            {
                ActivateGameStartButton(false);
                gameStartButton.gameObject.SetActive(client.IsHost);
                readyText.gameObject.SetActive(client.IsReady);
                readyButton.gameObject.SetActive(client.IsHost == false && client.IsReady == false);
                UpdateReadyCountUI(client.ReadyCount, client.userOrder.Count - 1);
                ActivateGameStartButton(client.ReadyCount >= (client.userOrder.Count - 1));
            }
        }

        #endregion
    }
}
