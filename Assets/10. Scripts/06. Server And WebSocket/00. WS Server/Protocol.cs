using System;
using System.Collections.Generic;

namespace HelloServer
{
    [Serializable]
    public class User
    {
        public string Id;
        public string NickName;
        public int score; // 점수 
    }

    [Serializable]
    public class PlayerState
    {
        public string Id;
        public float X;
        public float Y;
        
        // 추가 코드
        public float Z { get; set; } 
        public bool IsLiar;
        public string[] Items;
    }
    
    // 일반 채팅 모드 (C2S / S2C 공유)
    [Serializable]
    public class NormalChatMessage 
    {
        public string Type = "normalChat";
        public string Text;
        public string ID;
    }
    // 특수 채팅 모드 (C2S / S2C 공유)
    [Serializable]
    public class SpecialChatMessage 
    {
        public string Type = "specialChat";
        public string Text;
        public string ID;
    }
    
    // 투표
    [Serializable]
    public class VoteMessage
    {
        public string Type = ""; // NormalChat or SpecialChat
    
        public int selectID; // 선택한 유저 ID
        public int selectedID; // 선택된 유저 ID
    
        public bool IsVoteCancel; // 투표 취소 여부 bool값 
    }
    
    // GameState 변경사항 주고받기
    [Serializable]
    public class ChangeGameStateMessage // 서버 -> 클라이언트 
    {
        public string Type = ""; // GameState.~~~;
        public float Timer; // 남은 시간
    
        public string currentOwnerID; // 서버에서 선택한 유저
    
        public string Ganre; // 장르
    
        public Dictionary <int, string> Cycles; // int : 현재 사이클, string : 현재 카테고리
    }
    
    // 라이어 버튼 누름 감지
    [Serializable]
    public class PressLiarButtonMessage // 클라이언트 -> 서버
    {
        public User User;
    }

    // 게임 상태
    [Serializable]
    public class GameState
    {
        public string state1 = "마트에서 복귀";
    
        public string state2 = "물건 보여주고 발언";
        public string state3 = "발언 종료";
    
        public string state4 = "지목";
        public string state5 = "지목 종료";
    
        public string state6 = "라밍아웃 버튼 누름";
    
        public string state7 = "변론 시간";
        public string state8 = "변론 종료";
    
        public string state9 = "투표";
        public string state10 = "투표 종료";
    
        public string state11 = "라이어 확정";
        public string state12 = "라이어의 키워드 맞춤";
        public string state13 = "키워드 맞춤 종료";

        public string state14 = "점수 집계";
        public string state15 = "점수 집계 종료";
        public string state16 = "마트 이동"; // 최종 사이클 전

        public string state17 = "최종 결과";
        public string state18 = "최종 결과 종료";
    }
    

    [Serializable]
    public class TypeOnly
    {
        public string Type;
    }

    #region 클라이언트 -> 서버

    [Serializable]
    public class HelloMessage
    {
        public string Type = "hello";
        public string NickName;
    }

    [Serializable]
    public class MoveMessage
    {
        public string Type = "move";
        public string Id;
        public float X;
        public float Y;
    }

    #endregion

    #region 서버 -> 클라이언트

    [Serializable]
    public class WelcomeMessage
    {
        public string Type;

        public string RoomCode;

        public User User;
        public User[] Users;
    }

    // 누가 새로 들어왔다.
    [Serializable]
    public class JoinMessage
    {
        public string Type;
        public User User;
    }

    // 누가 나갔다.
    [Serializable]
    public class LeaveMessage
    {
        public string Type;
        public string Id;
    }

    [Serializable]
    public class ChatMessage
    {
        public string Type = "chat";
        public string Id;
        public string NickName;
        public string Text;
    }

    [Serializable]
    public class StateMessage
    {
        public string Type;
        public PlayerState[] States;
    }

    #endregion
}