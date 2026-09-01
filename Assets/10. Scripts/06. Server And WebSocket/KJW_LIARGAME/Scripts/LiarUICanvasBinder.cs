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

        [Header("제한시간 타이머")] 
        [SerializeField] private GameObject timerPanel;
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("TAB으로 정보창 보기 / 장르 / 키워드 / 특수채팅 목록")]
        [SerializeField] private GameObject infoOverlayPanel;
        [SerializeField] private TextMeshProUGUI genreText;
        [SerializeField] private TextMeshProUGUI keywordText;
        [SerializeField] private TextMeshProUGUI infoSpecialChatText;
        
        private bool showInfo; // 탭 활성화 상태 확인 bool값

        [Header("특수채팅 로그")] 
        [SerializeField] private GameObject specialChatLogPanel;
        [SerializeField] private TextMeshProUGUI specialChatLogText;
        
        [Header("Debate Special Chat Log")] // 변론 종료에서의 특수채팅 로그

        [Header("라밍아웃 버튼")] 
        [SerializeField] private Button liarOutButtonComponent;
        
        [Header("라밍아웃 버튼 결과 창")]
        [SerializeField] private GameObject liarOutButtonResultPanel;
        [SerializeField] private TextMeshProUGUI liarOutButtonResultText;

        [Header("최종 결과 (우승자)")] 
        [SerializeField] private GameObject finalResultPanel;
        [SerializeField] private TextMeshProUGUI finalResultText;

        [Header("점수 목록")]
        [SerializeField] private GameObject scoreTallyPanel;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("라이어 확정 / 키워드 맞춤 안내")]
        [SerializeField] private GameObject announcePanel;
        [SerializeField] private TextMeshProUGUI announceText;
        
        [Header("키워드 채팅 시각화")] 
        [SerializeField] private GameObject keywordChatPanel;
        [SerializeField] private TextMeshProUGUI keywordChatText; // 라이어가 작성한 키워드채팅
        [SerializeField] private TextMeshProUGUI liarKeyword; // 라이어의 키워드
        [SerializeField] private TextMeshProUGUI userKeyword; // 시민들의 키워드 
        
        [Header("우승 / 패배 시각화")]
        [SerializeField] private GameObject winOrDefeatPanel; 
        [SerializeField] private TextMeshProUGUI winOrDefeatText; // 우승 or 패배
        
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
            // 추후 마트 UI / 회의실 UI와 분리할 필요성!!
            if (client == null || client.Me == null) // 어짜피 client가 싱글톤이라 null이 될수가 읎다...
            {
                timerPanel.SetActive(false);
                infoOverlayPanel.SetActive(false);
                specialChatLogPanel.SetActive(false);
                liarOutButtonComponent.gameObject.SetActive(false);
                finalResultPanel.SetActive(false);
                scoreTallyPanel.SetActive(false);
                announcePanel.SetActive(false);
                keywordChatPanel.SetActive(false);
                finalResultPanel.SetActive(false);
                
                return;
            }

            UpdateTimer(); // 마트 + 회의실 공통 UI
            
            // 아래부터는 회의실 UI
            UpdateInfoOverlay();
            UpdateSpecialChatLog();
            UpdateLiarOutButton();
            UpdateWinOrDefeat();
            UpdateScoreTally();
            UpdateAnnounce();
            UpdateKeywordChat();
            UpdateLiarOutButtonResult();
            UpdateFinalResult(); 
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

            specialChatLogText.text = $"현재 발언자의 특수채팅은 <{client.specialChat}> 입니다";
        }
        
        private void UpdateLiarOutButton()
        {
            // 물건 보여주고 발언 상태 + 제한시간이 3초보다 많을 때 라밍아웃 버튼 활성화
            
            bool show = client.CurrentPhase == "물건 보여주고 발언 상태" && client.RemainingSeconds > 3f && client.LiarButtonClicked == false;

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

            finalResultText.text = $"우승자 : + " + string.Join(",", client.WinnerUserID);
        }

        private void UpdateScoreTally()
        {
            // 라이어의 키워드 맞춤 상태(라운드 종료) or 점수 집계 상태(게임 종료)일 때 점수 보임
            bool show = client.CurrentPhase == "키워드 맞춤 종료 상태" || client.CurrentPhase == "점수 집계 상태" || showInfo;
            scoreTallyPanel.SetActive(show);
            if (show == false) return;
            
            scoreText.text = "점수 목록\n" + string.Join("\n", client.GetScoreSorted());
            
        }
        

        // 확정된 유저의 라이어 판별 (라이어 확정 상태)
        // 라이어가 누군지 공개 (라이어의 키워드 맞춤 상태)
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
            if (client.CurrentPhase == "라이어의 키워드 맞춤 상태")
            {
                announceText.text = $"라이어는 {client.GetNickName(client.LiarId)} 입니다.";
            }
        }

        // 라이어가 입력한 키워드채팅을 모두에게 공개한다. (키워드 맞춤 종료 상태)
        private void UpdateKeywordChat()
        {
            bool show = client.CurrentPhase == "키워드 맞춤 종료 상태";
            keywordChatPanel.SetActive(show);
            if (show == false) return;

            keywordChatText.text = $"{client.GetNickName(client.LiarId)}의 키워드 선택은 : {client.KeywordChatText}";
            liarKeyword.text = $"라이어의 키워드는 {client.liarKeyword} 입니다.";
            userKeyword.text = $"플레이어의 키워드는 {client.nomalKeyword} 입니다.";
        }

        // 라밍아웃 결과창 텍스트 호출 (점수 집계 상태)
        private void UpdateLiarOutButtonResult()
        {
            // 점수 집계 상태일 때 라이어 버튼을 누른 사람에게만 뜨는 텍스트 패널
            bool show = client.CurrentPhase == "점수 집계 상태" && client.LiarButtonClicked; 
            liarOutButtonResultPanel.SetActive(show);
            if (show == false) return;
            
            // 라이어가 라밍아웃 버튼을 눌렀을 때 + 키워드를 맞췄을 때
            if (client.Me.Id == client.LiarId && client.KeywordChatText == client.nomalKeyword)
            {
                liarOutButtonResultText.text = $"라이어가 라밍아웃 버튼을 눌러 점수를 추가 획득하였습니다.";
            }
            // 라이어가 아닌 사람이 라밍아웃 버튼을 눌렀을 때
            if (client.Me.Id != client.LiarId) 
            {
                liarOutButtonResultText.text = $"라이어가 아닌 사람이 라밍아웃 버튼을 눌러 점수가 차감되었습니다";
            }
        }
        
        // 우승 / 패배 출력 (Win / Defeat)
        private void UpdateWinOrDefeat()
        {
            // 내가 라이어고 
            bool show = client.CurrentPhase == "최종 결과 종료 상태";
            winOrDefeatPanel.SetActive(show);
            if (show == false) return;

            bool isWinner = client.WinnerUserID.Contains(client.Me.Id);

            winOrDefeatText.text = (isWinner) ? $"승리" : $"패배";
        }
        
        
    }
}
