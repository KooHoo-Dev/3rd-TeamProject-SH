using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static TeamSH.Protocol;
using System.Collections.Generic;

namespace TeamSH
{
    // 미팅룸 전용 UI
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    public class MeetingRoomUIManager : MonoBehaviour
    {
        public static MeetingRoomUIManager Instance { get; private set; }

        private LiarClient client;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                liarOutButton.onClick.AddListener(OnLiarOutButtonClicked);
                
                HideAllPanels();
                liarOutButtonResultPanel.SetActive(false);
                scoreTallyPanel.SetActive(false);
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
        }

        private void Update()
        {
            // 라밍아웃 버튼은 "물건 보여주고 발언 상태 + 남은시간 3초 초과 + 아직 안 누름"이라는 조건까지 대기
            bool showLiarOutButton = client.CurrentState == TurnMessageType.ShowItemAndSpeakState
                                      && client.RemainingSeconds > 3f
                                      && client.LiarButtonClicked == false;
            liarOutButton.gameObject.SetActive(showLiarOutButton);
        }

        // 상태가 바뀔 때마다 이전 상태의 공지 패널이 남아있지 않도록 먼저 전부 끈다.
        // 점수 목록만 예외: 키워드 맞춤 종료 ~ 점수 집계 상태에 보여야함
        private void HideAllPanels()
        {
            speakerNoticePanel.SetActive(false);
            liarOutButtonResultPanel.SetActive(false);
            speechEndPanel.SetActive(false);
            pointAtNoticePanel.SetActive(false);
            pointAtResultPanel.SetActive(false);
            debateNoticePanel.SetActive(false);
            debateEndPanel.SetActive(false);
            liarConfirmedPanel.SetActive(false);
            liarGuessingNoticePanel.SetActive(false);
            keywordChatPanel.SetActive(false);
            winOrDefeatPanel.SetActive(false);
            finalResultPanel.SetActive(false);
            questResultPanel.SetActive(false);
        }

        // 점수 목록과 결합해서 보여줘야 하는 지점(ScoreTallyState)에서
        // 그 전 상태의 공지 패널만 정리하고 싶을 때 LiarClient가 호출한다.
        public void ClearTurnNotices() => HideAllPanels();

        #region 물건 보여주고 발언

        [Header("발언자 / 아이템 / 카테고리 공지")]
        [SerializeField] private GameObject speakerNoticePanel;
        [SerializeField] private TextMeshProUGUI speakerNoticeText;

        [Header("라밍아웃 버튼")]
        [SerializeField] private Button liarOutButton;

        public void ShowSpeakerNotice(string speakerNickname, string itemName, string category, int cycle, int maxCycle)
        {
            HideAllPanels();

            speakerNoticePanel.SetActive(true);
            speakerNoticeText.text = $"현재 발언자는 {speakerNickname} 입니다.\n" +
                                      $"{speakerNickname}가 소유한 아이템은 {itemName} 입니다.\n" +
                                      $"현재 카테고리 : {category} (사이클 {cycle} / {maxCycle})";
        }

        private void OnLiarOutButtonClicked()
        {
            LiarClient.Instance.RequestLiarSelfDisclose();
        }

        #endregion

        #region 마트 복귀 (퀘스트 성공 / 실패)

        [Header("퀘스트 결과")]
        [SerializeField] private GameObject questResultPanel;
        [SerializeField] private TextMeshProUGUI questResultText;

        // 마트 복귀 씬 로드가 끝난 뒤 LiarClient가 호출한다.
        public void ShowQuestResult(bool isSuccess)
        {
            HideAllPanels();
            questResultPanel.SetActive(true);
            questResultText.text = isSuccess ? "퀘스트에 성공했습니다!" : "퀘스트에 실패했습니다.";
        }

        #endregion

        #region 라밍아웃 결과 (라밍아웃 버튼 누름 상태 + 라이어 아닌 사람이 눌렀을 때, 두 상태에 걸쳐 겹쳐 보임)

        [Header("라밍아웃 결과")]
        [SerializeField] private GameObject liarOutButtonResultPanel;
        [SerializeField] private TextMeshProUGUI liarOutButtonResultText;

        // 서버가 라이어의 라밍아웃을 전체에 방송했을 때 (OnLiarOutButtonPressedState)
        public void ShowLiarOutAnnounce(string liarNickname)
        {
            HideAllPanels(); // 이전 패널 다 끔
            liarOutButtonResultPanel.SetActive(true);
            liarOutButtonResultText.text = $"라이어 {liarNickname}이(가) 라밍아웃 버튼을 눌렀습니다.";
        }

        // 라이어가 아닌 내가 버튼을 눌렀을 때 (나에게만 즉시 표시, 서버 응답 필요 없음)
        public void ShowNotLiarNotice()
        {
            liarOutButtonResultPanel.SetActive(true);
            liarOutButtonResultText.text = "당신은 라이어가 아닙니다.";
        }

        #endregion

        #region 발언 종료

        [Header("발언 종료 특수채팅 공지")]
        [SerializeField] private GameObject speechEndPanel;
        [SerializeField] private TextMeshProUGUI speechEndText;

        public void ShowSpeechEndNotice(string speakerNickname, string specialChat)
        {
            HideAllPanels();
            speechEndPanel.SetActive(true);
            speechEndText.text = $"{speakerNickname} : {specialChat}";
        }

