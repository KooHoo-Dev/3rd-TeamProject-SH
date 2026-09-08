using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Jay;
using NetworkManager;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Unity.VisualScripting;
using static TeamSH.Protocol; // protocol.cs가 Protocol 클래스로 묶여있어서 일단 using으로 선언함

namespace TeamSH
{
    // RoomClient가 서버와의 네트워크 작업을 담당한다.
    // 송신, 수신 등을 모두 처리하는 객체
    // 씬이 바뀌어도 유지해야해서 싱글톤베이스로 만듬
    public class LiarClient : SingletonBase<LiarClient>
    {
        [Header("서버")]
        // 우리 서버 주소
        [SerializeField]
        private string url = "ws://34.50.63.204:5003/room";

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
        [SerializeField]
        private float sendPerSecond = 20f;

        [Header("플레이어 프리팹 설정")]
        // 내가 조종하는 캐릭터. 씬에 미리 놓아 둔다.
        [SerializeField] private LocalPlayer LocalPlayerPrefab;
        public LocalPlayer localPlayer { get; private set; }

        // 남이 들어올 때마다 이걸로 하나씩 만든다.
        [SerializeField] private RemotePlayer remotePrefab;

        public string LiarKeyword { get; private set; } // 라이어의 키워드
        public string NormalKeyword { get; private set; } // 일반인의 키워드 


        public bool LiarButtonClicked { get; set; } // 라이어 버튼을 눌렀는지 확인하는 bool값
        public bool IsNotLiarPushCycle { get; private set; } // 라이어 아닌 사람이 버튼을 누른 사이클인지

        public UserScoreInfo[] UserScoreInfo { get; set; } // 유저의 ID, 유저의 Score를 저장하는 필드

        public List<string> WinnerUserID { get; private set; }

        #region Get관련

        // 점수 높은 순서대로 정렬
        public List<string> GetScoreSorted()
        {
            if (UserScoreInfo == null) Debug.Log("유저 스코어 정보가 없습니다");
            
            List<string> scoreSortedList = new List<string>(); // 빈 리스트 새로 만듦 (호출될때마다 빈 리스트로 다시생성)
            if (UserScoreInfo == null) return scoreSortedList; // null이면 그냥 내보냄

            // 내림차순 진행한다.
            Array.Sort(UserScoreInfo, (a, b) => b.UserScore.CompareTo(a.UserScore));
            
            foreach (UserScoreInfo info in UserScoreInfo)
                scoreSortedList.Add($"{GetNickName(info.UserId)} : {info.UserScore} 점"); // 순서대로 추가한다.

            return scoreSortedList;
        }
        
        // Id로 닉네임 찾기
        public string GetNickName(string userId) // ID로 닉네임 찾기
        {
            // 매개변수 id가 내 id와 동일하면 내 닉네임 리턴
            if (Me != null && userId == Me.Id) return Me.NickName;
            // 아니면 remotes 딕셔너리에서 매개변수 id와 동일한 id를 가진 remote의 닉네임 리턴 / 없으면 그냥 매개변수 리턴
            return remotes.TryGetValue(userId, out RemotePlayer remote) ? remote.User.NickName : userId;
        }

        public Transform GetPointPivot(string userId) // Id가 remote에 없으면 나다.
        {
            return remotes.TryGetValue(userId, out RemotePlayer remote) ? remote.pointPivot : localPlayer.pointPivot;
        }

        public Transform[] GetPointPivot(List<string> winnerId) // 우승자ID 리스트 전용 유틸함수 
        {
            Transform[] winnerPivot = new Transform[winnerId.Count];
            for (int i = 0; i < winnerId.Count; i++)
            {
                winnerPivot[i] = remotes.TryGetValue(winnerId[i], out RemotePlayer remote) 
                    ? remote.pointPivot : localPlayer.pointPivot;
            }

            return winnerPivot; // 우승자 transform[] 리턴
        }
        
        public Interaction_Player GetPlayer(string userId)
        {
            if (userId == Me.Id) return localPlayer;
            return remotes[userId];
        }

        public string GetPlayerId(Interaction_Player player)
        {
            string playerId = null;
            
            foreach (var remote in remotes.Values)
            {
                if (remote == player)
                {
                    return remote.User.Id;
                }
            }

            return null;
        }
        
        #endregion

        #region 플레이어 색상 설정

        public PlayerColorSet colorSet;
        
        #endregion
        
        [Header("방 정보")]
        // 게임 시작 확인을 받았는지 확인
        public bool IsGameStarted { get; private set; }

        // 내가 들어갔을 때 방에 아무도 없었으면 내가 방장
        public bool IsHost { get; private set; }
        public string RoomCode { get; private set; } // 서버가 다듬어 알려 준 방 코드. abcd 로 쳐도 ABCD 가 옴.

        [Header("내부 정보")]

        // 접속 순서를 그대로 기록
        public readonly List<string> userOrder = new List<string>();

        // 남들. 번호로 찾는다.
        // 리모트 유저의 ID : 리모트 유저의 프리팹
        public readonly Dictionary<string, RemotePlayer> remotes = new Dictionary<string, RemotePlayer>();

        public string genre = "";
        public string keyWord = "";
        
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
        private const string BootScene = "00. LobbyRoom";
        public const string LOBBYSCENE = "00. LobbyRoom";
        public const string CONFERENCESCENE = "01. MeetingRoom";
        public const string MARTSCENE = "02. Mart";

        public const string READYSTATE = "GameReady";
        public const string MEETINGSTATE = "applyMeeting";
        public const string MARTSTATE = "applyMart";

        // 지금 추가로 떠 있는 씬 이름
        private string activeContentScene;

        // 오간 말. 너무 쌓이지 않게 앞에서부터 버린다.
        private readonly List<string> chatLog = new List<string>();

        // 특수채팅만 따로 모은 기록. Tab 정보창 전용
        private List<string> specialChatLog = new List<string>();

        // 발언자의 턴에 입력한 특수채팅 단일
        public string SpecialChat { get; set; }

        private const int ChatKeep = 12;

        // 한 소켓에 동시에 두 번 보내면 안 된다.
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);

        #region 라이어 게임 필드
        
        // NewGameConfig 저장할 클래스 (아직 쓴적 없음, 아마 라운드 지나갈때마다 라운드 표시 UI로 쓸 수 있을듯)
        public NewGameConfig NewGameConfig;

        // 동적으로 변하는 플레이어 상태
        public PlayerState PlayerState = new PlayerState();
  
        // 서버한테 메세지 받을때마다 갱신되는 정보

        // 메세지로 받은 현재 페이즈 (처음엔 null값, Enum은 값타입이라 null을 못가져서 ? 붙여서 null 가질수 있게함)
        // 제일 중요한 필드!!!
        public TurnMessageType? CurrentState { get; private set; } = null;
        
        public float TimerMs { get; private set; }
        public int CurrentCycle { get; private set; }
        public int CurrentRound { get; private set; }
        // TimerMs를 받은 그 때의 시간 (Time.time)
        private float phaseStartTime;
        
        // 지금 상태에서 남은 시간(초).
        public float RemainingSeconds => Mathf.Max(0f, TimerMs / 1000f - (Time.time - phaseStartTime));

