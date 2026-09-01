using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Jay;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

namespace TeamSH
{

    // RoomClient가 서버와의 네트워크 작업을 담당한다.
    // 송신, 수신 등을 모두 처리하는 객체
    // 씬이 바뀌어도 유지해야해서 싱글톤베이스로 만듬
    public class LiarClient : SingletonBase<LiarClient>
    {
        [Header("서버")]
        // 우리 서버 주소
        [SerializeField] private string url = "ws://34.50.63.204:5003/room";
        // 서버에 알릴 내 이름.
        [SerializeField] private string nickName = "이름없음";
        // 들어갈 방.
        [SerializeField] private string roomCode = "ABCD";
        
        private ClientWebSocket socket;
        
        public bool IsBusy { get; private set; }
        
        
        [Header("보내기")]
        // 초당 몇 번 내 위치를 보낼지.
        // 서버가 뿌리는 횟수와는 별개. (그쪽은 appsettings.json)
        // 보내는 쪽이 더 자주 보내도 되지만, 받는 쪽이 그만큼 자주 보게 되지는 않는다.
        [SerializeField] private float sendPerSecond = 20f;
        
        [Header("플레이어 프리팹 설정")]
        // 내가 조종하는 캐릭터. 씬에 미리 놓아 둔다.
        [SerializeField] private LocalPlayer localPlayer;
        // 남이 들어올 때마다 이걸로 하나씩 만든다.
        [SerializeField] private RemotePlayer remotePrefab;

        public string liarKeyword; // 라이어의 키워드
        public string nomalKeyword; // 일반인의 키워드


        public bool LiarButtonClicked; // 라이어 버튼을 눌렀는지 확인하는 bool값
        
        public UserScoreInfo[] UserScoreInfo { get; set; } // 유저의 ID, 유저의 Score를 저장하는 필드

        public Dictionary<string, int> scoresDict = new Dictionary<string, int>(); // userId : score 딕셔너리
        
        public List<string> WinnerUserID { get; private set; }
        
        // 점수 높은 순서대로 정렬
        public List<string> GetScoreSorted()
        {
            List<string> scoreSortedList = new List<string>(); // 빈 리스트 새로 만듦 (호출될때마다 빈 리스트로 다시생성)
            if (UserScoreInfo == null) return scoreSortedList; // null이면 그냥 내보냄

            // 내림차순 진행한다.
            Array.Sort(UserScoreInfo, (a, b) => b.UserScore.CompareTo(a.UserScore));
            
            foreach (UserScoreInfo info in UserScoreInfo)
                scoreSortedList.Add($"{GetNickName(info.UserId)} : {info.UserScore} 점"); // 순서대로 추가한다.
            
            return scoreSortedList;
        }
        
        
        #region Get관련
        
        // Id로 닉네임 찾기
        public string GetNickName(string userId) // ID로 닉네임 찾기
        {
            // 매개변수 id가 내 id와 동일하면 내 닉네임 리턴
            if (Me != null && userId == Me.Id) return Me.NickName;
            // 아니면 remotes 딕셔너리에서 매개변수 id와 동일한 id를 가진 remote의 닉네임 리턴 / 없으면 그냥 매개변수 리턴
            return remotes.TryGetValue(userId, out RemotePlayer remote) ? remote.User.NickName : userId;
        }

        public Interaction_Player GetPlayer(string userId)
        {
            if (userId == Me.Id) return localPlayer; 
            return remotes[userId];
        }

        public int GetScore(string userId)
        {
            // 내 Id와 UserScoreInfo를 대조하여, 같은 Id일 시 점수를 리턴한다.
            return 0;
        }
        
        public Transform GetPointPivot(string userId) // Id가 remote에 없으면 나다.
        {
            return remotes.TryGetValue(userId, out RemotePlayer remote) ? remote.pointPivot : localPlayer.pointPivot;
        }
        
        #endregion
        
        [Header("방 정보")]
        // 게임 시작 확인을 받았는지. NewGameConfig는 [Serializable] 필드라서 Unity가
        // 항상 기본 인스턴스를 채워 넣어 null 체크로 못 씀 - 그래서 이 플래그를 따로 둔다.
        public bool IsGameStarted { get; private set; }
        // 내가 들어갔을 때 방에 아무도 없었으면 내가 방장
        public bool IsHost { get; private set; }
        public string RoomCode { get; private set; } // 서버가 다듬어 알려 준 방 코드. abcd 로 쳐도 ABCD 가 옴.
        
        [Header("내부 유저 정보")]

        // 접속 순서를 그대로 기록
        private readonly List<string> userOrder = new List<string>();
        // 남들. 번호로 찾는다.
        public readonly Dictionary<string, RemotePlayer> remotes = new Dictionary<string, RemotePlayer>();
        // 화면에 그리는 일은 RoomLobby 가 한다. 여기는 선을 타는 일만 한다.
        public int RemoteCount => remotes.Count;
        
