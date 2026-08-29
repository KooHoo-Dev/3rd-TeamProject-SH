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
        
        [SerializeField] private Transform[] SpawnPoints; // 스폰 위치

        private Dictionary<string, int> userSpawnIndex = new Dictionary<string, int>(); // 유저ID : 자리번호
        
        [Header("보내기")]
        // 초당 몇 번 내 위치를 보낼지.
        // 서버가 뿌리는 횟수와는 별개. (그쪽은 appsettings.json)
        // 보내는 쪽이 더 자주 보내도 되지만, 받는 쪽이 그만큼 자주 보게 되지는 않는다.
        [SerializeField] private float sendPerSecond = 20f;

        // 서버가 정해 준 내 정보. welcome 을 받기 전에는 비어 있다.
        public User Me { get; private set; }

        // 내가 들어갔을 때 방에 아무도 없었으면 내가 방장
        public bool IsHost { get; private set; }

        // 화면에 그리는 일은 RoomLobby 가 한다. 여기는 선을 타는 일만 한다.
        public int RemoteCount => remotes.Count;
        public LocalPlayer Local => localPlayer;

        // userId에 해당하는 닉네임 찾기
        public string GetNickName(string userId)
        {
            if (Me != null && userId == Me.Id) return Me.NickName;
            return remotes.TryGetValue(userId, out RemotePlayer remote) ? remote.User.NickName : userId;
        }

        public bool IsBusy { get; private set; }

        // 서버가 다듬어 알려 준 방 코드. abcd 로 쳐도 ABCD 가 옴.
        public string RoomCode { get; private set; }

        // 게임 시작 확인을 받았는지.
        public bool IsGameStarted { get; private set; }

        // 방에서 오간 말. 로비가 그대로 그린다.
        public IReadOnlyList<string> ChatLog => chatLog;

        // 이번 라운드에 오간 특수채팅 전체. q 정보창
        public IReadOnlyList<string> SpecialChatLog => specialChatLog;

        // 맨 처음 씬 이름
        private const string BootScene = "LIARGAME_Lobby";

        // 추가로 뜬 씬 이름
        private string ActiveContentScene;

        private ClientWebSocket socket;

        // 남들. 번호로 찾는다.
        private readonly Dictionary<string, RemotePlayer> remotes = new Dictionary<string, RemotePlayer>();

        // 오간 말. 너무 쌓이지 않게 앞에서부터 버린다.
        private readonly List<string> chatLog = new List<string>();

        // 특수채팅만 따로 모은 기록. q 정보창
        private readonly List<string> specialChatLog = new List<string>();

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

        // TimerMs를 받은 시각(Time.time)
        private float PhaseStartTime;

        // 지금 상태에서 남은 시간(초). 로컬에서만 돌아감, 0초 밑으로 안감
        public float RemainingSeconds => Mathf.Max(0f, TimerMs / 1000f - (Time.time - PhaseStartTime));

        // 발언차례 / 장르 ID / 키워드 ID
        public string CurrentOwnerID { get; private set; } = "";
        public int GenreId { get; private set; }
        public int KeywordId { get; private set; }

        // 이번 발언 차례에 특수채팅을 이미 보냈는지. 발언자가 바뀔 때마다 초기화된다.
        private bool hasSentSpecialChat;

        // 라이어의 키워드 맞춤 상태에서 키워드채팅을 이미 보냈는지. 그 상태에 들어갈 때마다 초기화된다.
        private bool hasSentKeywordChat;

        #endregion

        // 받은 글자 하나를 종류에 따라 나눠 준다.
        private void Handle(string json)
        {
            string type = JsonConvert.DeserializeObject<TypeOnly>(json).Type;

            if (type != "state") Debug.Log($"[방] 받음 : {type}");

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
                    case "라밍아웃 버튼 누름 상태": OnLiarOutButtonPressedState(JsonConvert.DeserializeObject<LiarOutButtonPressedStateMessage>(json)); break;
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

                    case "leave": OnLeave(JsonConvert.DeserializeObject<LeaveMessage>(json)); break;
                    case "state": OnState(JsonConvert.DeserializeObject<StateMessage>(json)); break;
                    case "chat": OnChat(JsonConvert.DeserializeObject<ChatMessage>(json)); break;

                // 모르는 종류는 흘린다. 서버가 먼저 새 소식을 붙여도 여기서 죽지 않게.
                default: Debug.Log($"[방] 등록되지 않은 종류(type) : {type}"); break;
            }
        }

        private int GetSpawnIndex()
        {
            for (int i = 0; i < SpawnPoints.Length; i++)
            {
                if (userSpawnIndex.ContainsValue(i) == false) return i; // 빈 곳 중에 맨 앞이 스폰위치 (앞부터 채워라)
            }
            
            return SpawnPoints.Length;
        } 
        
        #region 서버 -> 클라이언트 (On)
        
        private void OnWelcome(WelcomeMessage message)
        {
            Me = message.User;
            RoomCode = message.RoomCode;
            IsHost = message.Users == null || message.Users.Length == 0;

            localPlayer.Setup(Me);
            
            for (int i = 0; i < message.Users.Length; i++)
            {
                userSpawnIndex[message.Users[i].Id] = i; // 유저의 스폰인덱스를 가지는 딕셔너리에 저장
                DoSpawn(message.Users[i], SpawnPoints[i].position); // 기존 foreach 원리랑 동일
            }

            int mySpawnIndex = message.Users.Length; // 내 스폰인덱스
            userSpawnIndex[Me.Id] = mySpawnIndex; // 딕셔너리에 저장
            localPlayer.transform.position = SpawnPoints[mySpawnIndex].position; // 실제 위치로 이동 

            Debug.Log($"[방] {RoomCode} 에 들어왔다. 내 번호는 {Me.Id}, 이미 {message.Users.Length}명 있다");
        }

        private void OnJoin(JoinMessage message)
        {
            int index = GetSpawnIndex(); // 빈 곳 중에 맨 앞자리
            userSpawnIndex[message.User.Id] = index; // 지금 들어온 놈 딕셔너리에 저장
            
            DoSpawn(message.User, SpawnPoints[index].position); // 실제 스폰
            Debug.Log($"[방] {message.User.NickName}({message.User.Id}) 이(가) 들어왔다");
        }

        private void OnLeave(LeaveMessage message)
        {
            if (remotes.TryGetValue(message.Id, out RemotePlayer remote) == false) return;

            remotes.Remove(message.Id);
            Destroy(remote.gameObject);

            userSpawnIndex.Remove(message.Id); // 딕셔너리에서 나간놈 ID 키 찾아서 뺌

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
            Debug.Log("게임 데이터 요청을 받고 message 안의 newGameConfig를 클라이언트 NewGameConfig로 저장");

            NewGameConfig = message.newGameConfig;
            IsGameStarted = true;
        }

        private void OnGameStartState(GameStartStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
        }

        private void OnGenreAssignAndLiarSelectState(GenreAssignAndLiarSelectStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound, message.CurrentOwnerID);
            GenreId = message.GenreId;
        }

        private void OnKeywordDistributeState(KeywordDistributeStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
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
            DoLoadScene("LIARGAME_Conference");
        }

        private void OnShowItemAndSpeakState(ShowItemAndSpeakStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound, message.CurrentOwnerID);
            hasSentSpecialChat = false;

            string item = DoGetItem(message.CurrentOwnerID);
            Debug.Log($"[발언] {message.CurrentOwnerID} 이(가) {message.CurrentCategory} 카테고리의 '{item}' 을(를) 보여주며 발언 중");
        }

        // 발언 상태에서 발언자의 첫 채팅이 특수채팅
        private void OnSpecialChat(SpecialChatMessage message)
        {
            string mine = Me != null && message.ID == Me.Id ? " (나)" : "";
            string line = $"[특수] {message.NickName}{message.ID}{mine} : {message.Text}";

            chatLog.Add(line);
            if (chatLog.Count > ChatKeep) chatLog.RemoveAt(0);

            specialChatLog.Add(line);

            Debug.Log($"[특수] {message.ID} : {message.Text}");
        }

        private void OnSpeechEndState(SpeechEndStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            
            int start = Mathf.Max(0, SpecialChatLog.Count - (remotes.Count + 1));

            for (int i = start; i < SpecialChatLog.Count; i++)
            {
                Debug.Log("특수채팅을 공개합니다." + SpecialChatLog[i]);
            }
        }

        private void OnLiarOutButtonPressedState(LiarOutButtonPressedStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, CurrentCycle, CurrentRound);
        }

        private void OnPointAtSuspectState(PointAtSuspectStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
        }

        private void OnPointAtSuspectEndState(PointAtSuspectEndStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound, message.CurrentOwnerID);
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
        }

        private void OnVoteEndState(VoteEndStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
        }

        private void OnLiarConfirmedState(LiarConfirmedStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound, message.CurrentOwnerID);
        }

        private void OnLiarKeywordGuessState(LiarKeywordGuessStateMessage message)
        {
            DoSetPhase(message.Type, message.TimerMs, message.CurrentCycle, message.CurrentRound);
            hasSentKeywordChat = false;
            
            
        }

        // 라이어의 키워드 맞춤 상태에서 라이어의 첫 채팅이 키워드채팅
        private void OnKeywordChat(KeywordChatMessage message)
        {
            string mine = Me != null && message.Id == Me.Id ? " (나)" : "";
            chatLog.Add($"[키워드] {message.NickName}{mine} : {message.Text}");
            if (chatLog.Count > ChatKeep) chatLog.RemoveAt(0);
            Debug.Log($"[키워드] {message.NickName} : {message.Text}");
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

        // 지속적으로 계속 돈다.
        private void OnState(StateMessage message)
        {
            // 로컬 플레이어 정보 동기화
            foreach (PlayerState state in message.States)
            {
                if (state.Id == Me.Id)
                {
                    PlayerState.score = state.score;
                    PlayerState.IsLiar = state.IsLiar;
                    PlayerState.Items = state.Items;
                    continue;
                }
                
                // 다른 플레이어 정보 동기화
                if (remotes.TryGetValue(state.Id, out RemotePlayer remote) == false) continue;

                remote.PlayerState.score = state.score;
                remote.PlayerState.IsLiar = state.IsLiar;
                remote.PlayerState.Items = state.Items;
            }
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
            PhaseStartTime = Time.time;
        }

        // 다른 플레이어 생성
        private void DoSpawn(User user, Vector3 position)
        {
            // 이미 있으면 만들지 않는다. welcome 과 join 이 겹칠 수 있다.
            if (remotes.ContainsKey(user.Id)) return;

            RemotePlayer remote = Instantiate(remotePrefab, position, Quaternion.identity);

            remote.Setup(user);

            remotes[user.Id] = remote;
        }
        
        private void DoLoadScene(string sceneName)
        {
            if (ActiveContentScene == sceneName) return; // 현재씬을 안부르기

            string previous = ActiveContentScene; // 지금 씬 저장
            ActiveContentScene = sceneName; // 매개변수 씬 (새로 불러올 씬)

            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive).completed += _ => // 비동기 씬 로드가 다 끝나면
            {
                Scene loaded = SceneManager.GetSceneByName(sceneName); // 새로운 씬 이름 = loaded
                SceneManager.SetActiveScene(loaded); // 메인 씬으로 이동
                SceneManager.UnloadSceneAsync(previous); // 이전 씬 언로드
            };
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
            // 방장만 서버로 보냄
            if (Me == null || IsHost == false) return;
            SendAsync(new GameStartMessage()).Forget();
        }

        // 지금이 어떤 채팅을 보낼 차례인지 알아서 판단해서 보낸다. 로비가 이거 하나만 부르면 된다.
        public void RequestChat(string text)
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

            // 라이어의 키워드 맞춤 상태 + 내가 라이어 + 아직 키워드채팅을 안 보냈으면, 첫 마디는 키워드채팅이 된다.
            if (CurrentPhase == "라이어의 키워드 맞춤 상태" && PlayerState.IsLiar && hasSentKeywordChat == false)
            {
                hasSentKeywordChat = true;
                RequestKeywordChat(text);
                return;
            }

            RequestNormalChat(text);
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

            SendAsync(new LiarSelfDisclose { ID = Me.Id }).Forget();
        }

        #endregion

        protected override void Awake()
        {
            base.Awake(); // 싱글턴 등록 + DontDestroyOnLoad. 중복 인스턴스면 여기서 파괴된다.

            ActiveContentScene = BootScene;

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