        // 발언차례 / 장르 ID / 키워드 ID
        public string CurrentOwnerID { get; private set; } = "";
        public string Genre { get; private set; }
        public string Keyword { get; private set; }

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

            if (type != "state") Debug.Log($"[방] 받음 : {type}");

            switch (type)
            {
                case "welcome": OnWelcome(JsonConvert.DeserializeObject<WelcomeMessage>(json)); break;
                case "join": OnJoin(JsonConvert.DeserializeObject<JoinMessage>(json)); break;

                case "ready": OnReady(JsonConvert.DeserializeObject<ReadyMessage>(json)); break;
                case "AllReady": OnAllReady(); break;
                case "게임 시작 확인": OnGameStartOK(JsonConvert.DeserializeObject<GameStartOKMessage>(json)); break;

                // 채팅 3종(normalChat/specialChat/KeywordChat)이 ChatMessage 하나로 통합됨
                case "chat":
                    ParseNetworkManager.NetworkManager.MessageParse(JsonConvert.DeserializeObject<ChatMessage>(json));
                    break;

                // 턴 상태 메세지 전부가 TurnMessage 하나로 통합됨 (TurnMessageType으로 내부 분기), 나머지는 그대로인듯
                case "turnMessage":
                    ParseNetworkManager.NetworkManager.MessageParse(JsonConvert.DeserializeObject<TurnMessage>(json));
                    break;
                
                // 상호 작용
                case "userInteraction":
                    ParseNetworkManager.NetworkManager.MessageParse(JsonConvert.DeserializeObject<UserInteractionMessage>(json));
                    break;
                
                // 밀기 애니메이션 적용
                case "PushAnimation":
                    OnPushAnimationMessage(JsonConvert.DeserializeObject<PushAnimationMessage>(json));
                    break;

                case "Select": OnSelect(JsonConvert.DeserializeObject<SelectMessage>(json)); break;
                case "NonPoint": OnNonPointMessage(JsonConvert.DeserializeObject<NonPointMessage>(json)); break;
                case "Vote": OnVoteMessage(JsonConvert.DeserializeObject<VoteMessage>(json)); break;

                case "leave": OnLeave(JsonConvert.DeserializeObject<LeaveMessage>(json)); break;
                case "state": OnState(JsonConvert.DeserializeObject<StateMessage>(json)); break;

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
            
            // 이미 있으면 지움
            if (localPlayer != null) Destroy(localPlayer.gameObject);
            // 프리팹으로 로컬 플레이어 생성
            localPlayer = Instantiate(LocalPlayerPrefab, Vector3.zero, Quaternion.identity);
            
            // 마트 카메라 추적 로직 중 Target이 오브젝트 이름으로 추적함
            localPlayer.gameObject.name = "Player";
            
            // [변경] 컬러도 함께 넘김
            //localPlayer.Setup(Me, ColorOf(Me.Id));

            // 누가 들어올때마다 순서 초기화하고처음부터 다시세운다.
            userOrder.Clear();
            for (int i = 0; i < message.Users.Length; i++)
            {
                Debug.Log($"[LiarClient] {message.Users[i].Id}, {message.Users[i].NickName}");
            }
            if (message.Users != null)
            {
                foreach (User user in message.Users)
                {
                    userOrder.Add(user.Id);
                    Spawn(user);
                }
            }
            userOrder.Add(Me.Id);
            DoResponseAll(); // 플레이어위치 변경
            // [변경] 컬러도 함께 넘김
            localPlayer.Setup(Me, (colorSet.GetColor(Me.Id)));
            DoSetPlayer(MEETINGSTATE);

            Debug.Log($"[방] {RoomCode} 에 들어왔다. 내 번호는 {Me.Id}, 이미 {message.Users.Length}명 있다");

            CommonUIManager.Instance?.ShowChat(); // 채팅창 활성화
            LobbyUIManager.Instance?.ShowRoomGroup(); // 방 그룹 활성화

            ReadyCount = message.ReadyCount;
            LobbyUIManager.Instance?.RefreshRoomInfo(); // 방 정보 갱신
        }

        private void OnJoin(JoinMessage message)
        {
            userOrder.Add(message.User.Id); // 순서 리스트에 추가

            Spawn(message.User);
            DoSetPlayer(MEETINGSTATE);

            Debug.Log($"[방] {message.User.NickName}({message.User.Id}) 이(가) 들어왔다");

            //Vote_System.Instance.SetUserCount(remotes); //모든 유저 카운트알림 다른유저 + 본인
            Debug.Log($"총 유저 수 전달! {remotes.Count + 1}");
            LobbyUIManager.Instance?.RefreshRoomInfo(); // 누가 들어올때마다 방 정보가 갱신된다 (사람 몇명있는지)
        }

        private void OnLeave(LeaveMessage message)
        {
            if (remotes.TryGetValue(message.Id, out RemotePlayer remote) == false) return;
            
            if (message.Id == Me.Id) Destroy(localPlayer.gameObject); // 메세지의 Id가 나의 Id면 오브젝트 삭제
            
            remotes.Remove(message.Id);
            userOrder.Remove(message.Id); // 순서 에서 제거

            //Vote_System.Instance.SetUserCount(remotes); //모든 유저 카운트알림 다른유저 + 본인
            Destroy(remote.gameObject);

            Debug.Log($"[방] {message.Id} 이(가) 나갔다");
            LobbyUIManager.Instance?.RefreshRoomInfo(); // 사람이 나가도 방 정보 갱신
        }

        // 일반 채팅. ChatMessage(ChatType.Normal) 이벤트로 받음
        private void OnNormalChat(string id, string nickName, string text)
        {
            string mine = Me != null && id == Me.Id ? "(나)" : "";
            string line = $"[일반] {RichTextUtils.ToColoredRichText(nickName + mine + " : " + text, colorSet.GetColor(id))}"; // 채팅창 로그 표시
            chatLog.Add(line); // 로그에 추가
            if (chatLog.Count > ChatKeep) chatLog.RemoveAt(0);
            CommonUIManager.Instance?.AppendChatLine(line); // 채팅창 라인 추가
            Debug.Log($"[일반] {id} : {text}");
        }

        public int ReadyCount { get; private set; } = 0;
        public bool IsReady;
        private void OnReady(ReadyMessage ready)
        {
            if(ready.ID == Me.Id) IsReady = true;
            ReadyCount = ready.ReadyCount;
            LobbyUIManager.Instance?.UpdateReadyCountUI(ReadyCount,userOrder.Count - 1);
        }

        public void RequestReadyMessage()
        {
            SendAsync(new ReadyMessage{ID = Me.Id}).Forget();
        }
        private void OnAllReady()
        {
            LobbyUIManager.Instance?.ActivateGameStartButton(true);
        }
        private void OnGameStartOK(GameStartOKMessage message)
        {
            Debug.Log("게임 시작 완료 메세지를 받고 config 데이터 저장");
            
            ReadyCount = 0;
            IsReady = false;

            NewGameConfig = message.newGameConfig;
            IsGameStarted = true;
            UserScoreInfo = null; // 이전 게임 점수 초기화
            genre = ""; //장르 초기화
            keyWord = ""; //키워드 초기화
            LobbyUIManager.Instance?.RefreshRoomInfo(); // 방 시작 시 정보 갱신
        }

        private void OnGameStartState(float timerMs, int cycle, int round)
        {
            OnGameStartStateAsync(timerMs, cycle, round);
        }

