using TMPro;
using UnityEngine;

public class SaveGenerateInput : MonoBehaviour
{
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_InputField roomPassInput;
    [SerializeField] private ButtonCtrl buttonCtrl;

    // InputField 입력 Datamanager한테 전달
    public void SaveAllInputs(string text)
    {
        DataManager.Instance.RoomInput.RoomName = roomNameInput.text;
        DataManager.Instance.RoomInput.RoomPass = roomPassInput.text;
    }

    // 확인 버튼 : 이름/비밀번호를 모두 입력해야 방 생성 후 로비로 이동
    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameInput.text) || string.IsNullOrEmpty(roomPassInput.text))
        {
            Debug.LogWarning("이름과 비밀번호를 모두 입력하시오");
            return;
        }

        DataManager.Instance.CreateRoom();
        Debug.Log($"방 생성 완료 / 이름 : {roomNameInput.text}");
        buttonCtrl.LoadSceneByName("LobyScene");
    }
}