        [Header("내 정보")]
        // 서버가 정해 준 내 정보. welcome 을 받기 전에는 비어 있다.
        public User Me { get; private set; }
        public LocalPlayer Local => localPlayer;

        [Header("라이어 정보")]
        // 라이어 ID 저장하는 필드
        public string LiarId { get; private set; } = "";
        // 방에서 오간 말. 로비가 그대로 그린다.
        public IReadOnlyList<string> ChatLog => chatLog;
        // 이번 라운드에 오간 특수채팅 전체. Tab 정보창이 그대로 보여준다 (chatLog와 달리 개수 제한 없음).
        public IReadOnlyList<string> SpecialChatLog => specialChatLog;
        // 맨 처음 씬 이름. activeContentScene의 시작값으로 쓴다.
        private const string BootScene = "LIARGAME_Lobby";
        // 지금 추가로 떠 있는 씬 이름
        private string activeContentScene;

        // 오간 말. 너무 쌓이지 않게 앞에서부터 버린다.
        private readonly List<string> chatLog = new List<string>();

        // 특수채팅만 따로 모은 기록. Tab 정보창 전용이라 버리지 않는다.
        private List<string> specialChatLog = new List<string>();

        // 발언자의 턴에 입력한 특수채팅 단일
        public string specialChat;

        private const int ChatKeep = 12;

        // 한 소켓에 동시에 두 번 보내면 안 된다.
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);

        #region 라이어 게임 필드

        // NewGameConfig 저장할 클래스
        public NewGameConfig NewGameConfig;

        // 동적으로 변하는 플레이어 상태
        public PlayerState PlayerState = new PlayerState();

        // 서버한테 메세지 받을때마다 갱신되는 정보
        public string CurrentPhase { get; private set; } = "";
        public float TimerMs { get; private set; }
        public int CurrentCycle { get; private set; }
        public int CurrentRound { get; private set; }

        // TimerMs를 받은 시각(Time.time). 서버에는 안 보내고, 화면에 남은 시간을 보여주기 위한 것뿐이다.
        private float phaseStartTime;

        // 지금 상태에서 남은 시간(초). 서버가 실제로 언제 다음 메세지를 보낼지와는 무관한, 순전히 시각적인 값이다.
        public float RemainingSeconds => Mathf.Max(0f, TimerMs / 1000f - (Time.time - phaseStartTime));

        // 발언차례 / 장르 ID / 키워드 ID
        public string CurrentOwnerID { get; private set; } = "";
        public int GenreId { get; private set; }
        public int KeywordId { get; private set; }
        
        public int LiarKeywordID { get; private set; }

        // 지목 종료 때 최다 득표된 유저 ID
        public string MostSelectId { get; private set; } = "";

        // 이번 발언 차례에 특수채팅을 이미 보냈는지. 발언자가 바뀔 때마다 초기화된다.
        private bool hasSentSpecialChat;

        // 라이어의 키워드 맞춤 상태에서 키워드채팅을 이미 보냈는지. 그 상태에 들어갈 때마다 초기화된다.
        private bool hasSentKeywordChat;

        // 라이어가 보낸 키워드채팅 텍스트. 키워드 맞춤 종료 상태에서 전체 공개할 때 쓴다.
        public string KeywordChatText { get; private set; } = "";

        #endregion