        private async UniTask OnGameStartStateAsync(float timerMs, int cycle, int round)
        {
            await DoLoadScene(CONFERENCESCENE); // 게임 시작하면 회의실로 이동
         
            DoSetPhase(TurnMessageType.GameStartState, timerMs, cycle, round);
            DoSetPlayer(MEETINGSTATE); // 회의실에 앉은 상태
            CommonUIManager.Instance?.ShowTimer(); // 타이머 보여줌
        }

        private void OnGenreAssignAndLiarSelectState(float timerMs, int cycle, int round, int genreId,
            string liarId)
        {
            DoSetPhase(TurnMessageType.GenreAssignAndLiarSelectState, timerMs, cycle, round, liarId);
            Genre = DataManager.Instance.GetGenreDef(genreId).GenreName;
            this.genre = Genre; //장르 사용위해 저장 this는 분별용
            // 매 라운드 시작마다 오는 상태라 여기서도 켜준다.
            // 라운드 끝날 때 로비 경유하면서 HideTimer가 불려서 2라운드부턴 안 켜지던 버그
            CommonUIManager.Instance?.ShowTimer();

            Debug.Log($"라이어의 ID는 :{liarId} 입니다.");

            // 라이어의 Id는 메세지로 받은 Id임
            LiarId = liarId;
        }

        private void OnKeywordDistributeState(float timerMs, int cycle, int round, int keywordId)
        {
            DoSetPhase(TurnMessageType.KeywordDistributeState, timerMs, cycle, round);

            Debug.Log(keywordId);

            // 메세지로 온 키워드 ID 저장
            Keyword = DataManager.Instance.GetKeyWordDef(keywordId).KeywordName;
            this.keyWord = Keyword; // 헷갈려서 this붙임
            CommonUIManager.Instance?.ShowKeywordReveal(Genre, Keyword, round, NewGameConfig.MaxRound); // commonUIManager로 옮김
        }

        private void OnMartEnterState(float timerMs, int cycle, int round, string questId, CategoryItemArray[] allCategoryItemArrays) 
        {
            OnMartEnterStateAsync(timerMs, cycle, round, questId, allCategoryItemArrays).Forget();
        }
        private async UniTask OnMartEnterStateAsync(float timerMs, int cycle, int round, string targetItemId, CategoryItemArray[] allCategoryItemArrays)
        {
            DoSetPhase(TurnMessageType.MartEnterState, timerMs, cycle, round);
            CommonUIManager.Instance?.HideChat(); // 마트 진입시 채팅창 숨김
            CommonUIManager.Instance?.HideKeywordReveal(); // 마트 진입하면 키워드 공개 끔
            await DoLoadScene(MARTSCENE);
            DoSetPlayer(READYSTATE);
            await UniTask.WaitUntil(() => DisplayStandManager.Instance != null && DisplayStandManager.Instance.IsReady);

            for (int i = 0; i < allCategoryItemArrays.Length; i++)
                Debug.Log($"받은 아이템 정보 : 아이템 카테고리 {allCategoryItemArrays[i].Category}, 아이템 내역 {allCategoryItemArrays[i].ItemIds} ");
            
            await DisplayStandManager.Instance.SetItemIdBufferForArea(allCategoryItemArrays); // 배치(비동기) 완전 종료까지 대기
            MartQuestManager.Instance.SetPlayerMartQuest(targetItemId);
            MartManager.Instance.SetMartCategoryStand();
            MartManager.Instance.SetGenreAndKeyword(genre, keyWord);
        }
        private void OnMartMoveState(float timerMs, int cycle, int round)
        {
            DoSetPhase(TurnMessageType.MartMoveState, timerMs, cycle, round);
            DoSetPlayer(MARTSTATE); //모든 플레이어 설정변경: ex)마트 무브x, 회의실 무브O
        }

        // 로컬 유저가 가진 아이템 리스트
        public string[] MeItemList { get; set; } // 카테고리별로 1개씩

        // 이번 라운드 내 퀘스트 성공 / 실패 여부 
        public bool QuestSuccess { get; private set; } 

        // 마트 복귀 시 서버가 준 유저별 아이템 ID 목록. 
        // <유저 id / 아이템 id 배열>
        private Dictionary<string, string[]> itemListsByUser = new Dictionary<string, string[]>();

        // 씬을 이동하는 OnMartEnterState와 동일한 구조로 async 구조 래핑 진행
        private void OnMartReturnState(float timerMs, int cycle, int round, 
            Protocol.UserItemList[] userItemLists, Protocol.UserQuestInfo[] userQuestInfos) 
        {
            OnMartReturnStateAsync(timerMs, cycle, round, userItemLists, userQuestInfos).Forget(); 
        }

        // 씬 로드가 끝나기 전에 다음 상태(발언 등)가 와서 MeetingRoomUIManager가
        // 아직 없는 상태로 호출되는 걸 막기 위해 씬 로드를 기다린다.
        private async UniTask OnMartReturnStateAsync(float timerMs, int cycle, int round, 
            Protocol.UserItemList[] userItemLists, Protocol.UserQuestInfo[] userQuestInfos)
        {
            DoSetPhase(TurnMessageType.MartReturnState, timerMs, cycle, round);
            
            // 여기 마트복귀했을때 플레이어/remote의 리스트에 쌓인 아이템 클리어하고 손에든 아이템도 지운다.
            for (int i = 0; i < userItemLists.Length; i++)
            {
                Interaction_Player player = GetPlayer(userItemLists[i].UserId);
                player.ClearSelectedItems();
                ItemInteractionHandler handler = player.GetComponent<ItemInteractionHandler>();
                handler.ClearHandItem();
            }
            
            // 디버깅용 로그 (퀘스트 성공 / 실패 여부)
            foreach (UserQuestInfo userQuestInfo in userQuestInfos)
            {
                if (userQuestInfo.UserId != Me.Id) continue; 
                QuestSuccess = userQuestInfo.IsSuccess; 
                Debug.Log(QuestSuccess ? "퀘스트 성공!" : "퀘스트 실패!"); 
            }

            foreach (UserItemList userItemList in userItemLists)
            {
                // 내 이름과 일치하면 그 아이템 배열 가져옴
                if (userItemList.UserId == Me.Id) MeItemList = userItemList.ItemList;

                itemListsByUser[userItemList.UserId] = userItemList.ItemList;
            }

            // 디버깅용 로그 (내가 가진 아이템 목록 보기)
            Debug.Log(string.Join(",", MeItemList));

            // 씬 이동을 먼저 끝내고, 그 다음에 UI를 켠다 (MeetingRoomUIManager가 씬에 있어야 호출이 먹힘) 
            DoSetPlayer(MEETINGSTATE); //모든 플레이어 설정변경: ex)마트 무브x, 회의실 무브O
            await DoLoadScene(CONFERENCESCENE); 

            CommonUIManager.Instance?.ShowChat(); // 마트 복귀할때 채팅 켜짐
            CommonUIManager.Instance?.EnableTabInfo(); // tab키 정보창은 마트가 아니라 미팅룸 복귀 시점부터 활성화
            MeetingRoomUIManager.Instance?.ShowQuestResult(QuestSuccess); // 퀘스트 성공/실패 UI
            MeetingRoomUIManager.Instance?.ShowGenreKeyword(); // 장르/키워드 상시표시 시작 
        }

