using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.Analytics;

namespace TeamSH
{
    
    // RoomClient가 서버와의 네트워크 작업을 담당한다.
    // 송신, 수신 등을 모두 처리하는 객체
    public class LiarClient : MonoBehaviour
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
        
        private ClientWebSocket socket;
        
        // 남들. 번호로 찾는다.
        private readonly Dictionary<string, RemotePlayer> remotes = new Dictionary<string, RemotePlayer>();

        // 오간 말. 너무 쌓이지 않게 앞에서부터 버린다.
        private readonly List<string> chatLog = new List<string>();

        private const int ChatKeep = 12;

        // 한 소켓에 동시에 두 번 보내면 안 된다.
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);

        #region 라이어 게임 필드
        
        public NewGameConfig NewGameConfig;
        
        public PlayerState PlayerState = new PlayerState();
        
        #endregion
        
        
        private void Awake()
        {
            // true로 해둬야 백그라운드에서도 데이터 수신 가능
            Application.runInBackground = true;
        }

        public void Join(string code, string who)
        {
            if (IsBusy) return;

            roomCode = code;
            nickName = who;

            RunAsync().Forget();
        }

        public bool IsBusy { get; private set; }

        // 서버가 다듬어 알려 준 방 코드. abcd 로 쳐도 ABCD 가 옴.
        public string RoomCode { get; private set; }

        // 방에서 오간 말. 로비가 그대로 그린다.
        public IReadOnlyList<string> ChatLog => chatLog;

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

        #region 듣기

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
                
                case "게임 시작" : OnGameStart(JsonConvert.DeserializeObject<GameStartMessage>(json)); break;
                case "게임 시작 확인" : OnGameStartOK(JsonConvert.DeserializeObject<GameStartOKMessage>(json)); break;
                
                // 채팅 관련 case
                case "normalChat": OnNormalChat(JsonConvert.DeserializeObject<NormalChatMessage>(json)); break;
                case "specialChat": OnSpecialChat(JsonConvert.DeserializeObject<SpecialChatMessage>(json)); break;
                case "KeywordChat": OnKeywordChat(JsonConvert.DeserializeObject<KeywordChatMessage>(json)); break;
                

                // 아래는 전부 같은 모양(ChangeGameStateMessage)의 진행 단계 알림이라 한 번에 처리한다.
                case "장르 배분 및 라이어 선정":
                case "키워드 뿌리기":
                case "마트 진입":
                case "마트에서 복귀":
                case "물건 보여주고 발언":
                case "발언 종료":
                case "지목":
                case "지목 종료":
                case "라밍아웃 버튼 누름":
                case "변론 시간":
                case "변론 종료":
                case "투표":
                case "라이어 확정":
                case "라이어의 키워드 맞춤":
                case "키워드 맞춤 종료":
                case "점수 집계":
                case "점수 집계 종료":
                case "마트 이동":
                case "최종 결과":
                case "최종 결과 종료":

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
        #region 클라이언트 -> 서버 메세지

        public void GameStartMessage() // 게임 시작 요청
        {
            if (Me == null) return;
            SendAsync(new GameStartMessage()).Forget();
        }
        
        
        
        #endregion
        
        
        
        #region 서버 -> 클라이언트 
        
        private void OnNormalChat(NormalChatMessage message)
        {
            string mine = Me != null && message.ID == Me.Id ? $"나" : $"{message.ID}";
            chatLog.Add($"[일반] {message.NickName}{message.ID}{mine} : {message.Text}");
            if (chatLog.Count > ChatKeep) chatLog.RemoveAt(0);
            Debug.Log($"[일반] {message.ID} : {message.Text}");
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
        
        

        private void OnWelcome(WelcomeMessage message)
        {
            Me = message.User;
            RoomCode = message.RoomCode;

            localPlayer.Setup(Me);

            foreach (User user in message.Users) Spawn(user);

            Debug.Log($"[방] {RoomCode} 에 들어왔다. 내 번호는 {Me.Id}, 이미 {message.Users.Length}명 있다");
        }

        private void OnJoin(JoinMessage message)
        {
            Spawn(message.User);
            Debug.Log($"[방] {message.User.NickName}({message.User.Id}) 이(가) 들어왔다");
        }

        private void OnLeave(LeaveMessage message)
        {
            if (remotes.TryGetValue(message.Id, out RemotePlayer remote) == false) return;

            remotes.Remove(message.Id);
            Destroy(remote.gameObject);

            Debug.Log($"[방] {message.Id} 이(가) 나갔다");
        }

        private void OnGameStart(GameStartMessage message)
        {
            Debug.Log("게임 시작 및 게임 정보를 요청한다");
        }

        private void OnGameStartOK(GameStartOKMessage message)
        {
            Debug.Log("게임 데이터 요청을 받고 message 안의 newGameConfig를 클라이언트 NewGameConfig로 저장");
            
            NewGameConfig = message.newGameConfig;
        }

        private void OnVote()
        {
            throw new NotImplementedException();
        }

        private void OnstateGenreAssignAndLiarSelect()
        {
            
        }

        private void OnState(StateMessage message)
        {
            // 캐릭터는 움직이지 않으므로 서버가 뿌리는 위치는 더 이상 반영하지 않는다.
        }

        // 방에서 누가 말했다. 내 말도 여기로 돌아옴
        private void OnChat(ChatMessage message)
        {
            string mine = Me != null && message.Id == Me.Id ? " (나)" : "";

            chatLog.Add($"{message.NickName}{mine} : {message.Text}");

            if (chatLog.Count > ChatKeep) chatLog.RemoveAt(0);

            Debug.Log($"[방] {message.NickName}({message.Id}) : {message.Text}");
        }

        private void Spawn(User user)
        {
            // 이미 있으면 만들지 않는다. welcome 과 join 이 겹칠 수 있다.
            if (remotes.ContainsKey(user.Id)) return;

            RemotePlayer remote = Instantiate(remotePrefab, Vector3.zero, Quaternion.identity);

            remote.Setup(user);

            remotes[user.Id] = remote;
        }

        #endregion

        #region 보내기

        private async UniTaskVoid SendLoop()
        {
            int delay = (int)(1000f / sendPerSecond);

            while (destroyCancellationToken.IsCancellationRequested == false)
            {
                await UniTask.Delay(delay, cancellationToken: destroyCancellationToken);

                // 아직 번호를 못 받았으면 보낼 것이 없다.
                if (Me == null) continue;

                Vector2 position = localPlayer.transform.position;

                await SendAsync(new MoveMessage { Id = Me.Id, X = position.x, Y = position.y });
            }
        }

        public void Say(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (Me == null) return;

            SendAsync(new ChatMessage { Id = Me.Id, NickName = Me.NickName, Text = text.Trim() }).Forget();
        }

        // 한 마디를 보낸다. 보내는 곳이 둘(SendLoop, Say)이라 lock을 걸어서 한 번에 하나씩 내보낸다.
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

        #endregion

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
