using System;
using System.Collections.Generic;
using Jay;

namespace TeamSH
{
    public class RoomManager : SingletonBase<RoomManager>
    {
        // 방 목록 변경시 발생하는 이벤트
        public event Action OnRoomListChanged;

        // 방 생성 인풋 데이터
        public RoomData RoomInput { get; private set; } = new RoomData();

        // 생성된 방 목록
        public List<RoomData> RoomList { get; } = new List<RoomData>();

        // 선택한 방
        public RoomData SelectedRoom { get; private set; }

        // 실제로 입장한 방
        public RoomData CurrentRoom { get; private set; }

        // 현재 입력값을 List에 추가 후 초기화
        public void CreateRoom()
        {
            RoomList.Add(RoomInput);
            RoomInput = new RoomData();
            OnRoomListChanged?.Invoke();
        }

        // 선택된 방을 저장
        public void SelectRoom(RoomData room)
        {
            SelectedRoom = room;
        }

        // 선택된 방을 현재 방으로 확정
        public void JoinRoom(RoomData room)
        {
            CurrentRoom = room;
        }
    }
}