        // 받은 글자 하나를 종류에 따라 나눠 준다.
        private void Handle(string json)
        {
            string type = JsonConvert.DeserializeObject<TypeOnly>(json).Type;

            if(type != "state") Debug.Log($"[방] 받음 : {type}");

            switch (type)
            {
                    case "welcome": OnWelcome(JsonConvert.DeserializeObject<WelcomeMessage>(json)); break;
                    case "join": OnJoin(JsonConvert.DeserializeObject<JoinMessage>(json)); break;

                    case "게임 시작 확인": OnGameStartOK(JsonConvert.DeserializeObject<GameStartOKMessage>(json)); break;

                    // 채팅 관련 case
                    case "normalChat": OnNormalChat(JsonConvert.DeserializeObject<NormalChatMessage>(json)); break;
                    case "specialChat": OnSpecialChat(JsonConvert.DeserializeObject<SpecialChatMessage>(json)); break;
                    case "KeywordChat": OnKeywordChat(JsonConvert.DeserializeObject<KeywordChatMessage>(json)); break;


                    // 상태별 진행 단계 알림. 각자 모양이 달라서 타입마다 따로 받는다.
                    case "게임 시작 상태": OnGameStartState(JsonConvert.DeserializeObject<GameStartStateMessage>(json)); break;
                    case "장르 배분 및 라이어 선정 상태": OnGenreAssignAndLiarSelectState(JsonConvert.DeserializeObject<GenreAssignAndLiarSelectStateMessage>(json)); break;
                    case "키워드 뿌리기 상태": OnKeywordDistributeState(JsonConvert.DeserializeObject<KeywordDistributeStateMessage>(json)); break;
                    case "마트 진입 상태": OnMartEnterState(JsonConvert.DeserializeObject<MartEnterStateMessage>(json)); break;
                    case "마트 이동 상태": OnMartMoveState(JsonConvert.DeserializeObject<MartMoveStateMessage>(json)); break;
                    case "마트에서 복귀 상태": OnMartReturnState(JsonConvert.DeserializeObject<MartReturnStateMessage>(json)); break;
                    case "물건 보여주고 발언 상태": OnShowItemAndSpeakState(JsonConvert.DeserializeObject<ShowItemAndSpeakStateMessage>(json)); break;
                    case "발언 종료 상태": OnSpeechEndState(JsonConvert.DeserializeObject<SpeechEndStateMessage>(json)); break;
                    case "지목 상태": OnPointAtSuspectState(JsonConvert.DeserializeObject<PointAtSuspectStateMessage>(json)); break;
                    case "지목 종료 상태": OnPointAtSuspectEndState(JsonConvert.DeserializeObject<PointAtSuspectEndStateMessage>(json)); break;
                    case "변론 시간 상태": OnDebateTimeState(JsonConvert.DeserializeObject<DebateTimeStateMessage>(json)); break;
                    case "변론 종료 상태": OnDebateEndState(JsonConvert.DeserializeObject<DebateEndStateMessage>(json)); break;
                    case "투표 상태": OnVoteState(JsonConvert.DeserializeObject<VoteStateMessage>(json)); break;
                    case "투표 종료 상태": OnVoteEndState(JsonConvert.DeserializeObject<VoteEndStateMessage>(json)); break;
                    case "라이어 확정 상태": OnLiarConfirmedState(JsonConvert.DeserializeObject<LiarConfirmedStateMessage>(json)); break;
                    case "라이어의 키워드 맞춤 상태": OnLiarKeywordGuessState(JsonConvert.DeserializeObject<LiarKeywordGuessStateMessage>(json)); break;
                    case "키워드 맞춤 종료 상태": OnLiarKeywordGuessEndState(JsonConvert.DeserializeObject<LiarKeywordGuessEndStateMessage>(json)); break;
                    case "점수 집계 상태": OnScoreTallyState(JsonConvert.DeserializeObject<ScoreTallyStateMessage>(json)); break;
                    case "점수 집계 종료 상태": OnScoreTallyEndState(JsonConvert.DeserializeObject<ScoreTallyEndStateMessage>(json)); break;
                    case "최종 결과 상태": OnFinalResultState(JsonConvert.DeserializeObject<FinalResultStateMessage>(json)); break;
                    case "최종 결과 종료 상태": OnFinalResultEndState(JsonConvert.DeserializeObject<FinalResultEndStateMessage>(json)); break;

                    case "Select": OnSelect(JsonConvert.DeserializeObject<SelectMessage>(json)); break;
                    case "NonPoint" : OnNonPointMessage(JsonConvert.DeserializeObject<NonPointMessage>(json)); break;
                    case "Vote": OnVoteMessage(JsonConvert.DeserializeObject<VoteMessage>(json)); break;
                    
                    case "leave": OnLeave(JsonConvert.DeserializeObject<LeaveMessage>(json)); break;
                    case "state": OnState(JsonConvert.DeserializeObject<StateMessage>(json)); break;
                    case "chat": OnChat(JsonConvert.DeserializeObject<ChatMessage>(json)); break;

                // 모르는 종류는 흘린다. 서버가 먼저 새 소식을 붙여도 여기서 죽지 않게.
                default: Debug.Log($"[방] 등록되지 않은 종류(type) : {type}"); break;
            }
        }

        #region 서버 -> 클라이언트 (On)

        private void OnWelcome(WelcomeMessage message)
        {
            Me = message.User;
            RoomCode = message.RoomCode;
            IsHost = message.Users == null || message.Users.Length == 0;

            localPlayer.Setup(Me);
            
            // 누가 들어올때마다 순서 초기화하고처음부터 다시세운다.
            userOrder.Clear();
            if (message.Users != null)
            {
                foreach (User user in message.Users)
                {
                    userOrder.Add(user.Id);
                    Spawn(user);
                }
            }
            userOrder.Add(Me.Id);
            
            Debug.Log($"[방] {RoomCode} 에 들어왔다. 내 번호는 {Me.Id}, 이미 {message.Users.Length}명 있다");
        }

        private void OnJoin(JoinMessage message)
        {
            userOrder.Add(message.User.Id); // 순서 리스트에 추가

            Spawn(message.User);
            Debug.Log($"[방] {message.User.NickName}({message.User.Id}) 이(가) 들어왔다");
            
            //Vote_System.Instance.SetUserCount(remotes); //모든 유저 카운트알림 다른유저 + 본인
            Debug.Log($"총 유저 수 전달! {remotes.Count + 1}");
        }

