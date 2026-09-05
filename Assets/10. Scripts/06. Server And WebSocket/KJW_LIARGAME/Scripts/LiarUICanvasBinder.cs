/*
using System;
using Study.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static TeamSH.Protocol;

namespace TeamSH
{
    // UICanvas 프리팹 모음. LiarClient를 참조해서 동적으로 변경된다.
    [RequireComponent(typeof(Canvas))]
    public class LiarUICanvasBinder : MonoBehaviour
    {
        private LiarClient client;

        #region 초기화

        private void Awake()
        {
            // 씬이 바뀌어도 안사라짐
            DontDestroyOnLoad(gameObject);

            // 로비 - 입장 폼 / 방 대기 / 채팅
            joinButton.onClick.AddListener(OnJoinButtonClicked);
            gameStartButton.onClick.AddListener(() => client.RequestGameStart());
            sendButton.onClick.AddListener(OnSendButtonClicked);
            chatInput.onSubmit.AddListener(_ => OnSendButtonClicked()); // 엔터로도 전송
            
            // 라밍아웃 버튼 클릭 감지
            liarOutButtonComponent.onClick.AddListener(OnLiarOutButtonClicked);
        }

        private void Start()
        {
            client = LiarClient.Instance;
        }

        private void Update()
        {
            // ===== 로비: 방에 들어가기 전 =====
            if (client.Me == null)
            {
                timerPanel.SetActive(false);

                infoOverlayPanel.SetActive(false);
                liarOutButtonComponent.gameObject.SetActive(false);
                specialChatLogPanel.SetActive(false);
                announcePanel.SetActive(false);
                keywordChatPanel.SetActive(false);
                scoreTallyPanel.SetActive(false);
                liarOutButtonResultPanel.SetActive(false);
                winOrDefeatPanel.SetActive(false);
                finalResultPanel.SetActive(false);
                roomGroup.SetActive(false);

                UpdateJoinGroup();
                return;
            }

            // ===== 로비: 방에 들어간 후 (대기 / 채팅) =====
            joinGroup.SetActive(false);
            UpdateRoomGroup();

            // ===== 마트 + 회의실 공통 =====
            UpdateTimer();

            // ===== 회의실 (턴 흐름 순서) =====
            UpdateInfoOverlay();
            UpdateLiarOutButton();

            UpdateSpecialChatLog();
            UpdateAnnounce();
            UpdateKeywordChat();
            UpdateScoreTally();
            UpdateLiarOutButtonResult();
            UpdateWinOrDefeat();
            UpdateFinalResult();
        }

        #endregion

        #region 로비

        [Header("로비 - 입장 전 (LiarLobbyUIGenerator의 JoinGroup)")]
        [SerializeField] private GameObject joinGroup;
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_InputField roomCodeInput;
        [SerializeField] private Button joinButton;
        [SerializeField] private TextMeshProUGUI joinButtonLabel;

        // 방에 들어가기 전: 이름/방 코드 입력 + 들어가기 버튼 (예전 LiarLobby.DrawLobby)
        private void UpdateJoinGroup()
        {
            joinGroup.SetActive(true);
            joinButton.interactable = client.IsBusy == false;
            joinButtonLabel.text = client.IsBusy ? "들어가는 중…" : "들어가기";
        }

        private void OnJoinButtonClicked()
        {
            client.Join(roomCodeInput.text, nameInput.text);
        }

        [Header("로비 - 입장 후 (LiarLobbyUIGenerator의 RoomGroup)")]
        [SerializeField] private GameObject roomGroup;
        [SerializeField] private TextMeshProUGUI roomInfoLabel;
        [SerializeField] private Button gameStartButton;
        [SerializeField] private TextMeshProUGUI chatLogText;
        [SerializeField] private TMP_InputField chatInput;
        [SerializeField] private Button sendButton;
        [SerializeField] private ScrollRect chatScrollRect; 

        private int lastChatLogCount = -1; 

        // 방에 들어간 후: 방 정보 / 게임시작 버튼 / 채팅 (예전 LiarLobby.DrawRoom)
        // 참고: 아직 씬별로 분리되지 않아서 마트/회의실에서도 계속 보임 (예전 OnGUI와 동일한 동작)
        private void UpdateRoomGroup()
        {
            roomGroup.SetActive(true);
            roomInfoLabel.text = $"방 {client.RoomCode}    나 : {client.Me.NickName}({client.Me.Id})    남 : {client.RemoteCount}명";
            gameStartButton.gameObject.SetActive(client.IsHost && client.IsGameStarted == false);

            if (client.ChatLog.Count != lastChatLogCount)
            {
                lastChatLogCount = client.ChatLog.Count;
                chatLogText.text = string.Join("\n", client.ChatLog);

                if (chatScrollRect != null)
                {
                    RectTransform contentRT = chatScrollRect.content;
                    float width = contentRT.rect.width;
                    float height = chatLogText.GetPreferredValues(chatLogText.text, width, 0f).y;
                    contentRT.sizeDelta = new Vector2(contentRT.sizeDelta.x, height);

                    chatScrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }

        private void OnSendButtonClicked()
        {
            if (string.IsNullOrWhiteSpace(chatInput.text)) return;
            client.Say(chatInput.text);
            chatInput.text = "";
        }

        #endregion

        #region 마트

        [Header("제한시간 타이머")]
        [SerializeField] private GameObject timerPanel;
        [SerializeField] private TextMeshProUGUI timerText;

        // 화면 우측 상단에 지금 상태의 남은 시간을 보여준다. 순전히 시각용. 마트 + 회의실 공통으로 쓰임
        private void UpdateTimer()
        {
            bool show = client.CurrentState != null; // 현재 페이즈가 null이 아니면 계속 보임
            timerPanel.SetActive(show);
            if (show == false) return;

            timerText.text = $"{client.RemainingSeconds:0.0} 초";
        }

        #endregion

        #region 회의실

        [Header("TAB으로 정보창 보기 / 장르 / 키워드 / 특수채팅 목록")]
        [SerializeField] private GameObject infoOverlayPanel;
        [SerializeField] private TextMeshProUGUI genreText;
        [SerializeField] private TextMeshProUGUI keywordText;
        [SerializeField] private TextMeshProUGUI infoSpecialChatText;

        private bool showInfo; // 탭 활성화 상태 확인 bool값

        // TAB 를 누르면 화면 정중앙에 장르 / 내 키워드 / 지금까지의 특수채팅을 띄운다. 다시 누르면 닫힌다.
        // 특정 턴에 종속되지 않고 회의실 전체에서 상시 토글 가능.
        private void UpdateInfoOverlay()
        {
            bool chatting = chatInput != null && chatInput.isFocused; // 채팅중이면 true (예전엔 GUI.GetNameOfFocusedControl 사용, LiarLobby OnGUI 제거로 uGUI 포커스로 대체)

            // 채팅중일땐 안됨 + 탭 누를때마다 bool값 바뀜
            if (chatting == false && SimpleInput.GetKeyDown(Key.Tab)) showInfo = !showInfo;


            infoOverlayPanel.SetActive(showInfo); // showinfo가 true일때만 활성화
            if (showInfo == false) return; // false면 리턴

            genreText.text = $"장르 : {client.Genre}";
            keywordText.text = $"내 키워드 : {client.Keyword}";
            infoSpecialChatText.text = "특수 채팅 기록\n" + string.Join("\n", client.SpecialChatLog);
        }

        [Header("라밍아웃 버튼")]
        [SerializeField] private Button liarOutButtonComponent;

        private void UpdateLiarOutButton()
        {
            // 물건 보여주고 발언 상태 + 제한시간이 3초보다 많을 때 라밍아웃 버튼 활성화
            bool show = client.CurrentState == TurnMessageType.ShowItemAndSpeakState 
                        && client.RemainingSeconds > 3f 
                        && client.LiarButtonClicked == false;

            liarOutButtonComponent.gameObject.SetActive(show);
        }

        private void OnLiarOutButtonClicked() // 라밍아웃 버튼을 눌렀을 때
        {
            client.RequestLiarSelfDisclose(); // 서버로 메세지 보냄
        }

        [Header("특수채팅 로그")]
        [SerializeField] private GameObject specialChatLogPanel;
        [SerializeField] private TextMeshProUGUI specialChatLogText;

        private void UpdateSpecialChatLog() // 특수채팅 업데이트
        {
            // 발언 종료 상태일 때
            bool show = client.CurrentState == TurnMessageType.SpeechEndState;
            specialChatLogPanel.SetActive(show); // 발언 종료상태일때만 활성화
            if (show == false) return;

            specialChatLogText.text = $"현재 발언자의 특수채팅은 <{client.SpecialChat}> 입니다";
        }

        [Header("Debate Special Chat Log")] // 변론 종료에서의 특수채팅 로그

        [Header("라이어 확정 / 키워드 맞춤 안내")]
        [SerializeField] private GameObject announcePanel;
        [SerializeField] private TextMeshProUGUI announceText;

        // 확정된 유저의 라이어 판별 (라이어 확정 상태)
        // 라이어가 누군지 공개 (라이어의 키워드 맞춤 상태)
        private void UpdateAnnounce()
        {
            // 라이어 확정 상태일 때 or 키워드 맞춤 상태일 때
            bool show = client.CurrentState == TurnMessageType.LiarConfirmedState || client.CurrentState == TurnMessageType.LiarKeywordGuessState;
            announcePanel.SetActive(show);
            if (show == false) return;

            // 라이어 확정 상태일 때
            if (client.CurrentState == TurnMessageType.LiarConfirmedState)
            {
                string nick = client.GetNickName(client.CurrentOwnerID);
                bool isLiar = client.LiarId == client.CurrentOwnerID; // 선정된 라이어와 확정된 유저가 같은지 확인
                announceText.text = $"확정된 유저는 {nick} 입니다.\n{nick}는 {(isLiar ? "라이어입니다." : "라이어가 아닙니다.")}";
            }
            // 키워드 맞춤 상태일 때
            if (client.CurrentState == TurnMessageType.LiarKeywordGuessState)
            {
                announceText.text = $"라이어는 {client.GetNickName(client.LiarId)} 입니다.";
            }
        }

        [Header("키워드 채팅 시각화")]
        [SerializeField] private GameObject keywordChatPanel;
        [SerializeField] private TextMeshProUGUI keywordChatText; // 라이어가 작성한 키워드채팅
        [SerializeField] private TextMeshProUGUI liarKeyword; // 라이어의 키워드
        [SerializeField] private TextMeshProUGUI userKeyword; // 시민들의 키워드

        // 라이어가 입력한 키워드채팅을 모두에게 공개한다. (키워드 맞춤 종료 상태)
        private void UpdateKeywordChat()
        {
            bool show = client.CurrentState == TurnMessageType.LiarKeywordGuessEndState;
            keywordChatPanel.SetActive(show);
            if (show == false) return;

            keywordChatText.text = $"{client.GetNickName(client.LiarId)}의 키워드 선택은 : {client.KeywordChatText}";
            liarKeyword.text = $"라이어의 키워드는 {client.LiarKeyword} 입니다."; // 실제 라이어의 키워드
            userKeyword.text = $"플레이어의 키워드는 {client.NormalKeyword} 입니다."; // 실제 시민의 키워드
        }

        [Header("점수 목록")]
        [SerializeField] private GameObject scoreTallyPanel;
        [SerializeField] private TextMeshProUGUI scoreText;

        private void UpdateScoreTally() // 점수 공개
        {
            // 라이어의 키워드 맞춤 종료 상태(라운드 종료)
            // 점수 집계 상태(게임 종료)일 때
            // TAB 누를 때
            bool show = client.CurrentState == TurnMessageType.LiarKeywordGuessEndState || client.CurrentState == TurnMessageType.ScoreTallyState || showInfo;
            scoreTallyPanel.SetActive(show);
            if (show == false) return;

            // 내림차순으로 정렬
            scoreText.text = "점수 목록\n" + string.Join("\n", client.GetScoreSorted());

        }

        [Header("라밍아웃 버튼 결과 창")]
        [SerializeField] private GameObject liarOutButtonResultPanel;
        [SerializeField] private TextMeshProUGUI liarOutButtonResultText;

        // 라밍아웃 결과창 텍스트 호출 (점수 집계 상태)
        private void UpdateLiarOutButtonResult()
        {
            // 라이어가 버튼을 눌렀을 때 + client.CurrentPhase가 라밍아웃 버튼 누름 상태일 떄
            bool isLiarResult =
                client.CurrentState == TurnMessageType.LiarOutButtonPressedState;

            // 물건 보여주고 발언 상태에서 라이어가 아닌 사람이 버튼을 눌렀을 때 (그 사이클 한번만 텍스트 보임)
            bool isNotLiarResult =
                client.CurrentState == TurnMessageType.ShowItemAndSpeakState && client.IsNotLiarPushCycle;

            // 라이어거나 / 라이어가 아닐 때 (버튼을 누른건 동일)
            bool show = isLiarResult || isNotLiarResult;
            liarOutButtonResultPanel.SetActive(show);
            if (show == false) return;

            liarOutButtonResultText.text = isLiarResult
                ? $"라이어 {client.GetNickName(client.LiarId)}이(가) 라밍아웃 버튼을 눌렀습니다."
                : "당신은 라이어가 아닙니다.";
        }

        [Header("우승 / 패배 시각화")]
        [SerializeField] private GameObject winOrDefeatPanel;
        [SerializeField] private TextMeshProUGUI winOrDefeatText; // 우승 or 패배

        // 최종 결과 상태 : 승리 / 패배
        private void UpdateWinOrDefeat()
        {
            bool show = client.CurrentState == TurnMessageType.FinalResultState;
            winOrDefeatPanel.SetActive(show);
            if (show == false) return;

            bool isWinner = client.WinnerUserID.Contains(client.Me.Id);

            winOrDefeatText.text = (isWinner) ? $"승리" : $"패배";
        }

        [Header("최종 결과 (우승자)")]
        [SerializeField] private GameObject finalResultPanel;
        [SerializeField] private TextMeshProUGUI finalResultText;

        // 최종 결과 종료 상태 : 우승자 : ~~~
        private void UpdateFinalResult()
        {
            // 최종 결과 상태일때 활성화
            bool show = client.CurrentState == TurnMessageType.FinalResultEndState;
            finalResultPanel.SetActive(show);
            if (show == false) return;

            finalResultText.text = $"우승자 :" + string.Join(",", client.WinnerUserID);
        }

        #endregion
    }
}
*/
