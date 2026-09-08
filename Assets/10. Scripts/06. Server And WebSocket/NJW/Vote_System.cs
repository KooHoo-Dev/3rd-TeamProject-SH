using System.Collections.Generic;
using Study.Utilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using static TeamSH.Protocol;

namespace TeamSH
{
    public class Vote_System : MonoBehaviour
    {
        public static Vote_System Instance;
        
        //[Header("임시 UI 테스트용")]
        //public GameObject VoteUI; // 투표 UI

        [Header("유저 정보")] 
        private User Me => LiarClient.Instance.Me;

        private Dictionary<string, RemotePlayer> remotes => LiarClient.Instance.remotes;
        
        private TurnType CurrentTurnType { get; set; }
        
        [Header("지목 처리")]
        private User CurrentSelectUser { get; set; }
        
        [Header("투표 처리")]
        public User PointTarget { get; set; } //투표할 지목당한 유저

        public enum TurnType
        {
            Wait,
            Point, //지목
            Vote, //투표
        }
        
        private void Awake()
        {
            if (Instance == null) Instance = this;
            CurrentTurnType = TurnType.Wait;
        }

        public void SetTurnType(TurnType currentTurnType, float timerMs)
        {
            CurrentTurnType = currentTurnType;
            CurrentSelectUser = null;
        }

        public void ResetPointTarget()
        {
            PointTarget = null;
        }

        public void SetPointTarget(string targetUserID)
        {
            foreach (KeyValuePair<string, RemotePlayer> remotePlayer in remotes)
            {
                if (remotePlayer.Key == targetUserID)
                {
                    PointTarget = remotePlayer.Value.User;
                    break;
                }
            }
            if (PointTarget == null) PointTarget = Me;
        }

        public void SkipPoint() //원클릭버튼용 기권버튼
        {
            SendNonPointRequest();
            ConferenceManager.Instance?.RequestSkipPointUI(false);
            CurrentTurnType = TurnType.Wait; 
        }

        private void Update()
        {
            switch (CurrentTurnType)
            {
                case TurnType.Wait:
                    break;
                case TurnType.Point:
                    PointOtherPlayer();
                    break;
                case TurnType.Vote:
                    break;
            }
        }

        private void PointOtherPlayer()
        {
            if (SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Left))
            {
                Ray ray = Camera.main.ScreenPointToRay(SimpleInput.GetMousePosition());

                if (Physics.Raycast(ray, out RaycastHit hit3D))
                {
                    RemotePlayer remotePlayer = hit3D.collider.GetComponent<RemotePlayer>();
                    if (remotePlayer == null) return;

                    // 아무도 지목 안 한 상태 → 지목
                    if (CurrentSelectUser == null)
                    {
                        SendSelectRequest(remotePlayer.User.Id, false); // 지목
                        CurrentSelectUser = remotePlayer.User;
                        return;
                    }

                    // 이미 지목한 사람을 다시 클릭 → 지목 해제
                    if (CurrentSelectUser.Id == remotePlayer.User.Id)
                    {
                        SendSelectRequest(remotePlayer.User.Id, true); // 취소
                        CurrentSelectUser = null;
                        return;
                    }

                    // 다른 사람을 클릭 → 기존 타겟 취소 + 새 타겟 지목
                    SendSelectRequest(CurrentSelectUser.Id, true);
                    SendSelectRequest(remotePlayer.User.Id, false);
                    CurrentSelectUser = remotePlayer.User;
                }
            }
        }

        
        public void VoteOtherPlayer(string index) //버튼 인풋용 투표 메서드
        {
            VoteMessage message = new VoteMessage
            {
                UserID =  Me.Id,
                selectNum = index
            };
            
            LiarClient.Instance.RequestVoteMessage(message);
            ConferenceManager.Instance.RequestVoteUI(false); //정보 보내고 UI닫아버리기
            
            SoundManager.Instance?.PlaySfx(SoundManager.GameSfx.VoteButtonClick); // 투표 버튼 클릭 시 SFX
        }

        // 빠른스킵 전달
        public void SendNonPointRequest()
        {
            NonPointMessage message = new NonPointMessage
            {
                UserID =  Me.Id,
            };

            LiarClient.Instance.RequestNonPointMessage(message);
            Debug.Log($"{Me.NickName}가 기권함");
        }
        
        // 지목 전달
        public void SendSelectRequest(string targetSelectedID, bool isCancel = false)
        {
            SelectMessage message = new SelectMessage
            {
                selectID = Me.Id,
                selectedID = targetSelectedID,
                IsSelectCancel = isCancel
            };

            // LiarClient를 통해 서버로 전송
            LiarClient.Instance.RequestSelectMessage(message);
        }
        
        
    }
}