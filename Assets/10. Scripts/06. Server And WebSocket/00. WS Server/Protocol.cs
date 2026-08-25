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
    
    // 선택
    [Serializable]
    public class SelectMessage
    {
        public string Type = "Select";
    
        public int selectID; // 선택한 유저 ID
        public int selectedID; // 선택된 유저 ID
    
        public bool IsSelectCancel; // 선택 취소 여부 bool값
    }

    // 투표 안함
    [Serializable]
    public class NonVoteMessage
    {
        public string Type = "NonVote";
        public string UserID; // 투표 안하는 유저 ID
    }
    
    // 투표
    [Serializable]
    public class VoteMessage
    {
        public string Type = "Vote";
        public string UserID; // 투표하는 유저 ID
        public string selectNum; // 라이어가 아니다 or 모르겠다 or 라이어다
    }
    
    // 키워드 결정
    [Serializable]
    public class KeywordChatMessage
    {
        public string Type = "KeywordChat";
        public string Id;
        public string NickName;
        public string Text;
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
    
    // 게임 시작 
    [Serializable]
    public class GameStartMessage
    {
        public string Type = "게임 시작";
    }
    
    // 키워드 제공
    [Serializable]
    public class KeywordMessage
    {
        public string Type = "keyword";
        public string Keyword ;
    }
    
    // 라이어 버튼 누름 감지
    [Serializable]
    public class PressLiarButtonMessage // 클라이언트 -> 서버
    {
        public User User;
        public string Type = "라밍아웃 버튼 누름";
    }

    // 게임 상태
    [Serializable]
    public class GameState
    {
        // 게임 준비 단계
        public string stateGameStart = "게임 시작";
        public string stateGenreAssignAndLiarSelect = "장르 배분 및 라이어 선정";
        public string stateKeywordDistribute = "키워드 뿌리기";
        public string stateMartEnter = "마트 진입";

        // 마트 & 발언 단계
        public string stateMartReturn = "마트에서 복귀";
        public string stateShowItemAndSpeak = "물건 보여주고 발언";
        public string stateSpeechEnd = "발언 종료";

        // 지목 단계
        public string statePointAtSuspect = "지목";
        public string statePointAtSuspectEnd = "지목 종료";

        // 라이어 아웃 단계
        public string stateLiarOutButtonPressed = "라밍아웃 버튼 누름";

        // 변론 단계
        public string stateDebateTime = "변론 시간";
        public string stateDebateEnd = "변론 종료";

        // 투표 단계
        public string stateVote = "투표";
        public string stateVoteEnd = "투표 종료";

        // 라이어 확정 및 키워드 맞추기 단계
        public string stateLiarConfirmed = "라이어 확정";
        public string stateLiarKeywordGuess = "라이어의 키워드 맞춤";
        public string stateLiarKeywordGuessEnd = "키워드 맞춤 종료";

        // 점수 집계 단계
        public string stateScoreTally = "점수 집계";
        public string stateScoreTallyEnd = "점수 집계 종료";
        public string stateMartMove = "마트 이동"; // 최종 사이클 전

        // 최종 결과 단계
        public string stateFinalResult = "최종 결과";
        public string stateFinalResultEnd = "최종 결과 종료";
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