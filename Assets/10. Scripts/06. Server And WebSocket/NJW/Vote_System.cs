using System.Collections.Generic;
using Study.Utilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace TeamSH
{
    public class Vote_System : MonoBehaviour
    {
        public static Vote_System Instance;
        
        [Header("임시 UI 테스트용")]
        public TMP_Text specialMessage; //특수 채팅
        public TMP_Text keywordMessage; // 키워드 채팅
        public GameObject VoteUI; // 투표 UI

        [Header("유저 정보")]
        private User Me{ get; set; }
        private Dictionary<string, RemotePlayer> remotes = new Dictionary<string, RemotePlayer>();
        
        private TurnType CurrentTurnType { get; set; }
        
        [Header("지목 처리")]
        private User CurrentSelectUser { get; set; }

        public bool canPoint = false;
        
        [Header("투표 처리")]
        public User PointTarget { get; set; } //현재 지목당한 유저

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
            GameObject Canvas = GameObject.Find("Canvas");
            specialMessage = Canvas.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            specialMessage = Canvas.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            VoteUI = Canvas.transform.GetChild(2).gameObject;
            specialMessage.gameObject.SetActive(false);
            keywordMessage.gameObject.SetActive(false);
            VoteUI.SetActive(false);
        }

        public void SetInitializeData(User me)
        {
            Me = me;
            Debug.Log($"현재 본인 ID: {Me.Id}");
        }

        public void SetUserCount(Dictionary<string, RemotePlayer> users)
        {
            remotes = users;
            Debug.Log($"현재 방의 유저 수 : {remotes.Count + 1}");
        }

        public void SetTurnType(TurnType currentTurnType, float timerMs)
        {
            CurrentTurnType = currentTurnType;
            if(currentTurnType == TurnType.Point) canPoint = true;
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
            Debug.Log($"{PointTarget.NickName}에게 투표합니다" );
            VoteUI.SetActive(true);
        }

        public void CheckNonPointTarget()
        {
            canPoint = false;
            if(CurrentSelectUser == null) SendNonPointRequest();
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
                    //VoteOtherPlayer();
                    break;
            }
        }

        private void Wait()
        {
            return;
        }

        private void PointOtherPlayer()
        {
            if (canPoint == false) return;

            if (SimpleInput.GetMouseButtonDown(0))
            {
                // 마우스 커서 위치 기준 레이캐스트 발사
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
                if (Physics.Raycast(ray, out RaycastHit hit3D))
                {
                    RemotePlayer remotePlayer = hit3D.collider.GetComponent<RemotePlayer>();
                    
                    if (remotePlayer == null) return;

                    if (CurrentSelectUser == null || remotePlayer.User.Id != CurrentSelectUser.Id)
                    {
                        SendSelectRequest(remotePlayer.User.Id, false);
                        CurrentSelectUser = remotePlayer.User;
                        return;
                    }

                    SendSelectRequest(remotePlayer.User.Id, true);
                    CurrentSelectUser = null;
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
            VoteUI.SetActive(false); //정보 보내고 UI닫아버리기
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