using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TeamSH;

public class RoomListController : MonoBehaviour
{
    // 방 목록를 자식으로 삼는 부모 오브젝트
    [SerializeField] private RectTransform content;

    // 방 목록 오브젝트
    [SerializeField] private GameObject itemPrefab;

    [SerializeField] private TMP_InputField RoomPassInput;
    [SerializeField] private ButtonCtrl buttonCtrl;

    private void OnEnable()
    {
        // 씬 켜질때 Refresh 구독
        DataManager.Instance.OnRoomListChanged += Refresh;
        // 최신 상태로
        Refresh();
    }

    private void OnDisable()
    {
        // 씬 꺼지면 구독 해제
        DataManager.Instance.OnRoomListChanged -= Refresh;
    }

    public void Refresh() // 목록 바뀔때마다 계속 초기화
    {
        // 참조 안되있으면 리턴
        if (content == null || itemPrefab == null)
            return;

        ClearItems();

        foreach (RoomData room in DataManager.Instance.RoomList)
            CreateItem(room);
    }

    // 기존 항목 전부 삭제
    private void ClearItems()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
    }

    // 방 하나에 대한 항목 생성 + 텍스트 채우기 + 클릭 리스너 등록
    private void CreateItem(RoomData room)
    {
        GameObject itemObj = Instantiate(itemPrefab, content);
        TMP_Text text = itemObj.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.text = room.RoomName;

        Image image = itemObj.GetComponent<Image>();
        Button button = itemObj.GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(() => SelectItem(room, image));
    }

    // 항목 클릭 시: 선택 저장 + 하이라이트 (선택한 것만 초록, 나머지는 기본색)
    private void SelectItem(RoomData room, Image image)
    {
        // 클릭한 방 SelectRoom에 저장
        DataManager.Instance.SelectRoom(room);

        Color defaultColor = itemPrefab.GetComponent<Image>().color;
        
        // 목록 전체 색상 기본 색상으로
        foreach (Transform child in content)
        {
            Image childImage = child.GetComponent<Image>();
            if (childImage != null)
                childImage.color = defaultColor;
        }

        // 선택한 방은 초록색으로
        image.color = Color.green;
    }

    // 방 선택 + 비밀번호 일치 시에만 참가 확정 후 로비로 이동
    public void JoinSelectedRoom()
    {
        // 선택한 방 데이터 가져오기
        RoomData room = DataManager.Instance.SelectedRoom;
        if (room == null) { Debug.LogWarning("방을 선택하시오"); return; }
        
        // 비밀번호 검증
        if (RoomPassInput.text != room.RoomPass)
        {
            Debug.Log($"비밀번호 틀림 : {RoomPassInput.text}, 정답 : {room.RoomPass}");
            return;
        }

        // 맞으면 선택한 방을 현재 방으로 설정
        Debug.Log($"비밀번호 맞음 - 방 참가: {room.RoomName}");
        DataManager.Instance.JoinRoom(room);
        buttonCtrl.LoadSceneByName("LobyScene");
    }
}
