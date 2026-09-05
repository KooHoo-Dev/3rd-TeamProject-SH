using System.Collections.Generic;
using Study.Utilities;
using TMPro;
using UnityEngine;
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
            Instance = this;
            DontDestroyOnLoad(gameObject);

            sendButton.onClick.AddListener(OnSendButtonClicked);
            chatInput.onSubmit.AddListener(_ => OnSendButtonClicked());

            timerPanel.SetActive(false);
            infoOverlayPanel.SetActive(false);
            chatGroup.SetActive(false);
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
                if (chatting == false && SimpleInput.GetKeyDown(Key.Tab)) showInfo = !showInfo;

                infoOverlayPanel.SetActive(showInfo);
                if (showInfo)
                {
                    genreText.text = $"장르 : {client.Genre}";
                    keywordText.text = $"내 키워드 : {client.Keyword}";
                    infoSpecialChatText.text = "특수 채팅 기록\n" + string.Join("\n", client.SpecialChatLog);
                }
            }
        }

        #region 타이머

        [Header("타이머")]
        [SerializeField] private GameObject timerPanel;
        [SerializeField] private TextMeshProUGUI timerText;

        public void ShowTimer() => timerPanel.SetActive(true);
        public void HideTimer() => timerPanel.SetActive(false);

        #endregion

        #region TAB 정보창

        [Header("TAB 정보창 (장르 / 키워드 / 특수채팅 기록)")]
        [SerializeField] private GameObject infoOverlayPanel;
        [SerializeField] private TextMeshProUGUI genreText;
        [SerializeField] private TextMeshProUGUI keywordText;
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

        public void ShowChat() => chatGroup.SetActive(true);
        public void HideChat() => chatGroup.SetActive(false);

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
        }

        private void OnSendButtonClicked()
        {
            if (string.IsNullOrWhiteSpace(chatInput.text)) return;
            client.Say(chatInput.text);
            chatInput.text = "";
        }

        #endregion
    }
}
