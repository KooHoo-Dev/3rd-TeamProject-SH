﻿using System;
using System.Collections.Generic;

namespace TeamSH
{
    [Serializable]
    public class NewGameConfig
    {
        public int MaxRound{get;set;}
        public int MaxCycle{get;set;}
        
    }

    [Serializable]
    public class User // room에 들어와을 때 서버-> 클라 한번만 뿌려준다
    {
        public string Id{get;set;}
        public string NickName{get;set;}

    }

    [Serializable]
    public class PlayerState // 지속적으로 클라이언트에게 뿌려주는 데이터
    {
        public string Id{get;set;}
        public float X{get;set;}
        public float Y{get;set;}
        
        // 추가 코드
        public float Z { get; set; } 
        public int score{get;set;} // 점수 
        public bool IsLiar{get;set;} // 라이어 인가?
        public string[] Items{get;set;} // 카테고리별 최종 선택한 아이템
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


    [Serializable]
    public class LiarSelfDisclose
    {
        public string Type = "LiarSelfDisclose";
        public string ID;
    }
// ===================== 게임 준비 단계 =====================
[Serializable]
public class GameStartStateMessage
{
    public string Type { get; set; } = "게임 시작 상태";
    public float TimerMs { get; set; } // 모든 타임은 MS로 들어옴
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
}

[Serializable]
public class GenreAssignAndLiarSelectStateMessage
{
    public string Type { get; set; } = "장르 배분 및 라이어 선정 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
    public int GanreId{get;set;} // 장르
    public string CurrentOwnerID { get; set; }  // 선정된 라이어
    
}

[Serializable]
public class KeywordDistributeStateMessage
{
    public string Type { get; set; } = "키워드 뿌리기 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
    
    public int KeywordId {get;set;}

}

[Serializable]
public class MartEnterStateMessage
{
    public string Type { get; set; } = "마트 진입 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
}

[Serializable]
public class MartMoveStateMessage // 마트에 존재하는 상태
{
    public string Type { get; set; } = "마트 이동 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
}

// ===================== 마트 & 발언 단계 =====================
[Serializable]
public class MartReturnStateMessage
{
    public string Type { get; set; } = "마트에서 복귀 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
}

[Serializable]
public class ShowItemAndSpeakStateMessage
{
    public string Type { get; set; } = "물건 보여주고 발언 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
    public string CurrentOwnerID { get; set; } // 현재 발언 차례인 유저
}

[Serializable]
public class SpeechEndStateMessage
{
    public string Type { get; set; } = "발언 종료 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
}

// ===================== 지목 단계 =====================
[Serializable]
public class PointAtSuspectStateMessage
{
    public string Type { get; set; } = "지목 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
}

[Serializable]
public class PointAtSuspectEndStateMessage
{
    public string Type { get; set; } = "지목 종료 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
    public string CurrentOwnerID { get; set; } // 최다 득표(지목)된 유저
}

// ===================== 라이어 자진공개 (클라이언트 -> 서버) =====================
[Serializable]
public class LiarOutButtonPressedStateMessage
{
    public string Type { get; set; } = "라밍아웃 버튼 누름 상태";
    public string ID { get; set; } // 버튼을 누른 유저
}

// ===================== 변론 단계 =====================
[Serializable]
public class DebateTimeStateMessage
{
    public string Type { get; set; } = "변론 시간 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
}

[Serializable]
public class DebateEndStateMessage
{
    public string Type { get; set; } = "변론 종료 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
}

// ===================== 투표 단계 =====================
[Serializable]
public class VoteStateMessage
{
    public string Type { get; set; } = "투표 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
}

[Serializable]
public class VoteEndStateMessage
{
    public string Type { get; set; } = "투표 종료 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
    public string CurrentOwnerID { get; set; } // 투표 결과로 지목된 유저
}

// ===================== 라이어 확정 및 키워드 맞추기 단계 =====================
[Serializable]
public class LiarConfirmedStateMessage
{
    public string Type { get; set; } = "라이어 확정 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
    public string CurrentOwnerID { get; set; } // 확정된 라이어 ID
}

[Serializable]
public class LiarKeywordGuessStateMessage
{
    public string Type { get; set; } = "라이어의 키워드 맞춤 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
}

[Serializable]
public class LiarKeywordGuessEndStateMessage
{
    public string Type { get; set; } = "키워드 맞춤 종료 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
}

// ===================== 점수 집계 단계 =====================
[Serializable]
public class ScoreTallyStateMessage
{
    public string Type { get; set; } = "점수 집계 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
}

[Serializable]
public class ScoreTallyEndStateMessage
{
    public string Type { get; set; } = "점수 집계 종료 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
}

// ===================== 최종 결과 단계 =====================
[Serializable]
public class FinalResultStateMessage
{
    public string Type { get; set; } = "최종 결과 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
    public string CurrentOwnerID { get; set; } // 최종 승자 ID (필요 시)
}

[Serializable]
public class FinalResultEndStateMessage
{
    public string Type { get; set; } = "최종 결과 종료 상태";
    public float TimerMs { get; set; }
    public int CurrentCycle { get; set; }
    public int CurrentRound { get; set; }
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