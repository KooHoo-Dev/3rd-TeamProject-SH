using UnityEngine;
using TeamSH;

public class Test : MonoBehaviour
{
    public void GenerateDataSee()
    {
        RoomData room = DataManager.Instance.RoomInput;
        Debug.Log($"방 이름 : {room.RoomName} + 방 비밀번호 :  {room.RoomPass}");
    }
}
