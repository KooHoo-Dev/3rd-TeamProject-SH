using System;

namespace HelloServer
{
    [Serializable]
    public class User
    {
        public string Id;
        public string NickName;
    }

    [Serializable]
    public class PlayerState
    {
        public string Id;
        public float X;
        public float Y;
    }

    [Serializable]
    public class TypeOnly
    {
        public string Type;
    }

    #region 클라이언트 -> 서버

    [Serializable]
    public class HelloMessage
    {
        public string Type = "hello";
        public string NickName;
    }

    [Serializable]
    public class MoveMessage
    {
        public string Type = "move";
        public string Id;
        public float X;
        public float Y;
    }

    #endregion

    #region 서버 -> 클라이언트

    [Serializable]
    public class WelcomeMessage
    {
        public string Type;

        public string RoomCode;

        public User User;
        public User[] Users;
    }

    // 누가 새로 들어왔다.
    [Serializable]
    public class JoinMessage
    {
        public string Type;
        public User User;
    }

    // 누가 나갔다.
    [Serializable]
    public class LeaveMessage
    {
        public string Type;
        public string Id;
    }

    [Serializable]
    public class ChatMessage
    {
        public string Type = "chat";
        public string Id;
        public string NickName;
        public string Text;
    }

    [Serializable]
    public class StateMessage
    {
        public string Type;
        public PlayerState[] States;
    }

    #endregion
}