        // 현재 발언자
        public string CurrentFocusID { get; set; }
        // 현재 카테고리
        public string CurrentCategory { get; set; }
        
        // 특수채팅 초기화용 필드
        private int lastClearedRound = -1;
        
        private void OnShowItemAndSpeakState(float timerMs, int cycle, int round, string currentFocusID,
            string currentCategory)
        {
            // 같은 사이클이면 초기화 안함, 라운드가 다르면 초기화함
            if (round != lastClearedRound) 
            {
                specialChatLog.Clear();
                lastClearedRound = round;
            }

            DoSetPhase(TurnMessageType.ShowItemAndSpeakState, timerMs, cycle, round, currentFocusID);
            SpecialChat = null;
            CurrentFocusID = currentFocusID; // 발언자 닉네임

            // 서버에서 현재 카테고리 받기
            CurrentCategory = currentCategory;
            
            hasSentSpecialChat = false;
            IsNotLiarPushCycle = false; // 새 사이클 진입 시 알림 초기화

            string? itemName = DoGetItem(currentFocusID, currentCategory);

            Debug.Log($"[발언] {GetNickName(currentFocusID)} 이(가) {currentCategory} 카테고리의 '{itemName}' 을(를) 보여주며 발언 중");

            /*// 조명을 모두 끈다
            LightManager.Instance?.TurnOffSpotLight();
            // 현재 발언자의 pivot transform 위치에 spotlight를 비친다.
            LightManager.Instance?.TurnOnSpotLight(GetPointPivot(currentFocusID));*/
            
            // 현재 발언자의 pivot transform 위치에 spotlight를 비친다.
            LightManager.Instance?.TurnOnDotween(GetPointPivot(currentFocusID));
            
            // 현재 포커싱된 사람 닉네임, 아이템(없으면 ""), 현재 카테고리, 현재 사이클, 최종 사이클
            MeetingRoomUIManager.Instance?.ShowSpeakerNotice(GetNickName(currentFocusID), itemName ?? "", currentCategory, cycle, NewGameConfig.MaxCycle);
        }

        // 발언 상태에서 발언자의 첫 채팅이 특수채팅
        private void OnSpecialChat(string id, string nickName, string text)
        {
            // 다음 발전자에서 단일 특수채팅 초기화
            SpecialChat = null;

            string mine = Me != null && id == Me.Id ? " (나)" : "";
            string line = $"[특수] {nickName}{mine} : {text}"; // 일반 채팅창용 (아이템 표시 없음)

            chatLog.Add(line);
            if (chatLog.Count > ChatKeep) chatLog.RemoveAt(0);
            CommonUIManager.Instance?.AppendChatLine(line); // 채팅 라인 추가

            // 단일로 저장
            SpecialChat = text;

            // TAB 정보창용 특수채팅로그
            string itemName = DoGetItem(id, CurrentCategory); 
            specialChatLog.Add($"[특수] {nickName}({itemName}){mine} : {text}"); // 현재 카테고리의 아이템 이름 시각화

            Debug.Log($"[특수] {id} : {text}");
        }

        private void OnSpeechEndState(float timerMs, int cycle, int round)
        {
            DoSetPhase(TurnMessageType.SpeechEndState, timerMs, cycle, round);

            Debug.Log("현재 발언자의 특수채팅 : " + SpecialChat);
            MeetingRoomUIManager.Instance?.ShowSpeechEndNotice(GetNickName(CurrentFocusID), SpecialChat); 
        }

        private void OnPointAtSuspectState(float timerMs, int cycle, int round)
        {
            DoSetPhase(TurnMessageType.PointAtSuspectState, timerMs, cycle, round);
            //여기 지목단계 시작
            Vote_System.Instance?.SetTurnType(Vote_System.TurnType.Point, timerMs);
            
            //maxCycle 정의가 없는거같아서 일단 하드코딩. 나중에 바꿀 수 있음
            if(cycle != 3) ConferenceManager.Instance?.RequestSkipPointUI(true); // 스킵 버튼 켜기
            
            // 지목단계일 시 테이블에 조명을 포커싱한다.
            LightManager.Instance?.TurnOnFocusTable();
            
            MeetingRoomUIManager.Instance?.ShowPointAtNotice(); // 라이어 지목 공지 UI
        }

        private void OnPointAtSuspectEndState(float timerMs, int cycle, int round, string mostSelectedId)
        {
            DoSetPhase(TurnMessageType.PointAtSuspectEndState, timerMs, cycle, round, mostSelectedId);
            //여기가 지목단계 종료
            Vote_System.Instance?.SetTurnType(Vote_System.TurnType.Wait, timerMs);
            ConferenceManager.Instance?.RequestSkipPointUI(false); // 스킵 버튼 끄기
            ConferenceManager.Instance?.ClearAllArrows(); // 모든 화살표 지우기

            MostSelectId = mostSelectedId; //변론할 사람

            MeetingRoomUIManager.Instance?.ShowPointAtResult(string.IsNullOrEmpty(mostSelectedId) ? "" : GetNickName(mostSelectedId)); // 변론중인 사람이 잇으면 닉네임, 없으면 ""

            if (string.IsNullOrEmpty(mostSelectedId))
            {
                Debug.Log($"투표가 스킵되었습니다.");
                // 스킵되면 다시 발언 상태로 돌아가니까 테이블 조명으로 전환 !
                LightManager.Instance?.TurnOnFocusTable(); 
            }
            else
            {
                Debug.Log($"가장 많이 지목된 유저 {mostSelectedId}");
                Vote_System.Instance?.SetPointTarget(mostSelectedId);
                LightManager.Instance?.TurnOnDotween(GetPointPivot(mostSelectedId)); // 가장 많이 지목받은 사람 조명 포커싱 
            }
        }

        private void OnDebateTimeState(float timerMs, int cycle, int round)
        {
            DoSetPhase(TurnMessageType.DebateTimeState, timerMs, cycle, round);
            Debug.Log("변론 단계 시작");
            MeetingRoomUIManager.Instance?.ShowDebateNotice(GetNickName(MostSelectId)); // 변론중인 사람 닉네임
        }

        private void OnDebateEndState(float timerMs, int cycle, int round)
        {
            DoSetPhase(TurnMessageType.DebateEndState, timerMs, cycle, round);
            Debug.Log("변론 끝");
            MeetingRoomUIManager.Instance?.ShowDebateEndNotice(); // "변론이 끝났습니다."
        }

        private void OnVoteState(float timerMs, int cycle, int round)
        {
            DoSetPhase(TurnMessageType.VoteState, timerMs, cycle, round);
            //여기가 투표 시작
            Vote_System.Instance?.SetTurnType(Vote_System.TurnType.Vote, timerMs);
            if (Vote_System.Instance?.PointTarget.Id != Me.Id) ConferenceManager.Instance?.RequestVoteUI(true);
            else Vote_System.Instance?.SetTurnType(Vote_System.TurnType.Wait, timerMs);

            // 턴 패널 비활성화
            MeetingRoomUIManager.Instance?.ClearTurnNotices(); 
        }

