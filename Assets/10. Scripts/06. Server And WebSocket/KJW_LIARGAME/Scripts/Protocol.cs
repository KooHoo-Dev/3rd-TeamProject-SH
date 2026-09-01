﻿﻿using System;
using System.Collections.Generic;
using Jay;
using UnityEngine;

namespace TeamSH
{
    
    #region 공용 데이터 처리

    [Serializable]
    public enum SelectNum
    {
        Liar,
        DontKnow,
        NotLiar,
    }

    [Serializable]
    public enum QuestId
    {
        ItemPickUp
    }


    #region 채팅 처리

    [Serializable]
    public class ChatMessage
    {
        public string Type {get;set;}= "chat";
        public string Id{get;set;}
        public string NickName{get;set;}
        public string Text{get;set;}
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
    
    // 라이어가 키워드 맞추기 채팅
    [Serializable]
    public class KeywordChatMessage
    {
        public string Type{get;set;} = "KeywordChat";
        public string Id{get;set;}
        public string NickName{get;set;}
        public string Text{get;set;}
    }

    #endregion

    #region 이동 처리

    [Serializable]
    public class MoveMessage
    {
        public string Type{get;set;} = "move";
        public string Id{get;set;}
        public float X{get;set;}
        public float Y{get;set;}
        
        public  float Z{get;set;}
    }

    #endregion
    
    #endregion
    
    #region 서버 -> 클라이언트
    
    [Serializable]
    public class TypeOnly
    {
        public string Type { get; set; }
    }
    
    [Serializable]
    public class NewGameConfig
    {
        public int MaxRound{get;set;}
        public int MaxCycle{get;set;}
    }

    #region 퀘스트

    // 서버에서 클리어 판정(PlayerState를 보고 판단)과 시기를 정해 알아서 뿌려준다.
    [Serializable]
    public class ItemPickUpQuestSuccessMessage
    {
        public string Type { get; set; } = "itemPickUpQuestSuccess";
        public string Id{get;set;} // 클리어 한 유저의 Id
    }
        #endregion
    #region 기본 상태 처리

    [Serializable]
    public class StateMessage
    {
        public string Type { get; set; } = "state";
        public PlayerState[] States{get;set;}
    }
    
    
    [Serializable]
    public class UserScoreInfo
    {
        public string UserId { get; set; }
        public int UserScore { get; set; }
    }
    
    
    [Serializable]
    public class PlayerState // 지속적으로 클라이언트에게 뿌려주는 데이터
    {
        public string Id{get;set;}
        public float X{get;set;}
        public float Y{get;set;}
        
        // 추가 코드
        public float Z { get; set; } 
        public bool IsLiar{get;set;} // 라이어 인가?
        
        public string[] Items{get;set;} // 카테고리별 최종 선택한 아이템
        
        public string HoldingItem{get;set;} // 마트에서 들고 이동하는 중인 아이템
        
        public bool IsPushedState{get;set;} // 현재 밀쳐진 상태인가?
        
    }
    
    


    #endregion
    
    #region 유저 입장/퇴장 처리
    
    [Serializable]
    public class User // room에 들어와을 때 서버-> 클라 한번만 뿌려준다
    {
        public string Id{get;set;}
        public string NickName{get;set;}
    }
    
    // 누가 새로 들어왔다.
    [Serializable]
    public class JoinMessage
    {
        public string Type{get;set;} = "join";
        public User User{get;set;}
    }
    
    [Serializable]
    public class WelcomeMessage
    {
        public string Type{get;set;} = "welcome";

        public string RoomCode{get;set;}

        public User User{get;set;}
        public User[] Users{get;set;}
    }

    // 누가 나갔다.
    [Serializable]
    public class LeaveMessage
    {
        public string Type{get;set;} = "leave";
        public string Id{get;set;}
    }
    
    #endregion

    #region 게임 시작 처리
    
    [Serializable]
    public class GameStartStateMessage
    {
        public string Type { get; set; } = "게임 시작 상태";
        public float TimerMs { get; set; } // 모든 타임은 MS로 들어옴
        public int CurrentCycle { get; set; }
        public int CurrentRound { get; set; }
    }
    
    [Serializable]
    public class GameStartOKMessage
    {
        public string Type{get;set;} = "게임 시작 확인";
        public NewGameConfig newGameConfig{get;set;}
    }

    [Serializable]
    public class GenreAssignAndLiarSelectStateMessage
    {
        public string Type { get; set; } = "장르 배분 및 라이어 선정 상태";
        public float TimerMs { get; set; }
        public int CurrentCycle { get; set; }
        public int CurrentRound { get; set; }
        public int GenreId { get; set; } // 장르
        public string CurrentOwnerID { get; set; } // 선정된 라이어
    }

    [Serializable]
    public class KeywordDistributeStateMessage
    {
        public string Type { get; set; } = "키워드 뿌리기 상태";
        public float TimerMs { get; set; }
        public int CurrentCycle { get; set; }
        public int CurrentRound { get; set; }

        public int KeywordId { get; set; } // 라이어는 라이어 키워드, 나머지는 일반
    }

