using System;
using System.Collections.Generic;
using TeamSH;

namespace HelloServer
{
    [Serializable]
    public class NewGameConfig
    {
        public int MaxRound{get;set;}
        public int MaxCycle{get;set;}
        
    }

    [Serializable]
    public class User
    {
        public string Id{get;set;}
        public string NickName{get;set;}
        public int score{get;set;} // 점수 
        
    }

    [Serializable]
    public class PlayerState
    {
        public string Id{get;set;}
        public float X{get;set;}
        public float Y{get;set;}
        
        // 추가 코드
        public float Z { get; set; } 
        public bool IsLiar{get;set;}
        public string[] Items{get;set;}
    }
    
    // 일반 채팅 모드 (C2S / S2C 공유)
    [Serializable]
    public class NormalChatMessage 
    {
        public string Type{get;set;} = "normalChat";
        public string NickName{get;set;}
        public string Text{get;set;}
        public string ID{get;set;}
    }
    // 특수 채팅 모드 (C2S / S2C 공유)
    [Serializable]
    public class SpecialChatMessage 
    {
        public string Type{get;set;} = "specialChat";
        public string NickName{get;set;}
        public string Text{get;set;}
        public string ID{get;set;}
    }
    
    // 선택
    [Serializable]
    public class SelectMessage
    {
        public string Type{get;set;} = "Select";
    
        public int selectID{get;set;} // 선택한 유저 ID
        public int selectedID{get;set;} // 선택된 유저 ID
    
        public bool IsSelectCancel{get;set;} // 선택 취소 여부 bool값
    }

    // 투표 안함
    [Serializable]
    public class NonVoteMessage
    {
        public string Type{get;set;} = "NonVote";
        public string UserID{get;set;} // 투표 안하는 유저 ID
    }
    
    // 투표
    [Serializable]
    public class VoteMessage
    {
        public string Type{get;set;} = "Vote";
        public string UserID{get;set;} // 투표하는 유저 ID
        public string selectNum{get;set;} // 라이어가 아니다 or 모르겠다 or 라이어다
    }
    
    // 라이어가 키워드 맞추기 채팅
    [Serializable]
    public class KeywordChatMessage
    {
        public string Type{get;set;} = "KeywordChat";
        public string Id{get;set;}
        public string NickName{get;set;}
        public string Text{get;set;}
    }
    
    // GameState 변경사항 주고받기
    [Serializable]
    public class ChangeGameStateMessage // 서버 -> 클라이언트 
    {
        public string Type{get;set;} = ""; // GameState.~~~;
        public float Timer{get;set;} // 남은 시간
    
        public string currentOwnerID{get;set;} // 서버에서 선택한 유저
    



        
        public int CurrentCycle{get;set;}

        public int CurrentRound{get;set;}
        
        public string currentCategory{get;set;}
    }

    [Serializable]
    public class ReadyMessage
    {
        public string Type{get;set;} = "ready";
        public string ID{get;set;}
    }
    [Serializable]
    public class AllReadyMessage
    {
        public string Type{get;set;} = "AllReady";
    }
    // 게임 시작 (클라이언트 -> 서버)
    [Serializable]
    public class GameStartMessage
    {
        public string Type{get;set;} = "게임 시작";
        
    } 
    // 서버 -> 클라이언트
    [Serializable]
    public class GameStartOKMessage
    {
        public string Type{get;set;} = "게임 시작 확인";
        public NewGameConfig newGameConfig{get;set;}
    }
    
    // 클라이언트 -> 서버
    [Serializable]
    public class GameLeaveMessage
    {
        public string Type{get;set;} = "GameLeave";
        public string ID{get;set;}
    }
    // 키워드 제공 (서버 -> 클라이언트)
    [Serializable]
    public class KeywordMessage
    {
        public string Type{get;set;} = "keyword";
        public KeyWordDef Keyword {get;set;}
        public GenreDef Ganre{get;set;} // 장르
    }
    
