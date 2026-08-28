using System;
using System.Net.WebSockets;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace TeamSH
{
    // 테스트용 가짜 플레이어. hello만 보내고 그 이후로는 받은 걸 버리기만 하며 자리를 채운다.
    // 서버가 인원이 적으면 죽는 버그(GetRandomCategories/ShowItemAndSpeakState)를 피해서
    // 혼자서도 인원수를 채워 테스트하려고 만들었다. 지목/투표 등에는 응답하지 않으니
    // 게임 시작 ~ 마트 ~ 회의실 진입 정도까지만 테스트에 쓸 수 있다.
    public class LiarTestBot : MonoBehaviour
    {
        [Header("서버")]
        [SerializeField] private string url = "ws://34.50.63.204:5003/room";
        [SerializeField] private string roomCode = "ABCD";

        [Header("봇")]
        [SerializeField] private int botCount = 2;
        [SerializeField] private bool spawnOnStart = false;

        private void Start()
        {
            if (spawnOnStart) DoSpawnBots();
        }

        private void OnGUI()
        {
            if (GUI.Button(new Rect(20, 340, 220, 24), $"테스트 봇 {botCount}명 접속"))
            {
                DoSpawnBots();
            }
        }

        public void DoSpawnBots()
        {
            for (int i = 1; i <= botCount; i++)
            {
                DoRunBotAsync($"테스트봇{i}").Forget();
            }
        }

        // 봇 하나. 접속해서 hello만 보내고, 끊길 때까지 받은 건 그냥 버린다.
        private async UniTaskVoid DoRunBotAsync(string nickName)
        {
            using ClientWebSocket socket = new ClientWebSocket();

            string full = $"{url}?code={UnityWebRequest.EscapeURL(roomCode)}";

            try
            {
                await socket.ConnectAsync(new Uri(full), destroyCancellationToken).AsUniTask();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[봇] {nickName} 접속 실패 : {e.Message}");
                return;
            }

            byte[] hello = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new HelloMessage { NickName = nickName }));
            await socket.SendAsync(new ArraySegment<byte>(hello), WebSocketMessageType.Text, true, destroyCancellationToken).AsUniTask();

            Debug.Log($"[봇] {nickName} 접속함");

            byte[] buffer = new byte[4096];
            StringBuilder builder = new StringBuilder();

            while (socket.State == WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult result = await socket
                        .ReceiveAsync(new ArraySegment<byte>(buffer), destroyCancellationToken)
                        .AsUniTask();

                    if (result.MessageType == WebSocketMessageType.Close) break;

                    builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    if (result.EndOfMessage == false) continue;

                    string json = builder.ToString();
                    builder.Clear();

                    string type = JsonConvert.DeserializeObject<TypeOnly>(json).Type;

                    switch (type)
                    {
                        case "welcome":
                        {
                            WelcomeMessage message = JsonConvert.DeserializeObject<WelcomeMessage>(json);
                            Debug.Log($"[봇] {message.RoomCode} 에 들어왔다. 내 번호는 {message.User.Id}, 이미 {message.Users.Length}명 있다");
                            break;
                        }
                        case "join":
                        {
                            JoinMessage message = JsonConvert.DeserializeObject<JoinMessage>(json);
                            Debug.Log($"[봇] {message.User.NickName}({message.User.Id}) 이(가) 들어왔다");
                            break;
                        }
                        case "leave":
                        {
                            LeaveMessage message = JsonConvert.DeserializeObject<LeaveMessage>(json);
                            Debug.Log($"[봇] {message.Id} 이(가) 나갔다");
                            break;
                        }
                        // 그 외 종류는 처리하지 않고 버린다. 봇은 자리만 채우면 된다.
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (WebSocketException)
                {
                    break;
                }
            }

            Debug.Log($"[봇] {nickName} 끊김");
        }
    }
}
