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
        [Header("장면")]
        // 내가 조종하는 캐릭터. 씬에 미리 놓아 둔다.
        [SerializeField] private LocalPlayer localPlayer;
        // 남이 들어올 때마다 이걸로 하나씩 만든다.
        [SerializeField] private RemotePlayer remotePrefab;

        [Header("보내기")]
        // 초당 몇 번 내 위치를 보낼지.
        // 서버가 뿌리는 횟수와는 별개. (그쪽은 appsettings.json)
        // 보내는 쪽이 더 자주 보내도 되지만, 받는 쪽이 그만큼 자주 보게 되지는 않는다.
        [SerializeField] private float sendPerSecond = 20f;

        // 서버가 정해 준 내 정보. welcome 을 받기 전에는 비어 있다.
        public User Me { get; private set; }

        // 내가 들어갔을 때 방에 아무도 없었으면 내가 방장이다.
        // (서버의 Room.JoinAsync가 members.IsEmpty 로 host를 정하는 것과 같은 조건)
        public bool IsHost { get; private set; }

        // 화면에 그리는 일은 RoomLobby 가 한다. 여기는 선을 타는 일만 한다.
        public int RemoteCount => remotes.Count;
        public LocalPlayer Local => localPlayer;

        public bool IsBusy { get; private set; }

        // 서버가 다듬어 알려 준 방 코드. abcd 로 쳐도 ABCD 가 옴.
        public string RoomCode { get; private set; }

        // 방에서 오간 말. 로비가 그대로 그린다.
        public IReadOnlyList<string> ChatLog => chatLog;

        private ClientWebSocket socket;

        // 남들. 번호로 찾는다.
        private readonly Dictionary<string, RemotePlayer> remotes = new Dictionary<string, RemotePlayer>();

        // 씬이 바뀌어도 유저는 유지되어야함
        private readonly Dictionary<string, User> knownUsers = new Dictionary<string, User>();

        // 오간 말. 너무 쌓이지 않게 앞에서부터 버린다.
        private readonly List<string> chatLog = new List<string>();

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
        
        // 발언차례 / 장르 ID / 키워드 ID
        public string CurrentOwnerID { get; private set; } = "";
        public int GenreId { get; private set; }
        public int KeywordId { get; private set; }

        // 마트에서 쓸 카테고리 순서 모음 : KMS/MartMap/Data/ItemDef.cs에서 가져옴
        private CategoryType[] categoryOrder;

        #endregion

        // 받은 글자 하나를 종류에 따라 나눠 준다.
        private void Handle(string json)
        {
            try
            {
                string type = JsonConvert.DeserializeObject<TypeOnly>(json).Type;

                switch (type)
                {
                    case "welcome": OnWelcome(JsonConvert.DeserializeObject<WelcomeMessage>(json)); break;
                    case "join": OnJoin(JsonConvert.DeserializeObject<JoinMessage>(json)); break;

                    case "게임 시작": OnGameStart(JsonConvert.DeserializeObject<GameStartMessage>(json)); break;
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
                    case "NonPointMessage" : OnNonPointMessage(JsonConvert.DeserializeObject<NonPointMessage>(json)); break;
                    case "VoteMessage": OnVoteMessage(JsonConvert.DeserializeObject<VoteMessage>(json)); break;
                    
                    
                    case "leave": OnLeave(JsonConvert.DeserializeObject<LeaveMessage>(json)); break;
                    case "state": OnState(JsonConvert.DeserializeObject<StateMessage>(json)); break;
                    case "chat": OnChat(JsonConvert.DeserializeObject<ChatMessage>(json)); break;

                    // 모르는 종류는 흘린다. 서버가 먼저 새 소식을 붙여도 여기서 죽지 않게.
                    default: Debug.Log($"[방] 등록되지 않은 종류(type) : {type}"); break;
                }
            }
            catch (Exception e)
            {
                // 모르는 종류가 아니라 이상한 형식 데이터여도 일단 서버를 이어감
                Debug.LogWarning($"[방] 메시지를 알아보지 못했다 : {e.Message}\n{json}");
            }
        }

        #region 서버 -> 클라이언트 (On)

        #region 입장 처리

        private void OnJoin(JoinMessage message)
        {
            knownUsers[message.User.Id] = message.User;
            DoSpawn(message.User);
            Debug.Log($"[방] {message.User.NickName}({message.User.Id}) 이(가) 들어왔다");
            
            Vote_System.Instance.SetUserCount(remotes); //모든 유저 카운트알림 다른유저 + 본인
            Debug.Log($"총 유저 수 전달! {remotes.Count + 1}");
        }
        
        private void OnWelcome(WelcomeMessage message)
        {
            Me = message.User;
            RoomCode = message.RoomCode;
            IsHost = message.Users == null || message.Users.Length == 0;

            localPlayer.Setup(Me);
            
            Vote_System.Instance.SetInitializeData(Me); //투표시스템 ID적용
            Vote_System.Instance.SetUserCount(remotes); //모든 유저 카운트알림 다른유저 + 본인
            
            Debug.Log($"시스템에 본인 ID전달 {Me.Id}");

            foreach (User user in message.Users)
            {
                knownUsers[user.Id] = user;
                DoSpawn(user);
            }

            Debug.Log($"[방] {RoomCode} 에 들어왔다. 내 번호는 {Me.Id}, 이미 {message.Users.Length}명 있다");
        }
        
        private void OnLeave(LeaveMessage message)
        {
            knownUsers.Remove(message.Id);

            if (remotes.TryGetValue(message.Id, out RemotePlayer remote) == false) return;

            remotes.Remove(message.Id);
            Vote_System.Instance.SetUserCount(remotes); //모든 유저 카운트알림 다른유저 + 본인
            Destroy(remote.gameObject);

            Debug.Log($"[방] {message.Id} 이(가) 나갔다");
        }
        
        #endregion

        #region 채팅처리

        // 방에서 누가 말했다. 내 말도 여기로 돌아옴
        private void OnChat(ChatMessage message)
        {
            string mine = Me != null && message.Id == Me.Id ? " (나)" : "";

            chatLog.Add($"{message.NickName}{mine} : {message.Text}");

            if (chatLog.Count > ChatKeep) chatLog.RemoveAt(0);

            Debug.Log($"[방] {message.NickName}({message.Id}) : {message.Text}");
        }
        
        private void OnNormalChat(NormalChatMessage message)
        {
            string mine = Me != null && message.ID == Me.Id ? $"나" : $"{message.ID}";
            chatLog.Add($"[일반] {message.NickName}{message.ID}{mine} : {message.Text}");
            if (chatLog.Count > ChatKeep) chatLog.RemoveAt(0);
            Debug.Log($"[일반] {message.ID} : {message.Text}");
            //Vote_System.Instance.
        }

        private void OnSpecialChat(SpecialChatMessage message)
        {
            string mine = Me != null && message.ID == Me.Id ? " (나)" : "";
            chatLog.Add($"[특수] {message.NickName}{message.ID}{mine} : {message.Text}");
            if (chatLog.Count > ChatKeep) chatLog.RemoveAt(0);
            Debug.Log($"[특수] {message.ID} : {message.Text}");
        }
        
        private void OnKeywordChat(KeywordChatMessage message)
        {
            string mine = Me != null && message.Id == Me.Id ? " (나)" : "";
            chatLog.Add($"[키워드] {message.NickName}{mine} : {message.Text}");
            if (chatLog.Count > ChatKeep) chatLog.RemoveAt(0);
            Debug.Log($"[키워드] {message.NickName} : {message.Text}");
        }

        #endregion

        #region 게임 시작 처리

        private void OnGameStart(GameStartMessage message)
        {
            Debug.Log("게임 시작 및 게임 정보를 요청한다");
            RequestGameStartState();
        }
        
        private void OnGameStartOK(GameStartOKMessage message)
        {
            Debug.Log("게임 데이터 요청을 받고 message 안의 newGameConfig를 클라이언트 NewGameConfig로 저장");

            NewGameConfig = message.newGameConfig;
        }
        
        private void OnGameStartState(GameStartStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            RequestGenreAssignAndLiarSelectState();
        }
        
        private void OnGenreAssignAndLiarSelectState(GenreAssignAndLiarSelectStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound, message.CurrentOwnerID);
            GenreId = message.GenreId;
            RequestKeywordDistributeState();
        }

        private void OnKeywordDistributeState(KeywordDistributeStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            KeywordId = message.KeywordId;
            RequestMartEnterState();
        }

        #endregion

        #region 턴 처리

        private void OnMartEnterState(MartEnterStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            DoLoadScene("LIARGAME_MART");
            RequestMartMoveState();
        }

        private void OnMartMoveState(MartMoveStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            DoPickMartItem();
        }

        private void OnMartReturnState(MartReturnStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            DoLoadScene("LIARGAME_Conference");
        }

        private void OnShowItemAndSpeakState(ShowItemAndSpeakStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound, message.CurrentOwnerID);
        }

        private void OnSpeechEndState(SpeechEndStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
        }

        private void OnPointAtSuspectState(PointAtSuspectStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            //여기 지목단계 시작
            Vote_System.Instance.SetTurnType(Vote_System.TurnType.Point ,message.TimerMs);
        }

        private void OnPointAtSuspectEndState(PointAtSuspectEndStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound, message.CurrentOwnerID);
            //여기가 지목단계 종료
            Vote_System.Instance.SetTurnType(Vote_System.TurnType.Wait ,message.TimerMs);
            Vote_System.Instance.SetPointTarget(message.CurrentOwnerID);
        }

        private void OnDebateTimeState(DebateTimeStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
        }

        private void OnDebateEndState(DebateEndStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
        }

        private void OnVoteState(VoteStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            //여기가 투표 시작
            Vote_System.Instance.SetTurnType(Vote_System.TurnType.Vote ,message.TimerMs);
            
        }

        private void OnVoteEndState(VoteEndStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound, message.CurrentOwnerID);
            //여기가 투표 종료
            Vote_System.Instance.SetTurnType(Vote_System.TurnType.Wait ,message.TimerMs);
        }

        private void OnLiarConfirmedState(LiarConfirmedStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound, message.CurrentOwnerID);
        }

        private void OnLiarKeywordGuessState(LiarKeywordGuessStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
        }

        private void OnLiarKeywordGuessEndState(LiarKeywordGuessEndStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
        }

        private void OnScoreTallyState(ScoreTallyStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
        }

        private void OnScoreTallyEndState(ScoreTallyEndStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
        }

        private void OnFinalResultState(FinalResultStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound, message.CurrentOwnerID);
        }
        
        private void OnFinalResultEndState(FinalResultEndStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
        }
        
        #endregion
        
        #region 투표 처리

        private void OnSelect(SelectMessage message)
        {
            bool Select = message.IsSelectCancel;
            //여기 화살표 처리 넣어야함
            // 뭐 대충 DrawArrow같은느낌
            if(Select) Debug.Log($"{message.selectID}가 {message.selectedID}를 지목했습니다. ");
            else Debug.Log($"{message.selectID}가 {message.selectedID}에 대한 지목을 취소했습니다.");
        }

        private void OnNonPointMessage(NonPointMessage message)
        {
            //여기 투표안한 유저를 대기상태로 변경하는 코드 추가
            if (message.UserID == Me.Id) Vote_System.Instance.SetTurnType(Vote_System.TurnType.Wait, TimerMs);
        }

        private void OnVoteMessage(VoteMessage message)
        {
            
        }

        #endregion


        private void OnState(StateMessage message)
        {
            // 캐릭터는 움직이지 않으므로 좌표는 무시하고, 점수/라이어 여부/아이템만 반영한다.
            foreach (PlayerState state in message.States)
            {
                if (Me != null && state.Id == Me.Id)
                {
                    PlayerState.score = state.score;
                    PlayerState.IsLiar = state.IsLiar;
                    PlayerState.Items = state.Items;
                    continue;
                }

                if (remotes.TryGetValue(state.Id, out RemotePlayer remote) == false) continue;

                remote.PlayerState.score = state.score;
                remote.PlayerState.IsLiar = state.IsLiar;
                remote.PlayerState.Items = state.Items;
            }
        }

        #endregion

        #region 클라이언트 내부처리 (Do)

        // 공통 필드(Type/TimerMs/CurrentCycle/CurrentRound)만 옮겨 담는다. currentOwnerID 뜻은 상태마다 다르다.
        private void DoSetPhase(string type, float timerMs, int currentCycle, int currentRound, string currentOwnerID = "")
        {
            CurrentPhase = type;
            TimerMs = timerMs;
            CurrentCycle = currentCycle;
            CurrentRound = currentRound;
            CurrentOwnerID = currentOwnerID;
        }

        private void DoSpawn(User user)
        {
            // 이미 있으면 만들지 않는다. welcome 과 join 이 겹칠 수 있다.
            if (remotes.ContainsKey(user.Id)) return;

            RemotePlayer remote = Instantiate(remotePrefab, Vector3.zero, Quaternion.identity);

            remote.Setup(user);

            remotes[user.Id] = remote;
        }

        // 마트 이동 상태를 받을 때마다, 이번 사이클의 카테고리에서 아이템 1개를 랜덤으로 골라 담는다.
        private void DoPickMartItem()
        {
            if (NewGameConfig == null) return;
            if (CurrentCycle < 0 || CurrentCycle >= NewGameConfig.MaxCycle) return;

            if (PlayerState.Items == null) PlayerState.Items = new string[NewGameConfig.MaxCycle];
            if (categoryOrder == null) categoryOrder = DoShuffleCategories(NewGameConfig.MaxCycle);

            List<ItemDef> itemsInCategory = DataManager.Instance.GetItemDefsByCategory(categoryOrder[CurrentCycle]);
            if (itemsInCategory.Count == 0) return;

            ItemDef picked = itemsInCategory[UnityEngine.Random.Range(0, itemsInCategory.Count)];
            PlayerState.Items[CurrentCycle] = picked.ItemName;

            Debug.Log($"[마트] {CurrentCycle}번째 카테고리({categoryOrder[CurrentCycle]})에서 '{picked.ItemName}' 선택");
        }

        // 전체 카테고리를 랜덤으로 섞은 뒤, 앞에서부터 count개만 이번 게임의 카테고리 순서로 쓴다.
        private CategoryType[] DoShuffleCategories(int count)
        {
            List<CategoryType> all = new List<CategoryType>((CategoryType[])Enum.GetValues(typeof(CategoryType)));
            all.Remove(CategoryType.None);

            for (int i = all.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (all[i], all[j]) = (all[j], all[i]);
            }

            return all.GetRange(0, Mathf.Min(count, all.Count)).ToArray();
        }

        private void DoLoadScene(string sceneName)
        {
            if (SceneManager.GetActiveScene().name == sceneName) return; // 이미 그 씬이면 다시 부르지 않는다.
            SceneManager.LoadScene(sceneName);
        }

        // 씬 이동할때 오브젝트 연결
        public void DoBindScene(LocalPlayer localPlayer, RemotePlayer remotePrefab)
        {
            this.localPlayer = localPlayer;
            this.remotePrefab = remotePrefab;

            if (Me != null) this.localPlayer.Setup(Me);

            remotes.Clear(); // 이전 씬의 원격 오브젝트는 이미 파괴되었으니 목록만 비운다.

            foreach (User user in knownUsers.Values)
            {
                if (Me != null && user.Id == Me.Id) continue;
                DoSpawn(user);
            }
        }

        #region 투표 처리
        
        

        #endregion
        
        #endregion

        #region 클라이언트 -> 서버 (Request)

        public void RequestJoin(string roomCode, string nickName)
        {
            if (IsBusy) return;

            this.roomCode = roomCode;
            this.nickName = nickName;

            RunAsync().Forget();
        }

        public void RequestGameStart() // 게임 시작 요청
        {
            // UI 쪽 게이팅과 별개로, 방장이 아니면 서버에 보내지도 않는다.
            if (Me == null || IsHost == false) return;
            SendAsync(new GameStartMessage()).Forget();
        }

        public void RequestNormalChat(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (Me == null) return;

            SendAsync(new NormalChatMessage { ID = Me.Id, NickName = Me.NickName, Text = text.Trim() }).Forget();
        }

        // 이전 상태를 받은 뒤 다음 상태로 넘어가 달라고 서버에 알린다.
        // 지금 서버는 이 4개를 받아 처리하는 코드가 없어 당장은 효과가 없지만,
        // 문서 명세(클라이언트 -> 서버 흐름)를 그대로 지키기 위해 만들어 둔다.
        public void RequestGameStartState()
        {
            if (Me == null) return;
            SendAsync(new GameStartStateMessage()).Forget();
        }

        public void RequestGenreAssignAndLiarSelectState()
        {
            if (Me == null) return;
            SendAsync(new GenreAssignAndLiarSelectStateMessage()).Forget();
        }

        public void RequestKeywordDistributeState()
        {
            if (Me == null) return;
            SendAsync(new KeywordDistributeStateMessage()).Forget();
        }

        public void RequestMartEnterState()
        {
            if (Me == null) return;
            SendAsync(new MartEnterStateMessage()).Forget();
        }

        public void RequestMartMoveState()
        {
            if (Me == null) return;
            SendAsync(new MartMoveStateMessage()).Forget();
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

            // true로 해둬야 백그라운드에서도 데이터 수신 가능
            Application.runInBackground = true;
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
                catch (WebSocketException e)
                {
                    Debug.Log(e.Message);
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

                // 아직 번호를 못 받았으면 보낼 것이 없다.
                if (Me == null) continue;
                // 씬 전환 중 아직 재연결 전이면 잠시 쉰다.
                if (localPlayer == null) continue;

                Vector2 position = localPlayer.transform.position;

                await SendAsync(new MoveMessage { Id = Me.Id, X = position.x, Y = position.y });
            }
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
