using System.Collections.Generic;
using Study.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TeamSH
{
    // 마트/회의실/로비 전체에서 공통으로 쓰는 UI(타이머 / TAB 정보창 / 채팅)를 관리한다.
    // DontDestroyOnLoad로 씬이 바뀌어도 유지된다
    public class CommonUIManager : MonoBehaviour
    {
        public static CommonUIManager Instance { get; private set; }

        private LiarClient client;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                sendButton.onClick.AddListener(OnSendButtonClicked);
                chatInput.onSubmit.AddListener(_ => OnSendButtonClicked());

                timerPanel.SetActive(false);
                infoOverlayPanel.SetActive(false);
                chatGroup.SetActive(false);
                keywordRevealPanel.SetActive(false);
                
                // 볼륨 설정 버튼 추가
                volumeButton.onClick.AddListener(ActiveVolumePanel);
                volumePanel.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            client = LiarClient.Instance;
        }
        
        private void Update()
        {
            if (timerPanel.activeSelf)
            {
                timerText.text = $"{client.RemainingSeconds:0.0} 초";
            }

            if (tabInfoEnabled)
            {
                bool chatting = chatInput != null && chatInput.isFocused;
                showInfo = chatting == false && SimpleInput.GetKey(Key.Tab); // 토글 대신 누르는 동안 true로

                infoOverlayPanel.SetActive(showInfo);
                if (showInfo)
                {
                    itemText.text = "내 아이템들 : \n" + client.GetMyItemList();
                    infoSpecialChatText.text = "특수 채팅 기록\n" + string.Join("\n", client.SpecialChatLog);
                }
            }

            // 채팅창이 떠 있고 아직 입력 포커스가 없을 때 Enter를 누르면 입력창에 포커스를 준다.
            if (chatGroup.activeSelf && chatInput.isFocused == false &&
                (SimpleInput.GetKeyDown(Key.Enter)))
            {
                chatInput.ActivateInputField();
                chatInput.Select();
            }

            volumePanel.SetActive(IsVolumeButtonPressed); // bool값이 true일때만 .SetActive(true)
        }

        #region 볼륨 조절

        [Header("볼륨 조절")] 
        [SerializeField] private Button volumeButton;

        [SerializeField] private GameObject volumePanel;

        private bool IsVolumeButtonPressed = false; // 초기값은 false
        private void ActiveVolumePanel() => IsVolumeButtonPressed = !IsVolumeButtonPressed; // 누를때마다 bool값 바뀜

        #endregion

        #region 타이머

        [Header("타이머")]
        [SerializeField] private GameObject timerPanel;
        [SerializeField] private TextMeshProUGUI timerText;

        public void ShowTimer() => timerPanel.SetActive(true);
        public void HideTimer() => timerPanel.SetActive(false);

        #endregion

        #region TAB 정보창

        [Header("TAB 정보창 (아이템 / 특수채팅 기록)")]
        [SerializeField] private GameObject infoOverlayPanel;
        [SerializeField] private TextMeshProUGUI itemText; // 현재 라운드에 보유한 아이템 표시
        [SerializeField] private TextMeshProUGUI infoSpecialChatText;

        private bool tabInfoEnabled;
        private bool showInfo;

        // 마트 진입 상태 ~ 점수 집계 종료 상태 구간에서만 호출됨
        public void EnableTabInfo() => tabInfoEnabled = true;

        public void DisableTabInfo()
        {
            tabInfoEnabled = false;
            showInfo = false;
            infoOverlayPanel.SetActive(false);
        }

        #endregion

        #region 채팅 (로비 / 회의실 공통, 마트에서는 숨김)

        [Header("채팅")]
        [SerializeField] private GameObject chatGroup;
        [SerializeField] private TextMeshProUGUI chatLogText;
        [SerializeField] private ScrollRect chatScrollRect;
        [SerializeField] private TMP_InputField chatInput;
        [SerializeField] private Button sendButton;

        private readonly List<string> chatLines = new List<string>();
        private const int ChatKeep = 12;

        public void ShowChat()
        {
            chatGroup.SetActive(true);
            RefreshChatText(); // 비활성 상태였던 동안 밀린 렌더를 다시 강제
        }

        public void HideChat()
        {
            chatGroup.SetActive(false);
            chatInput.text = ""; // 입력 중이던 텍스트 삭제
            EventSystem.current?.SetSelectedGameObject(null); // 포커스 해제
        }

        // 방 입장 후 새 채팅 한 줄이 생길 때마다 LiarClient가 직접 호출한다 (폴링 없음).
        public void AppendChatLine(string line)
        {
            chatLines.Add(line);
            if (chatLines.Count > ChatKeep) chatLines.RemoveAt(0);
            RefreshChatText();
        }

        private void RefreshChatText()
        {
            chatLogText.text = string.Join("\n", chatLines);
            chatLogText.ForceMeshUpdate(); // TMP 메시 재생성 

            if (chatScrollRect != null)
            {
                // ContentSizeFitter는 런타임에 갱신이 안 되는 문제가 있어서
                // TMP가 계산해주는 값을 그대로 읽어서 Content 높이를 직접 지정한다.
                RectTransform contentRT = chatScrollRect.content;
                float width = contentRT.rect.width;
                float height = chatLogText.GetPreferredValues(chatLogText.text, width, 0f).y;
                contentRT.sizeDelta = new Vector2(contentRT.sizeDelta.x, height);

                chatScrollRect.verticalNormalizedPosition = 0f;
            }

            Canvas.ForceUpdateCanvases(); // 캔버스 레이아웃/그래픽 즉시 재계산
        }

        private void OnSendButtonClicked()
        {
            if (string.IsNullOrWhiteSpace(chatInput.text)) return;
            client.Say(chatInput.text);
            chatInput.text = "";
        }

        #endregion

        #region 장르 / 키워드 / 라운드 공개 (매 라운드 KeywordDistributeState에서 뜸)

        [Header("장르 / 키워드 / 라운드 공개")]
        [SerializeField] private GameObject keywordRevealPanel;
        [SerializeField] private TextMeshProUGUI keywordRevealText;

        // OnKeywordDistributeState에서 LiarClient가 호출한다.
        public void ShowKeywordReveal(string genre, string keyword, int round, int maxRound)
        {
            keywordRevealPanel.SetActive(true);
            keywordRevealText.text = $"장르 : {genre}\n키워드 : {keyword}\n현재 라운드 : {round} / {maxRound}";
        }

        // 마트 진입(OnMartEnterStateAsync) 시 LiarClient가 호출한다.
        public void HideKeywordReveal() => keywordRevealPanel.SetActive(false);

        #endregion
    }
}
