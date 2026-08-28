using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TeamSH
{
    // 방 코드를 받아 들어가고, 들어간 뒤에는 채팅을 그린다. RoomClient 와 같은 오브젝트에 붙인다.
    [RequireComponent(typeof(LiarClient))]
    public class LiarLobby : MonoBehaviour
    {
        [Header("처음 값")]
        [SerializeField] private string nickName = "이름없음";
        [SerializeField] private string roomCode = "ABCD";

        private LiarClient client;

        private string typing = "";

        private bool chatting;

        // Tab 키로 장르/키워드/특수채팅 정보창을 열고 닫는다.
        private bool showInfo;

        private void Awake()
        {
            client = GetComponent<LiarClient>();
        }

        private void OnGUI()
        {
            if (client.Me == null) DrawLobby();
            else DrawRoom();
        }

        private void DrawLobby()
        {
            GUI.Box(new Rect(20, 20, 320, 150), "방에 들어가기");

            GUI.Label(new Rect(35, 50, 70, 22), "이름");
            nickName = GUI.TextField(new Rect(105, 50, 220, 22), nickName, 12);

            GUI.Label(new Rect(35, 80, 70, 22), "방 코드");
            roomCode = GUI.TextField(new Rect(105, 80, 220, 22), roomCode, 8);

            // 붙는 중에는 다시 누르지 못하게 막는다.
            GUI.enabled = client.IsBusy == false;

            if (GUI.Button(new Rect(35, 112, 290, 30), client.IsBusy ? "들어가는 중…" : "들어가기"))
            {
                client.RequestJoin(roomCode, nickName);
            }
            
            GUI.enabled = true;

            GUI.Label(new Rect(20, 178, 700, 22),
                "같은 방 코드를 친 사람끼리만 서로 보인다. 대소문자는 서버가 맞춰 준다.");
        }

        // 들어갔다. 방 코드와 사람 수, 그리고 채팅을 그린다.
        private void DrawRoom()
        {
            GUI.Label(new Rect(20, 20, 700, 22),
                $"방 {client.RoomCode}    나 : {client.Me.NickName}({client.Me.Id})    남 : {client.RemoteCount}명");

            // 호스트에게만, 그리고 게임 시작 확인을 받기 전까지만 버튼을 보여준다.
            if (client.IsHost && client.IsGameStarted == false && GUI.Button(new Rect(20, 0, 150, 20), "게임 시작"))
            {
                client.RequestGameStart();
            }
            DrawChatLog();
            DrawChatInput();
            DrawLiarOutButton();
            DrawInfoOverlay();
            DrawTimer();
        }

        private void DrawChatLog()
        {
            GUI.Box(new Rect(20, 46, 380, 220), "");

            int line = 0;

            foreach (string said in client.ChatLog)
            {
                GUI.Label(new Rect(30, 52 + line * 18, 360, 18), said);
                line++;
            }
        }

        private void DrawChatInput()
        {
            // 이 칸에 커서가 있는지 이름으로 알아본다.
            GUI.SetNextControlName("chat");

            typing = GUI.TextField(new Rect(20, 272, 300, 24), typing, 100);

            chatting = GUI.GetNameOfFocusedControl() == "chat";

            bool enter = Event.current.type == EventType.KeyDown
                         && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);

            if (GUI.Button(new Rect(326, 272, 74, 24), "보내기") || (chatting && enter))
            {
                client.RequestChat(typing);
                typing = "";
            }
        }

        // 물건 보여주고 발언 상태일 때만 보인다. 실제 라이어인지는 서버가 검증한다.
        private void DrawLiarOutButton()
        {
            if (client.CurrentPhase != "물건 보여주고 발언 상태") return;

            if (GUI.Button(new Rect(20, 302, 150, 24), "라밍아웃"))
            {
                client.RequestLiarOutButtonPressedState();
            }
        }

        // 화면 우측 상단에 지금 상태의 남은 시간을 보여준다. 서버로는 아무것도 안 보낸다 - 순전히 시각용이다.
        private void DrawTimer()
        {
            if (client.CurrentPhase == "") return;

            Rect box = new Rect(Screen.width - 180, 20, 160, 44);
            GUI.Box(box, "");

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(box, $"{client.RemainingSeconds:0.0} 초", style);
        }

        // Tab 을 누르면 화면 정중앙에 장르 / 내 키워드 / 지금까지의 특수채팅을 띄운다. 다시 누르면 닫힌다.
        private void DrawInfoOverlay()
        {
            if (SimpleInput.GetKeyDown(Key.Q)) showInfo = !showInfo;

            if (showInfo == false) return;

            Rect box = new Rect(Screen.width / 2f - 220, Screen.height / 2f - 160, 440, 320);
            GUI.Box(box, "정보 (Tab 으로 닫기)");

            GUI.Label(new Rect(box.x + 16, box.y + 26, 400, 22), $"장르 : {client.GenreId}");
            GUI.Label(new Rect(box.x + 16, box.y + 48, 400, 22), $"내 키워드 : {client.KeywordId}");

            GUI.Label(new Rect(box.x + 16, box.y + 76, 400, 20), "특수 채팅 기록");

            int line = 0;

            foreach (string said in client.SpecialChatLog)
            {
                GUI.Label(new Rect(box.x + 16, box.y + 98 + line * 18, 408, 18), said);
                line++;
            }
        }


        public void DrawSpecialChatLog()
        {
            if (client.CurrentPhase != "발언 종료 상태") return;
            {
                Rect box = new Rect(Screen.width / 2f - 220, Screen.height / 2f - 160, 440, 320);
                GUI.Box(box, "특수 채팅 전체");

                int line = 0;

                foreach (string said in client.SpecialChatLog)
                {
                    GUI.Label(new Rect(box.x + 16, box.y + 30 + line * 18, 408, 18), said);
                    line++;
                }
            }
        }
    }
}