        private void OnLeave(LeaveMessage message)
        {
            if (remotes.TryGetValue(message.Id, out RemotePlayer remote) == false) return;

            remotes.Remove(message.Id);
            userOrder.Remove(message.Id); // 순서 에서 제거
            
            //Vote_System.Instance.SetUserCount(remotes); //모든 유저 카운트알림 다른유저 + 본인
            Destroy(remote.gameObject);

            Debug.Log($"[방] {message.Id} 이(가) 나갔다");
        }

        // 일반 채팅은 게임 시작부터 종료까지 언제든 오갈 수 있어서 여기 둔다.
        private void OnNormalChat(NormalChatMessage message)
        {
            string mine = Me != null && message.ID == Me.Id ? $"나" : $"{message.ID}";
            chatLog.Add($"[일반] {message.NickName}{message.ID}{mine} : {message.Text}");
            if (chatLog.Count > ChatKeep) chatLog.RemoveAt(0);
            Debug.Log($"[일반] {message.ID} : {message.Text}");
        }

        // 방에서 누가 말했다. 내 말도 여기로 돌아옴
        private void OnChat(ChatMessage message)
        {
            string mine = Me != null && message.Id == Me.Id ? " (나)" : "";

            chatLog.Add($"{message.NickName}{mine} : {message.Text}");

            if (chatLog.Count > ChatKeep) chatLog.RemoveAt(0);

            Debug.Log($"[방] {message.NickName}({message.Id}) : {message.Text}");
        }

        private void OnGameStartOK(GameStartOKMessage message)
        {
            Debug.Log("게임 시작 완료 메세지를 받고 config 데이터 저장");

            NewGameConfig = message.newGameConfig;
            IsGameStarted = true;
            UserScoreInfo = null; // 이전 게임 점수 초기화
        }

        private void OnGameStartState(GameStartStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
        }

        private void OnGenreAssignAndLiarSelectState(GenreAssignAndLiarSelectStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound, message.CurrentOwnerID);
            GenreId = message.GenreId;

            Debug.Log($"라이어의 ID는 :{message.CurrentOwnerID} 입니다.");
            