        private void OnVoteEndState(float timerMs, int cycle, int round)
        {
            DoSetPhase(TurnMessageType.VoteEndState, timerMs, cycle, round);
            //여기가 투표 종료
            Vote_System.Instance?.SetTurnType(Vote_System.TurnType.Wait, timerMs);
            ConferenceManager.Instance?.RequestVoteUI(false);
            //투표 종료할 때 PointTarget 초기화
            Vote_System.Instance?.ResetPointTarget();
        }

        private void OnLiarConfirmedState(float timerMs, int cycle, int round, string electedId)
        {
            DoSetPhase(TurnMessageType.LiarConfirmedState, timerMs, cycle, round, electedId);

            // 최종 선택된 사람 = MostSelectId
            // 지목종료(PointAtSuspectEndState) 때부터 갖고 있던 MostSelectId(투표받은 사람)를 그대로 쓴다.
            Debug.Log($"확정된 유저는 {MostSelectId} 입니다.");

            Debug.Log($"라이어는 {GetNickName(LiarId)} 입니다.");
            
            LightManager.Instance?.TurnOnLiar(GetPointPivot(LiarId)); // 라이어 조명 포커싱
            MeetingRoomUIManager.Instance?.ShowLiarConfirmed(GetNickName(MostSelectId), GetNickName(LiarId)); 
        }

        private void OnLiarKeywordGuessState(float timerMs, int cycle, int round)
        {
            DoSetPhase(TurnMessageType.LiarKeywordGuessState, timerMs, cycle, round);

            hasSentKeywordChat = false;
            KeywordChatText = "";
            MeetingRoomUIManager.Instance?.ShowLiarGuessingNotice(); // 라이어 키워드 맞춤상태 공지
        }

        private void OnLiarOutButtonPressedState(float timerMs, int cycle, int round, string liarId)
        {
            DoSetPhase(TurnMessageType.LiarOutButtonPressedState, timerMs, cycle, round);
            
            LightManager.Instance?.TurnOnLiar(GetPointPivot(liarId)); // 라이어 조명 포커싱
            MeetingRoomUIManager.Instance?.ShowLiarOutAnnounce(GetNickName(liarId)); // 라이어의 라밍아웃 버튼 누름 공지
        }

        // 라이어의 키워드 맞춤 상태에서 라이어의 첫 채팅이 키워드채팅. 
        private void OnKeywordGuessChat(string id, string nickName, string text)
        {
            string mine = Me != null && id == Me.Id ? " (나)" : "";
            string line = $"[키워드] {nickName}{mine} : {text}"; 
            chatLog.Add(line); // 로그에 추가
            if (chatLog.Count > ChatKeep) chatLog.RemoveAt(0);
            CommonUIManager.Instance?.AppendChatLine(line); // 채팅창 UI에 반영
            Debug.Log($"[키워드] {nickName} : {text}");

            KeywordChatText = text;
        }

        // 라이어 정답이 맞는지 틀리는지 판별하는 bool값
        public bool IsRightAnswer { get; set; }
        
        private void OnLiarKeywordGuessEndState(float timerMs, int cycle, int round, string liarKeyword,
            string normalKeyword, bool isRightAnswer, UserScoreInfo[] userScoreInfo)
        {
            DoSetPhase(TurnMessageType.LiarKeywordGuessEndState, timerMs, cycle, round);
            LiarKeyword = liarKeyword; // 라이어 키워드 저장
            NormalKeyword = normalKeyword; // 일반 키워드 저장
            IsRightAnswer = isRightAnswer; // 라이어 키워드 맞춤여부
            
            specialChatLog.Clear(); // 현재까지의 특수 채팅 로그 초기화

            LiarButtonClicked = false; // 라밍아웃 버튼 눌림상태 해제
            
            Debug.Log($"라이어 {GetNickName(LiarId)}는 키워드를 {(IsRightAnswer ? "맞췄습니다" : "못맞췄습니다")}");
            
            UserScoreInfo = new UserScoreInfo[userScoreInfo.Length]; // userScoreInfo 인스턴스 생성
            // UI에 표시한다.
            UserScoreInfo = userScoreInfo; // 서버에서 온 스코어 정보를 클라의 UserScoreInfo에 저장한다. 

            for (int i = 0; i < UserScoreInfo.Length; i++)
            {
                Debug.Log($"유저의 ID는 {UserScoreInfo[i].UserId} 이고, 점수는 {UserScoreInfo[i].UserScore} 입니다.");
            }

            MeetingRoomUIManager.Instance?.ShowKeywordGuessResult(KeywordChatText, liarKeyword, normalKeyword); // 라이어의 키워드채팅, 라이어 키워드, 시민 키워드
            MeetingRoomUIManager.Instance?.ShowScoreTally(); // 점수 공개
            MeetingRoomUIManager.Instance?.HideGenreKeyword(); // 장르/키워드 상시표시 종료 
        }

        private void OnScoreTallyState(float timerMs, int cycle, int round, string[] liarOutButtonInfo,
            UserScoreInfo[] userScoreInfo)
        {
            DoSetPhase(TurnMessageType.ScoreTallyState, timerMs, cycle, round);

            UserScoreInfo = new UserScoreInfo[userScoreInfo.Length];

            // UI에 표시한다
            UserScoreInfo = userScoreInfo; // userScoreInfo 최신값 받기

            for (int i = 0; i < UserScoreInfo.Length; i++)
            {
                Debug.Log($"유저의 ID는 {UserScoreInfo[i].UserId} 이고, 점수는 {UserScoreInfo[i].UserScore} 입니다.");
            }

            LightManager.Instance?.TurnOnFocusTable();
            
            MeetingRoomUIManager.Instance?.ClearTurnNotices(); // 모든 UI 끄기
            MeetingRoomUIManager.Instance?.ShowScoreTally(); // 점수 공개
        }

        private void OnScoreTallyEndState(float timerMs, int cycle, int round)
        {
            DoSetPhase(TurnMessageType.ScoreTallyEndState, timerMs, cycle, round);
            CommonUIManager.Instance?.DisableTabInfo(); // 점수 집계 종료되면 Tab 꺼짐
            MeetingRoomUIManager.Instance?.HideScoreTally(); // 점수 공개 멈춤
        }

        private void OnFinalResultState(float timerMs, int cycle, int round, string[] currentOwnerIDs)
        {
            DoSetPhase(TurnMessageType.FinalResultState, timerMs, cycle, round);

            WinnerUserID = new List<string>(currentOwnerIDs); // 우승자 List 받기

            // 최종라운드 후 게임 완전 종료가 되서 점수를 초기화한다.
            foreach (string winner in currentOwnerIDs)
            {
                Debug.Log($"우승자는 {winner} 입니다.");
            }

            LightManager.Instance?.TurnOnWinners(GetPointPivot(WinnerUserID)); // 우승자 조명 강조
            MeetingRoomUIManager.Instance?.ShowWinOrDefeat(WinnerUserID.Contains(Me.Id)); // 승리 or 패배
        }

        private void OnFinalResultEndState(float timerMs, int cycle, int round) 
        {
            OnFinalResultEndStateAsync(timerMs, cycle, round).Forget(); 
        }