    public enum MessageType
    {
        어쩌고,
        저쩌고,
        턴1메세지
    }
    public class TurnMessageEx
    {
        
        public float TimerMs { get; set; }
        public int CurrentCycle { get; set; }
        public int CurrentRound { get; set; }

        public string SendUserId { get; set; }
        public string ReceiveUserId { get; set; }
        public MessageType Type{get;set;}
        public string parameter{get;set;}
    }
    #endregion

    public static class Message생성
    { 
        public static TurnMessageEx createMessage(MessageType messageType, string parameter)
        {
            switch (messageType)
            {
                
            }
            return new TurnMessageEx();
        }
    }

    public struct PushEventData
    {
        public float feedbackTime;
        public int power;
        public int damage;
    }
    
    public class NetworkManager : SingletonBase<NetworkManager>
    {
        public Action<Interaction_Player, Interaction_Player, InteractionEvent> InteractionEvent { get; set; }
        public Action<string, string, float, Vector2, Vector2> PushEvent { get; set; }

        public void 메시지해석(TurnMessageEx msg)
        {
            switch (msg.Type)
            {
                // 어쩌고는 parameter = 0.5, pX, pY, dX, dY  = 미는 세기, 위치, 방향
                case MessageType.어쩌고:
                    string[] djWjrh = msg.parameter.Split(",");
                    float power = float.Parse(djWjrh[0]);
                    Vector2 position = new Vector2(float.Parse(djWjrh[1]), float.Parse(djWjrh[2]));
                    Vector2 direction = new Vector2(float.Parse(djWjrh[3]), float.Parse(djWjrh[4]));

                    PushEvent?.Invoke(msg.SendUserId, msg.ReceiveUserId, power, position, direction);
                    
                    ///
                    
                    Interaction_Player sender = LiarClient.Instance.GetPlayer(msg.SendUserId);
                    Interaction_Player receiver = LiarClient.Instance.GetPlayer(msg.ReceiveUserId);

                    InteractionEvent @event = new InteractionEvent();
                    @event.PushPower = power;
                    @event.Position = position;
                    @event.HitDirection = direction;
                    
                    Interaction_System.Instance.To(sender, receiver, @event);
                    
                    break;
                // 어쩌고는 parameter = true, 20 = On/Off, 거리
                case MessageType.저쩌고:
                    string[] wjWjrh = msg.parameter.Split(",");
                    bool onOff =  bool.Parse(wjWjrh[0]);
                    float distance = float.Parse(wjWjrh[1]);
                    
                    break;
                // 어쩌고는 parameter = 30.0, "PlayerA 발언" = 제한시간, UI 출력 메시지
                case MessageType.턴1메세지:
                    string[] turnMsg = msg.parameter.Split(",");
                    float time = float.Parse(turnMsg[0]);
                    string uiMessage = turnMsg[1];
                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        
    }
    
    
    
    
    #region 마트 처리

    [Serializable]
    public class MartEnterStateMessage
    {
        public string Type { get; set; } = "마트 진입 상태";
        public float TimerMs { get; set; }
        public int CurrentCycle { get; set; }
        public int CurrentRound { get; set; }
        // 퀘스트는 개인적으로만 이루어지고 결과가 state에 적용되기만 하면 되므로 따로 결과를 받거나 보내지 않음
        // 만약 결과가 남들에게 보여줘야한다면 퀘스트 클리어 메세지에 ID담아서 클라에서 서버로 -> 다른 유저에게 뿌릴 에정
        public string QuestId{get;set;}
    }

    [Serializable]
    public class MartMoveStateMessage // 마트에 존재하는 상태
    {
        public string Type { get; set; } = "마트 이동 상태";
        public float TimerMs { get; set; }
        public int CurrentCycle { get; set; }
        public int CurrentRound { get; set; }
    }
    
    [Serializable]
    public class MartReturnStateMessage
    {
        public string Type { get; set; } = "마트에서 복귀 상태";
        public float TimerMs { get; set; }
        public int CurrentCycle { get; set; }
        public int CurrentRound { get; set; }
        public UserScoreInfo[] userScoreInfo {get;set;}
    }

    #endregion

    #region 턴 처리

    // ===================== 발언 단계 =====================
    [Serializable]
    public class ShowItemAndSpeakStateMessage
    {
        public string Type { get; set; } = "물건 보여주고 발언 상태";
        public float TimerMs { get; set; }
        public int CurrentCycle { get; set; }
        public int CurrentRound { get; set; }
        public string CurrentOwnerID { get; set; } // 현재 발언 차례인 유저

        public string CurrentCategory { get; set; } // 현재 사이클의 아이템 카테고리
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
        public string CurrentOwnerID { get; set; } // 최다 득표(지목)된 유저(동율이거나 없으면 빈 값)
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
        public string liarKeyword { get; set; }
        public string nomalKeyword { get; set; }
        public bool IsRightAnswer{ get; set; }
        
        public UserScoreInfo[]  userScoreInfo{get;set;}
    }

    #endregion

    #region 점수 집계/ 라운드 종료 처리

    // ===================== 점수 집계 단계 =====================
    [Serializable]
    public class ScoreTallyStateMessage
    {
        public string Type { get; set; } = "점수 집계 상태";
        public float TimerMs { get; set; }
        public int CurrentCycle { get; set; }
        public int CurrentRound { get; set; }
        // 라이어가 아닌데 버튼 누른 유저들
        public string[]  LiarOutButtonInfo { get; set; }
        
        public UserScoreInfo[]  userScoreInfo{get;set;}

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
        public List<string> CurrentOwnerID { get; set; } // 최종 승자 ID (필요 시) 동률 시 같이 담아서 보냄
    }

    [Serializable]
    public class FinalResultEndStateMessage
    {
        public string Type { get; set; } = "최종 결과 종료 상태";
        public float TimerMs { get; set; }
        public int CurrentCycle { get; set; }
        public int CurrentRound { get; set; }
    }

    #endregion

    // (특수)라밍아웃 처리
    [Serializable]
    public class LiarSelfDisclose
    {
        public string Type = "LiarSelfDisclose";
        public string ID;
    }
    
    #endregion

    #region 클라이언트 -> 서버

    #region 유저 입장/퇴장 처리

    [Serializable]
    public class HelloMessage
    {
        public string Type{get;set;} = "hello";
        public string NickName{get;set;}
    }
    
    [Serializable]
    public class GameLeaveMessage
    {
        public string Type{get;set;} = "GameLeave";
        public string ID{get;set;}
    }

    #endregion

    #region 게임 시작 처리

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
    
    [Serializable]
    public class GameStartMessage
    {
        public string Type{get;set;} = "게임 시작";
    } 

    #endregion

    #region 투표 처리
    
    // 선택
    [Serializable]
    public class SelectMessage
    {
        public string Type{get;set;} = "Select";
    
        public string selectID{get;set;} // 선택한 유저 ID
        public string selectedID{get;set;} // 선택된 유저 ID
    
        public bool IsSelectCancel{get;set;} // 선택 취소 여부 bool값
    }
    
    // 빠른 스킵
    [Serializable]
    public class NonPointMessage
    {
        public string Type{get;set;} = "NonPoint";
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

    #endregion

    #region 마트 부분

    // 얘기할거( 자기자신 포함으로 메세지를 보내도 처리가 편한지? 그냥 자기 자신 제외로 보낼지?)
    // 누군가 아이템을 집을 때 다른 플레이어들에게 보내는 메세지
    // (이 메세지를 보내면 WhoSuccessGetItemMessage로 변해서 간다.)
    [Serializable]
    public class TryGetItemMessage
    {
        public string Type{get;set;} = "TryGetItem";
        public string UserID{get;set;}
        public string ItemID{get;set;}
    }
    // 클라에서 집기가 성공하는 것은 오로지 이 메세지를 받아야만 한다.
    // 천만분의 하나의 확률로 연달아 같은 ItemId가 들어오는 경우 먼저온 메세지만 보낸다.(검증 로직)
    // 서버 -> 클라이언트임
    [Serializable]
    public class WhoSuccessGetItemMessage
    {
        public string Type{get;set;} = "WhoSuccessGetItem";
        public string UserID{get;set;}
        public string ItemID{get;set;}
    }
    // 최종 가방에 아이템 넣을 때 클라가 서버에게 보내는 메세지
    [Serializable]
    public class TryInputItemInBag
    {
        public string Type{get;set;} = "TryInputItemInBag";
        public string UserID{get;set;}
        public string ItemID{get;set;}
    }

    // 서버에서 판정해서 결과를 보내주는 방식( 서버 -> 클라)
    // 논의할 것 : 같은 카테고리 아이템을 넣으면 이전에 넣었던 아이템이 원래 자리로 가는가?
    // 혹은 손으로 가는가? 아니면 넣기에 실패하는가? 
    [Serializable]
    public class ResultInputItemInBag
    {
        public string Type{get;set;} = "ResultInputItemInBag";
        public string UserID{get;set;}
        public string ItemID{get;set;}
        public bool  IsSuccess{get;set;}
    }

    // 밀치기, 모든 유저에게 한번 뿌려짐
    [Serializable]
    public class PushMessage
    {
        public string Type{get;set;} = "push";
        
        public string PushUserId{get;set;} // 밀치기 시도한 유저
        
        public string PushedUserId{get;set;} // 밀쳐진 유저, 일단 한명만 가능하다 가정 (없을 수 있음)
        
    }



    // 퀘스트 정의 예시
    public interface IQuest<out T>
    {
        public string QuestId{get;set;}
        public bool IsSuccess{get;set;}
        public bool IsFailed{get;set;}
        public T GetReward();

        public void CheckQuestStatus();
    }
    

        #endregion
    // 라밍아웃 처리(라이어가 라이어 버튼을 눌렀을 때 진입하는 상태)
    [Serializable]
    public class LiarOutButtonPressedStateMessage
    {
        public string Type { get; set; } = "라밍아웃 버튼 누름 상태";
        public string ID { get; set; } // 버튼을 누른 유저
        public float TimerMs { get; set; }

    }

    #endregion
}