            // 라이어의 Id는 메세지로 받은 Id임
            LiarId = message.CurrentOwnerID;
        }

        private void OnKeywordDistributeState(KeywordDistributeStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);

            // 메세지로 온 키워드 ID 저장
            KeywordId = message.KeywordId;
        }

        private void OnMartEnterState(MartEnterStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            DoLoadScene("LIARGAME_MART");
            
        }

        private void OnMartMoveState(MartMoveStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
        }

        private void OnMartReturnState(MartReturnStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            DoLoadScene("00. MeetingRoom");
        }

        private void OnShowItemAndSpeakState(ShowItemAndSpeakStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound, message.CurrentOwnerID);
            hasSentSpecialChat = false;

            LiarButtonClicked = false;
            
            string item = DoGetItem(message.CurrentOwnerID);
            Debug.Log($"[발언] {message.CurrentOwnerID} 이(가) {message.CurrentCategory} 카테고리의 '{item}' 을(를) 보여주며 발언 중");
        }

        // 발언 상태에서 발언자의 첫 채팅이 특수채팅
        private void OnSpecialChat(SpecialChatMessage message)
        {   
            // 다음 발전자에서 단일 특수채팅 초기화
            specialChat = null;
            
            string mine = Me != null && message.ID == Me.Id ? " (나)" : "";
            string line = $"[특수] {message.NickName}{message.ID}{mine} : {message.Text}";

            chatLog.Add(line);
            if (chatLog.Count > ChatKeep) chatLog.RemoveAt(0);

            // 단일로 저장
            specialChat = message.Text;
            
            specialChatLog.Add(line);

            Debug.Log($"[특수] {message.ID} : {message.Text}");
        }

        private void OnSpeechEndState(SpeechEndStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            
            int playerCount = RemoteCount + 1; // 나 + 이외 사람들
            int start = Mathf.Max(0, SpecialChatLog.Count - playerCount);

            for (int i = start; i < SpecialChatLog.Count; i++)
            {
                Debug.Log(SpecialChatLog[i]);
            }
        }

        private void OnPointAtSuspectState(PointAtSuspectStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            //여기 지목단계 시작
            Vote_System.Instance?.SetTurnType(Vote_System.TurnType.Point ,message.TimerMs);
            ConferenceManager.Instance?.RequestSkipPointUI(true); // 스킵 버튼 켜기
        }

        private void OnPointAtSuspectEndState(PointAtSuspectEndStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound, message.CurrentOwnerID);
            //여기가 지목단계 종료
            Vote_System.Instance?.SetTurnType(Vote_System.TurnType.Wait ,message.TimerMs);
            ConferenceManager.Instance?.RequestSkipPointUI(false); // 스킵 버튼 끄기
            ConferenceManager.Instance?.ClearAllArrows(); // 모든 화살표 지우기
            
            if(string.IsNullOrEmpty(message.CurrentOwnerID)) Debug.Log($"투표가 스킵되었습니다.");
            else
            {
                Debug.Log($"가장 많이 지목된 유저 {message.CurrentOwnerID}");
                Vote_System.Instance?.SetPointTarget(message.CurrentOwnerID);
            }
        }

        private void OnDebateTimeState(DebateTimeStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            hasSentSpecialChat = false;
            Debug.Log("변론 단계 시작");
        }

        private void OnDebateEndState(DebateEndStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            Debug.Log("변론 끝");
        }

        private void OnVoteState(VoteStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            //여기가 투표 시작
            Vote_System.Instance?.SetTurnType(Vote_System.TurnType.Vote ,message.TimerMs);
            if (Vote_System.Instance?.PointTarget.Id != Me.Id) ConferenceManager.Instance?.RequestVoteUI(true);
            else Vote_System.Instance?.SetTurnType(Vote_System.TurnType.Wait, message.TimerMs);
        }

        private void OnVoteEndState(VoteEndStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            //여기가 투표 종료
            Vote_System.Instance?.SetTurnType(Vote_System.TurnType.Wait ,message.TimerMs);
            ConferenceManager.Instance?.RequestVoteUI(false);
            //투표 종료할 때 PointTarget 초기화
            Vote_System.Instance?.ResetPointTarget();
        }

        private void OnLiarConfirmedState(LiarConfirmedStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound, message.CurrentOwnerID);

            Debug.Log($"확정된 유저는 {message.CurrentOwnerID} 입니다.");
            
            if (LiarId == message.CurrentOwnerID) Debug.Log($"{message.CurrentOwnerID}는 라이어입니다.");
            else Debug.Log($"{message.CurrentOwnerID}는 라이어가 아닙니다.");
            
            Debug.Log($"라이어는 {GetNickName(LiarId)} 입니다.");
                
        }

        private void OnLiarKeywordGuessState(LiarKeywordGuessStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);

            hasSentKeywordChat = false;
            KeywordChatText = "";
        }

        // 라이어의 키워드 맞춤 상태에서 라이어의 첫 채팅이 키워드채팅
        private void OnKeywordChat(KeywordChatMessage message)
        {
            string mine = Me != null && message.Id == Me.Id ? " (나)" : "";
            chatLog.Add($"[키워드] {message.NickName}{mine} : {message.Text}");
            if (chatLog.Count > ChatKeep) chatLog.RemoveAt(0);
            Debug.Log($"[키워드] {message.NickName} : {message.Text}");

            KeywordChatText = message.Text;
        }

        private void OnLiarKeywordGuessEndState(LiarKeywordGuessEndStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            liarKeyword = message.liarKeyword; // 라이어 키워드 저장
            nomalKeyword = message.nomalKeyword; // 일반 키워드 저장
            
            specialChatLog.Clear(); // 현재까지의 특수 채팅 초기화

            // UI에 표시한다.
            UserScoreInfo = message.userScoreInfo; // 서버에서 온 스코어 정보를 클라의 UserScoreInfo에 저장한다.

            for (int i = 0; i < UserScoreInfo.Length; i++) // 받아서 ID/점수를 Dict으로 저장함
            {
                scoresDict[UserScoreInfo[i].UserId] = UserScoreInfo[i].UserScore;
            }
        }

        private void OnScoreTallyState(ScoreTallyStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            
            // UI에 표시한다.
            for (int i = 0; i < UserScoreInfo.Length; i++) // 키워드 맞춤 종료 상태의 점수 상태를 업데이트한다.
            {
                scoresDict[UserScoreInfo[i].UserId] = UserScoreInfo[i].UserScore;
            }
        }

        private void OnScoreTallyEndState(ScoreTallyEndStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
        }

        private void OnFinalResultState(FinalResultStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            // 여기서 추가한다. (뭘 추가할까)

            // 최종 우승자 (동률일 경우를 고려해서 List로)를 받아온다.
            WinnerUserID = message.CurrentOwnerID;
            
            // 최종라운드 후 게임 완전 종료가 되서 점수를 초기화한다.
            scoresDict.Clear();
            
        }

        private void OnFinalResultEndState(FinalResultEndStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            
            IsGameStarted = false; // 게임시작 아님
            DoLoadScene("LIARGAME_Lobby"); // 로비씬으로
        }
        
        #region 투표 처리

        private void OnSelect(SelectMessage message)
        {
            if (message.IsSelectCancel == false)
            {
                Debug.Log($"{message.selectID}가 {message.selectedID}를 지목했습니다.");
                Transform fromPivot = GetPointPivot(message.selectID);
                Transform toPivot = GetPointPivot(message.selectedID);

                Color arrowColor = ColorOf(message.selectID); // 지목한 사람마다 고유 색
                ConferenceManager.Instance?.DrawArrow(message.selectID, fromPivot, toPivot, arrowColor);
            }
            else
            {
                Debug.Log($"{message.selectID}가 {message.selectedID}에 대한 지목을 취소했습니다.");
                ConferenceManager.Instance?.RemoveArrow(message.selectID);
            }
        }

        private void OnNonPointMessage(NonPointMessage message)
        {
            //여기 투표안한 유저를 대기상태로 변경하는 코드 추가
            if (message.UserID == Me.Id)
            {
                Vote_System.Instance?.SetTurnType(Vote_System.TurnType.Wait, TimerMs);
                Debug.Log($"나 는 투표에서 기권했다.");
            }
            else Debug.Log($"{message.UserID}가 기권했습니다.");
            ConferenceManager.Instance?.RemoveArrow(message.UserID);
        }

        private void OnVoteMessage(VoteMessage message)
        {
            string debugMessage = "";
            
            switch (message.selectNum)
            {
                case "Liar":
                    debugMessage = "라이어로 투표했습니다.";
                    break;
                case "DontKnow":
                    debugMessage = "모르겠다로 투표했습니다.";
                    break;
                case "NotLiar":
                    debugMessage = "라이어가 아니다로 투표했습니다.";
                    break;
            }
            Debug.Log($"{message.UserID}가 {Vote_System.Instance.PointTarget.NickName}을 {debugMessage}");
        }

        #endregion

        // 지속적으로 계속 돈다.
        private void OnState(StateMessage message)
        {
            // 로컬 플레이어 정보 동기화
            foreach (PlayerState state in message.States)
            {
                if (state.Id == Me.Id)
                {
                    // PlayerState.score = state.score;
                    PlayerState.IsLiar = state.IsLiar;
                    PlayerState.Items = state.Items;
                    continue;
                }
                
                // 다른 플레이어 정보 동기화
                if (remotes.TryGetValue(state.Id, out RemotePlayer remote) == false) continue;
                // remote.SetTarget(new Vector2(state.X, state.Y));

                // remote.PlayerState.score = state.score;
                remote.PlayerState.IsLiar = state.IsLiar;
                remote.PlayerState.Items = state.Items;
            }
        }

        private void Spawn(User user)
        {
            // 이미 있으면 만들지 않는다. welcome 과 join 이 겹칠 수 있다.
            if (remotes.ContainsKey(user.Id)) return;

            RemotePlayer remote = Instantiate(remotePrefab, Vector3.zero, Quaternion.identity);

            remote.Setup(user);
            // remote.Setup(user, ColorOf(user.Id));

            remotes[user.Id] = remote;

            // 이미 마트/회의실에 들어와 있는 상태라면 새로 들어온 사람까지 포함해서 즉시 재배치
            DoRepositionAll();
        }

        #endregion

        #region 클라이언트 내부처리 (Do)

        // 메세지 내부 정보 받기
        private void DoSetPhase(string type, float timerMs, int currentCycle, int currentRound, string currentOwnerID = "")
        {
            CurrentPhase = type;
            TimerMs = timerMs;
            CurrentCycle = currentCycle;
            CurrentRound = currentRound;
            CurrentOwnerID = currentOwnerID;
            phaseStartTime = Time.time;
        }

        private void DoLoadScene(string sceneName)
        {
            if (activeContentScene == sceneName) return; // 이미 그 씬이면 다시 부르지 않는다.

            string previous = activeContentScene;
            activeContentScene = sceneName;

            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive).completed += _ =>
            {
                Scene loaded = SceneManager.GetSceneByName(sceneName);
                SceneManager.SetActiveScene(loaded);
                SceneManager.UnloadSceneAsync(previous);
                
                // 씬이 완전히 로드되어 매니저가 Awake를 끝낸 뒤에 전원 재배치
                DoRepositionAll();
                DoSetPlayer(sceneName); //모든 플레이어 설정변경: ex)마트 무브x, 회의실 무브O
            };
        }

        private void DoSetPlayer(string sceneName)
        {
            Debug.Log($"플레이어 위치변경 {sceneName}");
            localPlayer.SetPlayerWithScenes(sceneName);
            foreach (RemotePlayer remote in remotes.Values)
            {
                remote.SetPlayerWithScenes(sceneName);
            }
        }

        // userId가 이번 사이클에 고른 아이템을 찾는다. 나 자신이거나, remotes에 있는 사람만 알 수 있다.
        private string DoGetItem(string userId)
        {
            PlayerState state = Me != null && userId == Me.Id
                ? PlayerState
                : remotes.TryGetValue(userId, out RemotePlayer remote) ? remote.PlayerState : null;

            if (state?.Items == null || CurrentCycle < 0 || CurrentCycle >= state.Items.Length) return null;

            return state.Items[CurrentCycle];
        }   
        
        // 현재 씬(마트/회의실)의 스폰 포인트를 가져와서 접속 순서대로 배치한다.
        private void DoRepositionAll()
        {
            userOrder.Sort((a, b) =>
            {
                if (int.TryParse(a, out int idA) && int.TryParse(b, out int idB))
                    return idA.CompareTo(idB);
                return string.CompareOrdinal(a, b);
            });
            
            Transform[] spawnPoints;

            switch (activeContentScene)
            {
                case "LIARGAME_MART":
                    spawnPoints = MartManager.Instance?.userSpawnPoints;
                    break;
                case "00. MeetingRoom":
                    spawnPoints = ConferenceManager.Instance?.userSpawnPoints;
                    break;
                default:
                    return; // 로비처럼 위치가 필요 없는 씬이면 조용히 넘어간다.
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning($"[배치] '{activeContentScene}' 씬에서 스폰 포인트를 찾지 못함 - 매니저에 배열이 연결되어 있는지 확인하세요");
                return;
            }

            for (int i = 0; i < userOrder.Count; i++)
            {
                if (i >= spawnPoints.Length)
                {
                    Debug.LogWarning($"[배치] 스폰 포인트({spawnPoints.Length}개)보다 인원이 많다. 나머지는 배치하지 않음");
                    break;
                }

                string id = userOrder[i];
                Vector3 pos = spawnPoints[i].position;
                Quaternion rot = spawnPoints[i].rotation;

                if (id == Me.Id)
                {
                    localPlayer.transform.position = pos;
                    localPlayer.transform.rotation = rot;
                }
                else if (remotes.TryGetValue(id, out RemotePlayer remote))
                {
                    remote.transform.position = pos;
                    remote.transform.rotation = rot;
                }
            }
        }

        #endregion

        #region 클라이언트 -> 서버 (Request)

        public void RequestGameStart() // 게임 시작 요청
        {
            // 방장만 서버로 보냄
            if (Me == null || IsHost == false) return;
            SendAsync(new GameStartMessage()).Forget();
        }

        private void RequestNormalChat(string text)
        {
            SendAsync(new NormalChatMessage { ID = Me.Id, NickName = Me.NickName, Text = text.Trim() }).Forget();
        }

        private void RequestSpecialChat(string text)
        {
            SendAsync(new SpecialChatMessage { ID = Me.Id, NickName = Me.NickName, Text = text.Trim() }).Forget();
        }

        private void RequestKeywordChat(string text)
        {
            SendAsync(new KeywordChatMessage { Id = Me.Id, NickName = Me.NickName, Text = text.Trim() }).Forget();
        }

        // 라밍아웃 버튼을 눌렀다. 실제 라이어인지는 서버가 검증한다.
        public void RequestLiarSelfDisclose()
        {
            if (Me == null) return;

            LiarButtonClicked = true;
            
            SendAsync(new LiarSelfDisclose { ID = Me.Id }).Forget();
        }
        
        #region 투표 처리

        // 선택결과 전달
        public void RequestSelectMessage(SelectMessage message)
        {
            if (Me == null) return;
            SendAsync(message).Forget();
        }

        // 빠른 스킵 전달
        public void RequestNonPointMessage(NonPointMessage message)
        {
            if (Me == null) return;
            SendAsync(message).Forget();
        }

        // 투표 결과 전달
        public void RequestVoteMessage(VoteMessage message)
        {
            if (Me == null) return;
            SendAsync(message).Forget();
        }

        #endregion
        
        #endregion

        protected override void Awake()
        {
            base.Awake(); // 싱글턴 등록 + DontDestroyOnLoad. 중복 인스턴스면 여기서 파괴된다.

            activeContentScene = BootScene;

            // true로 해둬야 백그라운드에서도 데이터 수신 가능
            Application.runInBackground = true;
        }

        public void Join(string roomCode, string nickName)
        {
            if (IsBusy) return;

            this.roomCode = roomCode;
            this.nickName = nickName;

            RunAsync().Forget();
        }

        // 접속하고, 이름을 알리고, 끊길 때까지 듣는다.
        private async UniTaskVoid RunAsync()
        {
            IsBusy = true;

            socket = new ClientWebSocket();

            // 방 코드는 주소에 실어 보낸다.  ws://localhost:5000/room?code=ABCD
            string full = $"{url}?code={UnityWebRequest.EscapeURL(roomCode)}";

            try
            {
                // socket을 통해 해당 주소로 접속한다. UnitTask를 사용한다
                await socket.ConnectAsync(new Uri(full), destroyCancellationToken).AsUniTask();
            }
            catch (Exception e)
            {
                // 서버가 안 켜져 있는 것은 오류가 아니라 흔한 일이다. 수업 중에 반드시 일어난다.
                Debug.LogWarning($"[방] 접속하지 못했다. 서버가 켜져 있는지, 방 코드가 맞는지 확인 : {e.Message}");

                IsBusy = false;
                return;
            }

            Debug.Log($"[방] {full} 에 접속.  : {nickName}");
            // 첫 마디는 반드시 hello 다. 서버가 이걸 받아야 번호를 줌.
            await SendAsync(new HelloMessage { NickName = nickName });
            // 보내기는 따로 돈다. 듣는 동안에도 계속 보내야함.
            SendLoop().Forget();
            await ReceiveLoopAsync();

            // 여기까지 오면 끊긴 것.
            IsBusy = false;
            Me = null;
        }

        // 끊길 때까지 계속 듣는다.
        private async UniTask ReceiveLoopAsync()
        {
            byte[] buffer = new byte[4096];
            StringBuilder builder = new StringBuilder();

            while (socket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;

                try
                {
                    // .AsUniTask() 를 붙이는 이유
                    // : 이 await 뒤에서 유니티 오브젝트를 만진다.
                    //  붙여 두면 본 스레드로 돌아와서 이어진다. 안 붙이면 다른 스레드일 수 있고,
                    //  거기서 transform 을 만지는 순간 유니티가 예외를 던진다.
                    result = await socket
                        .ReceiveAsync(new ArraySegment<byte>(buffer), destroyCancellationToken)
                        .AsUniTask();
                }
                catch (OperationCanceledException)
                {
                    // 이 오브젝트가 사라졌다
                    return;
                }
                catch (WebSocketException)
                {
                    Debug.LogWarning("[방] 연결이 끊겼다");
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Close) return;

                builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                // 긴 글자는 여러 조각으로 나뉘어 온다. 끝 조각이 올 때까지 이어 붙인다.
                if (result.EndOfMessage == false) continue;

                string json = builder.ToString();
                builder.Clear();

                Handle(json);
            }
        }

        private async UniTaskVoid SendLoop()
        {
            int delay = (int)(1000f / sendPerSecond);

            while (destroyCancellationToken.IsCancellationRequested == false)
            {
                await UniTask.Delay(delay, cancellationToken: destroyCancellationToken);

                // 라이어 게임에서는 플레이어가 움직이지 않아서 위치를 보낼 필요가 없다.
                // 아직 번호를 못 받았으면 보낼 것이 없다.
                if (Me == null) continue;
                // 씬 전환 중 아직 재연결 전이면 잠시 쉰다.
                if (localPlayer == null) continue;

                // Vector2 position = localPlayer.transform.position;
                // await SendAsync(new MoveMessage { Id = Me.Id, X = position.x, Y = position.y });
            }
        }

        // 지금이 어떤 채팅을 보낼 차례인지 알아서 판단해서 보낸다. 로비가 이거 하나만 부르면 된다.
        public void Say(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (Me == null) return;

            // 발언 상태 + 내가 발언자 + 이번 차례에 아직 특수채팅을 안 보냈으면, 첫 마디는 특수채팅이 된다.
            if (CurrentPhase == "물건 보여주고 발언 상태" && CurrentOwnerID == Me.Id && hasSentSpecialChat == false)
            {
                hasSentSpecialChat = true;
                RequestSpecialChat(text);
                return;
            }

            // 변론 시간 상태 + 내가 득표된 사람 + 아직 특수채팅을 안 보냈으면, 첫 마디는 특수채팅이 된다.
            if (CurrentPhase == "변론 시간 상태" && MostSelectId == Me.Id && hasSentSpecialChat == false)
            {
                hasSentSpecialChat = true;
                RequestSpecialChat(text);
                return;
            }

            // 라이어의 키워드 맞춤 상태 + 내 id가 라이어 id와 동일하면 + 아직 키워드채팅을 안 보냈으면, 첫 마디는 키워드채팅이 된다.
            if (CurrentPhase == "라이어의 키워드 맞춤 상태" && LiarId == Me.Id && hasSentKeywordChat == false)
            {
                hasSentKeywordChat = true;
                RequestKeywordChat(text);
                return;
            }

            RequestNormalChat(text);
            // SendAsync(new ChatMessage { Text = text.Trim() }).Forget();
        }

        // 한 마디를 보낸다. 보내는 곳이 여럿이라 lock을 걸어서 한 번에 하나씩 내보낸다.
        private async UniTask SendAsync(object message)
        {
            if (socket == null || socket.State != WebSocketState.Open) return;

            byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

            await sendLock.WaitAsync();

            try
            {
                await socket
                    .SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, destroyCancellationToken)
                    .AsUniTask();
            }
            catch (OperationCanceledException)
            {
                // 이 오브젝트가 사라졌다.
            }
            catch (WebSocketException)
            {
                // 보내는 순간 끊겼다. 듣는 쪽 루프가 알아서 빠져나온다.
            }
            finally
            {
                sendLock.Release();
            }
        }

        private static Color ColorOf(string id)
        {
            int hash = 0;

            foreach (char c in id) hash = hash * 31 + c;

            return Color.HSVToRGB(Mathf.Abs(hash % 360) / 360f, 0.65f, 0.95f);
        }

        private void OnDestroy()
        {
            socket?.Dispose();
        }
    }
}