    // 라이어 버튼 누름 감지 
    [Serializable]
    public class PressLiarButtonMessage // 클라이언트 -> 서버
    {
        public PlayerState PlayerState{get;set;}
        public string Type {get;set;}= "라밍아웃 버튼 누름";
    }

    [Serializable]
    public class LiarSelfDisclose
    {
        public string Type = "LiarSelfDisclose";
        public string ID;
    }
    // 게임 상태
    [Serializable]
    public class GameState
    {
        // 게임 준비 단계
        public string stateGameStart{get;set;} = "게임 시작";
        public string stateGenreAssignAndLiarSelect{get;set;} = "장르 배분 및 라이어 선정";
        public string stateKeywordDistribute {get;set;}= "키워드 뿌리기";
        public string stateMartEnter {get;set;}= "마트 진입";

        // 마트 & 발언 단계
        public string stateMartReturn{get;set;} = "마트에서 복귀";
        public string stateShowItemAndSpeak {get;set;}= "물건 보여주고 발언";
        public string stateSpeechEnd {get;set;}= "발언 종료";

        // 지목 단계
        public string statePointAtSuspect {get;set;}= "지목";
        public string statePointAtSuspectEnd {get;set;} = "지목 종료";

        // 라이어 아웃 단계
        public string stateLiarOutButtonPressed{get;set;} = "라밍아웃 버튼 누름";

        // 변론 단계
        public string stateDebateTime{get;set;} = "변론 시간";
        public string stateDebateEnd{get;set;} = "변론 종료";

        // 투표 단계
        public string stateVote{get;set;} = "투표";
        public string stateVoteEnd{get;set;} = "투표 종료";

        // 라이어 확정 및 키워드 맞추기 단계
        public string stateLiarConfirmed{get;set;} = "라이어 확정";
        public string stateLiarKeywordGuess{get;set;} = "라이어의 키워드 맞춤";
        public string stateLiarKeywordGuessEnd{get;set;} = "키워드 맞춤 종료";

        // 점수 집계 단계
        public string stateScoreTally{get;set;} = "점수 집계";
        public string stateScoreTallyEnd{get;set;} = "점수 집계 종료";
        public string stateMartMove{get;set;} = "마트 이동"; // 최종 사이클 전

        // 최종 결과 단계
        public string stateFinalResult{get;set;} = "최종 결과";
        public string stateFinalResultEnd{get;set;} = "최종 결과 종료";
    }
    

    [Serializable]
    public class TypeOnly
    {
        public string Type { get; set; }
    }

    #region 클라이언트 -> 서버

    [Serializable]
    public class HelloMessage
    {
        public string Type{get;set;} = "hello";
        public string NickName{get;set;}
    }

    [Serializable]
    public class MoveMessage
    {
        public string Type{get;set;} = "move";
        public string Id{get;set;}
        public float X{get;set;}
        public float Y{get;set;}
    }

    #endregion

    #region 서버 -> 클라이언트

    [Serializable]
    public class WelcomeMessage
    {
        public string Type{get;set;} = "welcome";

        public string RoomCode{get;set;}

        public User User{get;set;}
        public User[] Users{get;set;}
    }


    // 누가 새로 들어왔다.
    [Serializable]
    public class JoinMessage
    {
        public string Type{get;set;} = "join";
        public User User{get;set;}
    }

    // 누가 나갔다.
    [Serializable]
    public class LeaveMessage
    {
        public string Type{get;set;} = "leave";
        public string Id{get;set;}
    }

    [Serializable]
    public class ChatMessage
    {
        public string Type {get;set;}= "chat";
        public string Id{get;set;}
        public string NickName{get;set;}
        public string Text{get;set;}
    }

    [Serializable]
    public class StateMessage
    {
        public string Type { get; set; } = "state";
        public PlayerState[] States{get;set;}
    }

    #endregion
}