        #endregion

        #region 지목

        [Header("지목 안내")]
        [SerializeField] private GameObject pointAtNoticePanel;
        [SerializeField] private TextMeshProUGUI pointAtNoticeText;

        [Header("지목 결과")]
        [SerializeField] private GameObject pointAtResultPanel;
        [SerializeField] private TextMeshProUGUI pointAtResultText;

        public void ShowPointAtNotice()
        {
            HideAllPanels();
            pointAtNoticePanel.SetActive(true);
            pointAtNoticeText.text = "라이어로 의심되는 사람을 지목하십시오";
        }

        public void ShowPointAtResult(string mostVotedNickname)
        {
            HideAllPanels();
            pointAtResultPanel.SetActive(true);
            pointAtResultText.text = string.IsNullOrEmpty(mostVotedNickname)
                ? "지목이 스킵되었습니다."
                : $"가장 많이 지목받은 사람은 {mostVotedNickname} 입니다.";
        }

        #endregion

        #region 변론

        [Header("변론 안내")]
        [SerializeField] private GameObject debateNoticePanel;
        [SerializeField] private TextMeshProUGUI debateNoticeText;

        [Header("변론 종료 특수채팅 공지")]
        [SerializeField] private GameObject debateEndPanel;
        [SerializeField] private TextMeshProUGUI debateEndText;

        public void ShowDebateNotice(string debaterNickname)
        {
            HideAllPanels();
            debateNoticePanel.SetActive(true);
            debateNoticeText.text = $"현재 {debaterNickname}가 변론중입니다.";
        }

        public void ShowDebateEndNotice()
        {
            HideAllPanels();
            debateEndPanel.SetActive(true);
            debateEndText.text = "변론이 끝났습니다.";
        }

        #endregion

        #region 라이어 확정 / 라이어의 키워드 맞춤

        [Header("라이어 확정")]
        [SerializeField] private GameObject liarConfirmedPanel;
        [SerializeField] private TextMeshProUGUI liarConfirmedText;

        [Header("라이어 키워드 맞추는 중 안내")]
        [SerializeField] private GameObject liarGuessingNoticePanel;
        [SerializeField] private TextMeshProUGUI liarGuessingNoticeText;

        public void ShowLiarConfirmed(string mostSelectNickname, string liarNickname)
        {
            HideAllPanels();
            liarConfirmedPanel.SetActive(true);

            if (mostSelectNickname == liarNickname) liarConfirmedText.text = $"{mostSelectNickname}는 라이어가 맞습니다";
            else liarConfirmedText.text = $"{mostSelectNickname}은 라이어가 아닙니다.\n라이어는 {liarNickname} 입니다";
        }

        public void ShowLiarGuessingNotice()
        {
            HideAllPanels();
            liarGuessingNoticePanel.SetActive(true);
            liarGuessingNoticeText.text = "라이어가 키워드를 맞추고 있습니다..";
        }

        #endregion

        #region 키워드 맞춤 종료

        [Header("키워드 채팅 시각화")]
        [SerializeField] private GameObject keywordChatPanel;
        [SerializeField] private TextMeshProUGUI keywordChatText;
        [SerializeField] private TextMeshProUGUI liarKeywordText;
        [SerializeField] private TextMeshProUGUI userKeywordText;

        public void ShowKeywordGuessResult(string guessedKeyword, string liarKeyword, string normalKeyword)
        {
            HideAllPanels();
            keywordChatPanel.SetActive(true);
            keywordChatText.text = $"라이어의 키워드 선택은 : {guessedKeyword}";
            liarKeywordText.text = $"라이어의 키워드는 {liarKeyword} 입니다.";
            userKeywordText.text = $"플레이어의 키워드는 {normalKeyword} 입니다.";
        }

        #endregion

        #region 점수 집계 (라이어의 키워드 맞춤 종료 ~ 점수 집계 상태에 걸쳐 겹쳐 보임)

        [Header("점수 목록")]
        [SerializeField] private GameObject scoreTallyPanel;
        [SerializeField] private TextMeshProUGUI scoreTallyText;

        public void ShowScoreTally()
        {
            scoreTallyPanel.SetActive(true);
            scoreTallyText.text = "점수 목록\n" + string.Join("\n", client.GetScoreSorted());
        }

        public void HideScoreTally() => scoreTallyPanel.SetActive(false);

        #endregion

        #region 최종 결과

        [Header("우승 / 패배")]
        [SerializeField] private GameObject winOrDefeatPanel;
        [SerializeField] private TextMeshProUGUI winOrDefeatText;

        [Header("최종 결과 (우승자)")]
        [SerializeField] private GameObject finalResultPanel;
        [SerializeField] private TextMeshProUGUI finalResultText;

        public void ShowWinOrDefeat(bool isWinner)
        {
            HideAllPanels();
            winOrDefeatPanel.SetActive(true);
            winOrDefeatText.text = isWinner ? "승리" : "패배";
        }

        public void ShowFinalResult(List<string> winnerNicknames)
        {
            HideAllPanels();
            finalResultPanel.SetActive(true);
            finalResultText.text = "우승자 : " + string.Join(",", winnerNicknames);
        }

        #endregion
        
    }
}