        private async UniTask OnFinalResultEndStateAsync(float timerMs, int cycle, int round) 
        {
            DoSetPhase(TurnMessageType.FinalResultEndState, timerMs, cycle, round);

            List<string> winnerNickNames = new List<string>();

            foreach (string winnerId in WinnerUserID)
            {
                winnerNickNames.Add(GetNickName(winnerId));
            }

            MeetingRoomUIManager.Instance?.ShowFinalResult(winnerNickNames); 

            // 유저 스코어 초기화 (최종 라운드 종료되어서 점수가 null으로 초기화)
            UserScoreInfo = null;

            IsGameStarted = false; // 게임시작 아님

            // 제한시간에서 3초 남을 때까지 결과 화면을 보여주고 로비로 이동 
            float delay = Mathf.Max(0f, RemainingSeconds - 3f); 
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: destroyCancellationToken);

            LightManager.Instance?.TurnOffWinners(); // 로비 이동하기 전에 다 끔

            await DoLoadScene(LOBBYSCENE);
            CommonUIManager.Instance?.HideTimer(); 
            DoSetPlayer(READYSTATE);
        }

        #region 투표 처리

        private void OnSelect(SelectMessage message)
        {
            if (message.IsSelectCancel == false)
            {
                Debug.Log($"{message.selectID}가 {message.selectedID}를 지목했습니다.");
                Transform fromPivot = GetPointPivot(message.selectID);
                Transform toPivot = GetPointPivot(message.selectedID);

                Color arrowColor = (colorSet.GetColor((message.selectID))); // 지목한 사람마다 고유 색
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

        #region 마트 처리

        // 다른 사람의 밀기 애니메이션 수신
        private void OnPushAnimationMessage(PushAnimationMessage message)
        {
            if (Me != null && message.UserID == Me.Id) return; // 내가 보낸 것은 무시

            if (remotes.TryGetValue(message.UserID, out RemotePlayer remote))
                remote.PlayPushAnimation();
        }
        
        // 서버에서 밀치기 결과 수신
        private void OnPushAnswer(bool isValid, bool isSuccess, string senderId, string receivedId)
        {
            if (isValid == false) return;
            if (isSuccess == false) return;

            Interaction_Player sender = GetPlayer(senderId);
            Interaction_Player receiver = GetPlayer(receivedId);

            // 밀쳐낼 방향 계산 (XZ 평면 기준 정규화)
            Vector3 hitDir = (receiver.transform.position - sender.transform.position);
            hitDir.y = 0.0f;
            hitDir.Normalize();
            
            InteractionEvent @event = new InteractionEvent
            {
                HitDirection = hitDir,
                PushPower = receiver.PushPower,
                Position = receiver.transform.position
            };
            
            Interaction_System.Instance.To(sender, receiver, @event);
        }
        
        // 집기 결과
        private void OnItemHoldAnswer(bool isValid, bool isSuccess, string senderId, string receivedId)
        {
            // 레이스 컨디션에서 진 경우(다른 사람이 먼저 집음) → 완전히 무시
            if (isValid == false) return;

            if (isSuccess == false)
            {
                Debug.Log($"[아이템] {senderId} 집기 실패 (itemId:{receivedId})");
                return;
            }

            Interaction_Player player = GetPlayer(senderId);
            ItemInteractionHandler handler = player.GetComponentInChildren<ItemInteractionHandler>();
            if (handler == null) return;

            if (int.TryParse(receivedId, out int itemId))
                handler.SuccessGetItem(itemId);
        }

        // 가방 넣기 결과 (교체 정보 포함)
        private void OnItemPutInBagAnswer(bool isValid, bool isSuccess, string senderId, string receivedId, string changedItemId)
        {
            if (isValid == false) return;

            Interaction_Player player = GetPlayer(senderId);
            ItemInteractionHandler handler = player.GetComponentInChildren<ItemInteractionHandler>();
            if (handler == null) return;

            handler.SuccessPutDownItemInBag(isSuccess, changedItemId);
        }

        private void OnItemReturnInStandAnswer(bool isValid, bool isSuccess, string senderId, string receivedId)
        {
            if (isValid == false) return;
            Interaction_Player player = GetPlayer(senderId);
            ItemInteractionHandler handler = player.GetComponentInChildren<ItemInteractionHandler>();
            if (handler == null) return;

            //돌려놓기는 손에든 아이템을 돌려놓는건데 아이템자체에 되돌리기 코드있어서 receivedId가 쓸모없어지는듯?
            handler.SuccesReturnItemInStand(isSuccess);
        }
        
        #endregion

        // 지속적으로 계속 돈다.
        private void OnState(StateMessage message)
        {
            // 로컬 플레이어 정보 동기화
            foreach (PlayerState state in message.States)
            {
                // Debug.Log($"[state 수신] id={state.Id} pos=({state.X:F2}, {state.Y:F2}, {state.Z:F2})");

                if (state.Id == Me.Id)
                {
                    // PlayerState.score = state.score;
                    PlayerState.IsLiar = state.IsLiar;

                    continue;
                }

                // 다른 플레이어 정보 동기화
                if (remotes.TryGetValue(state.Id, out RemotePlayer remote) == false) continue;
                remote.SetTarget(new Vector3(state.X, state.Y, state.Z));

                // remote.PlayerState.score = state.score;
                remote.PlayerState.IsLiar = state.IsLiar;
            }
        }

        private void Spawn(User user)
        {
            // 이미 있으면 만들지 않는다. welcome 과 join 이 겹칠 수 있다.
            if (remotes.ContainsKey(user.Id)) return;
            RemotePlayer remote = Instantiate(remotePrefab, Vector3.zero, Quaternion.identity);

            remote.Setup(user, colorSet.GetColor(user.Id));   // [변경] 컬러 전달

            remotes[user.Id] = remote;
            // 이미 마트/회의실에 들어와 있는 상태라면 새로 들어온 사람까지 포함해서 즉시 재배치
            DoResponseAll();
        }

        #endregion

        #region 클라이언트 내부처리 (Do)

        // 메세지 내부 정보 받기
        private void DoSetPhase(TurnMessageType type, float timerMs, int currentCycle, int currentRound,
            string currentOwnerID = "")
        {
            CurrentState = type;
            TimerMs = timerMs;
            CurrentCycle = currentCycle;
            CurrentRound = currentRound;
            CurrentOwnerID = currentOwnerID;
            phaseStartTime = Time.time;
        }

        private async UniTask DoLoadScene(string sceneName)
        {
            if (activeContentScene == sceneName) return; // 이미 그 씬이면 다시 부르지 않는다.

            string previous = activeContentScene;
            activeContentScene = sceneName;

            await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            
                Scene loaded = SceneManager.GetSceneByName(sceneName);
                SceneManager.SetActiveScene(loaded);
            await SceneManager.UnloadSceneAsync(previous);
            
            DoResponseAll(); // 씬이 완전히 로드되어 매니저가 Awake를 끝낸 뒤에 전원 재배치
        }

        private void DoSetPlayer(string playerState)
        {
            Debug.Log($"플레이어 상태 {playerState}");
            localPlayer.SetPlayerWithScenes(playerState);
            foreach (RemotePlayer remote in remotes.Values)
            {
                remote.SetPlayerWithScenes(playerState);
            }
        }

        // TAB 정보창에서 현재 라운드의 카테고리별 아이템"들"을 string으로 보여주는 로직
        public string GetMyItemList() 
        {
            if (MeItemList == null) return ""; 

            List<string> lines = new List<string>(); 
            foreach (string idText in MeItemList) 
            {
                if (int.TryParse(idText, out int itemId) == false) continue; 

                ItemDef itemDef = DataManager.Instance.GetItemDef(itemId); 
                if (itemDef != null) lines.Add($"{itemDef.CategoryType} : {itemDef.ItemName}"); 
            }

            return string.Join("\n", lines); 
        }

        // userId가 이번 라운드 마트에서 고른 아이템 중, currentCategory에 해당하는 아이템 이름을 찾는다. !
        private string DoGetItem(string userId, string currentCategory)
        {
            if (itemListsByUser.TryGetValue(userId, out string[] itemIds) == false) return ""; // 딕셔너리 키 중에 유저id가 없으면 "" 리턴

            foreach (string idText in itemIds) // 아이템id 순차적으로 가져옴
            {
                if (int.TryParse(idText, out int itemId) == false) continue; // GetItemDef가 int형을 받아서 int로 파싱

                ItemDef itemDef = DataManager.Instance.GetItemDef(itemId); // 아이템 id로 itemdef 찾기
                if (itemDef != null && itemDef.CategoryType.ToString() == currentCategory) // 아이템의 카테고리랑 현재 카테고리가 일치하면
                    return itemDef.ItemName; // 아이템 이름 리턴
            }

            return ""; // 아니면 "" 리턴
        }

        private void DoResponseAll()
        {
            AsyncResponseAll();
        }
        // 현재 씬(마트/회의실)의 스폰 포인트를 가져와서 접속 순서대로 배치한다.
        private async UniTask AsyncResponseAll() //스폰좌표가 자꾸 문제생긴다 비동기 처리로 바꾸자
        {
            userOrder.Sort((a, b) =>
            {
                if (int.TryParse(a, out int idA) && int.TryParse(b, out int idB))
                    return idA.CompareTo(idB);
                return string.CompareOrdinal(a, b);
            }); // 위치바꾸기?

            Transform[] spawnPoints = new Transform[userOrder.Count];

            switch (activeContentScene)
            {
                case "00. LobbyRoom":
                    spawnPoints = LobbyManager.Instance.userSpawnPoints;
                    break;
                case "01. MeetingRoom":
                    spawnPoints = ConferenceManager.Instance.userSpawnPoints;
                    break;
                case "02. Mart":
                    spawnPoints = MartManager.Instance.userSpawnPoints;
                    break;
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
                Vector3 pos = spawnPoints[i]?.position ?? Vector3.zero;
                Quaternion rot = spawnPoints[i]?.rotation ?? Quaternion.identity;

                if (id == Me.Id)
                {
                    localPlayer.transform.position = pos;
                    localPlayer.Body.transform.position = pos;
                    localPlayer.Body.transform.rotation = rot;
                    // 다음 SendLoop 틱을 기다리지 않고 재배치된 좌표를 즉시 한 번 더 전송
                    SendAsync(new MoveMessage { Id = Me.Id, X = pos.x, Y = pos.y, Z = pos.z }).Forget();
                }
                else if (remotes.TryGetValue(id, out RemotePlayer remote))
                {
                    remote.Teleport(pos);
                    remote.Body.transform.position = pos;
                    remote.Body.transform.rotation = rot;
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
            // CreateNetworkManager의 팩토리 클래스에서 NormalChat 호출 => CreateSimple(ChatType.Normal)
            SendAsync(ChatMessageFactory.NormalChat(Me.Id, Me.NickName, text.Trim())).Forget();
        }

        private void RequestSpecialChat(string text)
        {
            // NormalChat과 만들어지는 방식은 동일
            SendAsync(ChatMessageFactory.SpecialChat(Me.Id, Me.NickName, text.Trim())).Forget();
        }

        private void RequestKeywordChat(string text)
        {
            // 위와 동일함
            SendAsync(ChatMessageFactory.KeywordGuessChat(Me.Id, Me.NickName, text.Trim())).Forget();
        }

        // 라밍아웃 버튼을 눌렀다. 실제 라이어인지는 서버가 검증한다.
        public void RequestLiarSelfDisclose()
        {
            if (Me == null) return;

            LiarButtonClicked = true;
            if (LiarId != Me.Id)
            {
                IsNotLiarPushCycle = true;
                MeetingRoomUIManager.Instance?.ShowNotLiarNotice(); // 라이어가 아닌 사람이 라밍아웃 버튼을 누름
            }

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

        #region 마트 처리

        // 아이템 집기 요청
        public void RequestTryGetItem(int itemId)
        {
            if (Me == null) return;

            var msg = InteractionMessageFactory.ItemHoldQuery(
                Protocol.InteractionType.ItemHoldQuery,
                IsValid: true,
                IsSuccess: false,
                senderId: Me.Id,
                receivedId: itemId.ToString());

            SendAsync(msg).Forget();
        }
        
        // 아이템 톨려놓기 요청
        public void RequestTryReturnItem(int itemId)
        {
            var msg = InteractionMessageFactory.ItemDropQuery(
                Protocol.InteractionType.ItemDropQuery,
                IsValid: true,
                IsSuccess: false,
                senderId: Me.Id,
                receivedId: itemId.ToString());
            
            SendAsync(msg).Forget();
        }
        
        // 가방에 넣기 요청
        public void RequestTryInputItemInBag(int itemId)
        {
            if (Me == null) return;

            var msg = InteractionMessageFactory.ItemPutInBagQuery(
                Protocol.InteractionType.ItemPutInBagQuery,
                IsValid: true,
                IsSuccess: false,
                senderId: Me.Id,
                receivedId: itemId.ToString(),
                changedItemId: null);

            SendAsync(msg).Forget();
        }
        
        // 밀기 애니메이션 요청 (실제로 맞았는지와 무관)
        public void RequestPushAnimation()
        {
            if (Me == null) return;
            SendAsync(new PushAnimationMessage { UserID = Me.Id }).Forget();
        }
        
        // 밀기 판정 요청
        public void RequestPushQuery(string receivedId)
        {
            if (Me == null) return;

            var msg = InteractionMessageFactory.PushQuery(
                Protocol.InteractionType.PushQuery,
                IsValid: true,
                IsSuccess: false,
                senderId: Me.Id,
                receivedId: receivedId
                );

            SendAsync(msg).Forget();
        }

        #endregion

        #endregion

        protected override void Awake()
        {
            base.Awake(); // 싱글턴 등록 + DontDestroyOnLoad. 중복 인스턴스면 여기서 파괴된다.

            activeContentScene = BootScene;
            colorSet = GetComponent<PlayerColorSet>();
            // true로 해둬야 백그라운드에서도 데이터 수신 가능
            Application.runInBackground = true;

            SubAllEvents(); // 구독 목록들 전부 구독
        }

        // Awake때 전부 구독한다.
        private void SubAllEvents()
        {
            ParseNetworkManager.NetworkManager.OnGameStartState += OnGameStartState;
            ParseNetworkManager.NetworkManager.OnGenreAssignAndLiarSelectState += OnGenreAssignAndLiarSelectState;
            ParseNetworkManager.NetworkManager.OnKeywordDistributeState += OnKeywordDistributeState;
            ParseNetworkManager.NetworkManager.OnMartEnterState += OnMartEnterState;
            ParseNetworkManager.NetworkManager.OnMartMoveState += OnMartMoveState;
            ParseNetworkManager.NetworkManager.OnMartReturnState += OnMartReturnState;
            ParseNetworkManager.NetworkManager.OnShowItemAndSpeakState += OnShowItemAndSpeakState;
            ParseNetworkManager.NetworkManager.OnSpeechEndState += OnSpeechEndState;
            ParseNetworkManager.NetworkManager.OnPointAtSuspectState += OnPointAtSuspectState;
            ParseNetworkManager.NetworkManager.OnPointAtSuspectEndState += OnPointAtSuspectEndState;
            ParseNetworkManager.NetworkManager.OnDebateTimeState += OnDebateTimeState;
            ParseNetworkManager.NetworkManager.OnDebateEndState += OnDebateEndState;
            ParseNetworkManager.NetworkManager.OnVoteState += OnVoteState;
            ParseNetworkManager.NetworkManager.OnVoteEndState += OnVoteEndState;
            ParseNetworkManager.NetworkManager.OnLiarConfirmedState += OnLiarConfirmedState;
            ParseNetworkManager.NetworkManager.OnLiarKeywordGuessState += OnLiarKeywordGuessState;
            ParseNetworkManager.NetworkManager.OnLiarKeywordGuessEndState += OnLiarKeywordGuessEndState;
            ParseNetworkManager.NetworkManager.OnScoreTallyState += OnScoreTallyState;
            ParseNetworkManager.NetworkManager.OnScoreTallyEndState += OnScoreTallyEndState;
            ParseNetworkManager.NetworkManager.OnFinalResultState += OnFinalResultState;
            ParseNetworkManager.NetworkManager.OnFinalResultEndState += OnFinalResultEndState;
            
            ParseNetworkManager.NetworkManager.OnLiarOutButtonPressedState += OnLiarOutButtonPressedState;
            
            // 마트 처리
            ParseNetworkManager.NetworkManager.OnItemHoldAnswer     += OnItemHoldAnswer;
            ParseNetworkManager.NetworkManager.OnItemDropAnswer += OnItemReturnInStandAnswer;
            ParseNetworkManager.NetworkManager.OnItemPutInBagAnswer += OnItemPutInBagAnswer;
            ParseNetworkManager.NetworkManager.OnPushAnswer += OnPushAnswer;
            
            //채팅 처리
            ParseNetworkManager.NetworkManager.OnNormalChat += OnNormalChat;
            ParseNetworkManager.NetworkManager.OnSpecialChat += OnSpecialChat;
            ParseNetworkManager.NetworkManager.OnKeywordGuessChat += OnKeywordGuessChat;
        }

        // 등록한 이벤트는 오브젝트가 사라지기 전에 해제한다
        private void UnSubAllEvents()
        {
            ParseNetworkManager.NetworkManager.OnGameStartState -= OnGameStartState;
            ParseNetworkManager.NetworkManager.OnGenreAssignAndLiarSelectState -= OnGenreAssignAndLiarSelectState;
            ParseNetworkManager.NetworkManager.OnKeywordDistributeState -= OnKeywordDistributeState;
            ParseNetworkManager.NetworkManager.OnMartEnterState -= OnMartEnterState;
            ParseNetworkManager.NetworkManager.OnMartMoveState -= OnMartMoveState;
            ParseNetworkManager.NetworkManager.OnMartReturnState -= OnMartReturnState;
            ParseNetworkManager.NetworkManager.OnShowItemAndSpeakState -= OnShowItemAndSpeakState;
            ParseNetworkManager.NetworkManager.OnSpeechEndState -= OnSpeechEndState;
            ParseNetworkManager.NetworkManager.OnPointAtSuspectState -= OnPointAtSuspectState;
            ParseNetworkManager.NetworkManager.OnPointAtSuspectEndState -= OnPointAtSuspectEndState;
            ParseNetworkManager.NetworkManager.OnDebateTimeState -= OnDebateTimeState;
            ParseNetworkManager.NetworkManager.OnDebateEndState -= OnDebateEndState;
            ParseNetworkManager.NetworkManager.OnVoteState -= OnVoteState;
            ParseNetworkManager.NetworkManager.OnVoteEndState -= OnVoteEndState;
            ParseNetworkManager.NetworkManager.OnLiarConfirmedState -= OnLiarConfirmedState;
            ParseNetworkManager.NetworkManager.OnLiarKeywordGuessState -= OnLiarKeywordGuessState;
            ParseNetworkManager.NetworkManager.OnLiarKeywordGuessEndState -= OnLiarKeywordGuessEndState;
            ParseNetworkManager.NetworkManager.OnScoreTallyState -= OnScoreTallyState;
            ParseNetworkManager.NetworkManager.OnScoreTallyEndState -= OnScoreTallyEndState;
            ParseNetworkManager.NetworkManager.OnFinalResultState -= OnFinalResultState;
            ParseNetworkManager.NetworkManager.OnFinalResultEndState -= OnFinalResultEndState;
            
            ParseNetworkManager.NetworkManager.OnLiarOutButtonPressedState -= OnLiarOutButtonPressedState;

            //마트 처리
            ParseNetworkManager.NetworkManager.OnItemHoldAnswer     -= OnItemHoldAnswer;
            ParseNetworkManager.NetworkManager.OnItemDropAnswer -= OnItemReturnInStandAnswer;
            ParseNetworkManager.NetworkManager.OnItemPutInBagAnswer -= OnItemPutInBagAnswer;
            ParseNetworkManager.NetworkManager.OnPushAnswer -= OnPushAnswer;
            
            //채팅 처리
            ParseNetworkManager.NetworkManager.OnNormalChat -= OnNormalChat;
            ParseNetworkManager.NetworkManager.OnSpecialChat -= OnSpecialChat;
            ParseNetworkManager.NetworkManager.OnKeywordGuessChat -= OnKeywordGuessChat;
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

                // 마트아니면 이동좌표 안보냄(테스트 용)
                if (activeContentScene != "02. NewMart") continue;
                
                Vector3 position = localPlayer.transform.position;
                await SendAsync(new MoveMessage { Id = Me.Id, X = position.x, Y = position.y, Z = position.z });
            }
        }

        // 지금이 어떤 채팅을 보낼 차례인지 알아서 판단해서 보낸다. 로비가 이거 하나만 부르면 된다.
        public void Say(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (Me == null) return;

           text = RichTextUtils.ToColoredRichText(text, colorSet.GetColor(Me.Id));

            // 발언 상태 + 내가 발언자 + 이번 차례에 아직 특수채팅을 안 보냈으면, 첫 마디는 특수채팅이 된다.
            if (CurrentState == TurnMessageType.ShowItemAndSpeakState && CurrentOwnerID == Me.Id &&
                hasSentSpecialChat == false)
            {
                hasSentSpecialChat = true;
                RequestSpecialChat(text);
                return;
            }

            // 라이어의 키워드 맞춤 상태 + 내 id가 라이어 id와 동일하면 + 아직 키워드채팅을 안 보냈으면, 첫 마디는 키워드채팅이 된다.
            if (CurrentState == TurnMessageType.LiarKeywordGuessState && LiarId == Me.Id && hasSentKeywordChat == false)
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



        private void OnDestroy() // 사라지면 모두 구독 해제
        {
            UnSubAllEvents();
            socket?.Dispose();
        }
    }
}