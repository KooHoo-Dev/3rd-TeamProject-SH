using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ConferenceScene UI 담당 (조기투표 / 채팅 / 음량조절)
public class ConferenceUIController : MonoBehaviour
{
    [Header("조기투표")]
    [SerializeField] private Button[] voteButtons;

    [Header("채팅")]
    [SerializeField] private RectTransform chatContent;
    [SerializeField] private ScrollRect chatScrollRect;
    [SerializeField] private TMP_InputField chatInput;
    [SerializeField] private GameObject chatMessagePrefab;

    [Header("세팅 버튼")]
    [SerializeField] private Button settingsButton;
    
    [Header("음량조절")]
    [SerializeField] private GameObject volumePanel;

    private void Awake()
    {
        SetupVoteButtons();
        SetupChat();

        volumePanel.SetActive(false); // 처음엔 볼륨 UI 비활성화
        settingsButton.onClick.AddListener(ToggleVolumePanel);
    }

    // 조기투표 버튼 클릭 시 그 버튼의 글자를 로그로 출력
    private void SetupVoteButtons()
    {
        foreach (Button button in voteButtons)
        {
            // 버튼 하위의 TMP_Text 컴포넌트 읽음
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            // 버튼 클릭하면 label 로그로 출력
            button.onClick.AddListener(() => Debug.Log(label.text));
        }
    }

    // 채팅 입력창에서 엔터를 누르면 목록에 메시지 추가
    private void SetupChat()
    {
        // InputField.onSubmit.AddListener : 엔터 누르면 매개변수 실행
        chatInput.onSubmit.AddListener(SubmitChatMessage);
    }

    private void SubmitChatMessage(string message)
    {
        // 채팅 메세지가 없으면 리턴
        if (string.IsNullOrEmpty(message))
            return;

        // 채팅 블록 오브젝트를 프리팹에서 복제
        GameObject messageObj = Instantiate(chatMessagePrefab, chatContent);
        // 채팅 블록의 Text를 매개변수 message로 채움
        messageObj.GetComponent<TMP_Text>().text = message;

        // 채팅 다쓰면 비움
        chatInput.text = "";
    }

    // 설정 버튼 클릭 시 음량조절 패널 토글 (실제 음량 제어는 없음, 구색만)
    private void ToggleVolumePanel()
    {
        // 활성화 상태인지 확인
        if (volumePanel.activeSelf)
            volumePanel.SetActive(false);
        else
            volumePanel.SetActive(true);
    }
}
