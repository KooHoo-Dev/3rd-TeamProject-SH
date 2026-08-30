using Study.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TeamSH
{
    // UICanvas 프리팹 모음. LiarClient를 참조해서 동적으로 변경된다.
    [RequireComponent(typeof(Canvas))]
    public class LiarUICanvasBinder : MonoBehaviour
    {
        private LiarClient client;

        [Header("Timer")] // 제한시간 타이머
        [SerializeField] private GameObject timerPanel;
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Info Overlay (TAB)")] // TAB으로 정보창 보기
        [SerializeField] private GameObject infoOverlayPanel;
        [SerializeField] private TextMeshProUGUI genreText;
        [SerializeField] private TextMeshProUGUI keywordText;
        [SerializeField] private TextMeshProUGUI infoSpecialChatText;
        
        private bool showInfo; // 탭 활성화 상태 확인 bool값

        [Header("Special Chat Log")] // 특수채팅 로그
        [SerializeField] private GameObject specialChatLogPanel;
        [SerializeField] private TextMeshProUGUI specialChatLogText;
        
        [Header("Debate Special Chat Log")] // 변론 종료에서의 특수채팅 로그

        [Header("Liar Out")] // 라밍아웃 버튼
        [SerializeField] private Button liarOutButtonComponent;

        [Header("Final Result")] // 최종 결과 (우승자)
        [SerializeField] private GameObject finalResultPanel;
        [SerializeField] private TextMeshProUGUI finalResultText;

        [Header("Score Tally")] // 점수 목록
        [SerializeField] private GameObject scoreTallyPanel;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Announce")] // 라이어 확정 / 키워드 맞춤 안내
        [SerializeField] private GameObject announcePanel;
        [SerializeField] private TextMeshProUGUI announceText;
        
        [Header("Keyword Chat")] // 키워드 채팅 시각화
        [SerializeField] private GameObject keywordChatPanel;
        [SerializeField] private TextMeshProUGUI keywordChatText;

        private void Awake()
        {
            // 씬이 바뀌어도 안사라짐
            DontDestroyOnLoad(gameObject);

            // 라밍아웃 버튼 클릭 감지
            liarOutButtonComponent.onClick.AddListener(OnLiarOutButtonClicked);
        }

        private void Start()
        {
            client = LiarClient.Instance;
        }

        private void Update()
        {
            if (client == null || client.Me == null) // 클라이언트 or 내가 null이면 false (내가 방에 들어오기 전 상태이면)
            {
                timerPanel.SetActive(false);
                infoOverlayPanel.SetActive(false);
                specialChatLogPanel.SetActive(false);
                liarOutButtonComponent.gameObject.SetActive(false);
                finalResultPanel.SetActive(false);
                scoreTallyPanel.SetActive(false);
                announcePanel.SetActive(false);
                keywordChatPanel.SetActive(false);
                return;
            }

            UpdateTimer();
            UpdateInfoOverlay();
            UpdateSpecialChatLog();
            UpdateLiarOutButton();
            UpdateFinalResult();
            UpdateScoreTally();
            UpdateAnnounce();
            UpdateKeywordChat();
        }

        // 화면 우측 상단에 지금 상태의 남은 시간을 보여준다. 순전히 시각용
        private void UpdateTimer()
        {
            bool show = client.CurrentPhase != "";
            timerPanel.SetActive(show);
            if (show == false) return;

            timerText.text = $"{client.RemainingSeconds:0.0} 초";
        }

        // TAB 를 누르면 화면 정중앙에 장르 / 내 키워드 / 지금까지의 특수채팅을 띄운다. 다시 누르면 닫힌다.
        private void UpdateInfoOverlay()
        {
            bool chatting = GUI.GetNameOfFocusedControl() == "chat"; // 채팅중이면 true

            // 채팅중일땐 안됨 + 탭 누를때마다 bool값 바뀜
            if (chatting == false && SimpleInput.GetKeyDown(Key.Tab)) showInfo = !showInfo; 

            // 
            infoOverlayPanel.SetActive(showInfo); // showinfo가 true일때만 활성화
            if (showInfo == false) return; // false면 리턴

            genreText.text = $"장르 : {client.GenreId}";
            keywordText.text = $"내 키워드 : {client.KeywordId}";
            infoSpecialChatText.text = "특수 채팅 기록\n" + string.Join("\n", client.SpecialChatLog);
        }

        private void UpdateSpecialChatLog() // 특수채팅 업데이트
        {
            bool show = client.CurrentPhase == "발언 종료 상태";
            specialChatLogPanel.SetActive(show); // 발언 종료상태일때만 활성화
            if (show == false) return;

            specialChatLogText.text = string.Join("\n", client.SpecialChatLog);
        }

        private void UpdateDebateSpecialChatLog() // 변론 종료에서의 특수채팅 시각화
        {
            bool show = client.CurrentPhase == "변론 종료 상태";
            
        }
        
        private void UpdateLiarOutButton()
        {
            // 물건 보여주고 발언 상태 + 제한시간이 3초보다 많을 때 라밍아웃 버튼 활성화 
            bool show = client.CurrentPhase == "물건 보여주고 발언 상태" && client.RemainingSeconds > 3f; 
            liarOutButtonComponent.gameObject.SetActive(show);
        }

        private void OnLiarOutButtonClicked() // 라밍아웃 눌럿다고 서버한테 알림
        {
            client.RequestLiarSelfDisclose();
        }

        // 최종 결과 상태 : 우승자 : {우승자 닉네임}
        private void UpdateFinalResult()
        {
            // 최종 결과 상태일때 활성화
            bool show = client.CurrentPhase == "최종 결과 상태";
            finalResultPanel.SetActive(show);
            if (show == false) return;

            finalResultText.text = $"우승자 : {client.GetNickName(client.CurrentOwnerID)}";
        }

        private void UpdateScoreTally()
        {
            // 점수 집계 상태일때 활성화
            bool show = client.CurrentPhase == "점수 집계 상태";
            scoreTallyPanel.SetActive(show);
            if (show == false) return;

            scoreText.text = string.Join("\n", client.GetOurScores());
        }

        // 라이어 확정 상태 / 라이어의 키워드 맞춤 상태일 때, 누가 라이어인지 화면에 띄운다.
        private void UpdateAnnounce()
        {
            bool show = client.CurrentPhase == "라이어 확정 상태" || client.CurrentPhase == "라이어의 키워드 맞춤 상태";
            announcePanel.SetActive(show);
            if (show == false) return;

            if (client.CurrentPhase == "라이어 확정 상태")
            {
                string nick = client.GetNickName(client.CurrentOwnerID);
                bool isLiar = client.LiarId == client.CurrentOwnerID; // 선정된 라이어와 확정된 유저가 같은지 확인
                announceText.text = $"확정된 유저는 {nick} 입니다.\n{nick}는 {(isLiar ? "라이어입니다." : "라이어가 아닙니다.")}";
            }
            else 
            {
                announceText.text = $"라이어는 {client.GetNickName(client.LiarId)} 입니다.";
            }
        }

        // 키워드 맞춤 종료 상태일 때, 라이어가 입력한 키워드채팅을 모두에게 공개한다.
        private void UpdateKeywordChat()
        {
            bool show = client.CurrentPhase == "키워드 맞춤 종료 상태";
            keywordChatPanel.SetActive(show);
            if (show == false) return;

            keywordChatText.text = $"{client.GetNickName(client.LiarId)}의 키워드 선택은 : {client.KeywordChatText}";
        }
    }
}
