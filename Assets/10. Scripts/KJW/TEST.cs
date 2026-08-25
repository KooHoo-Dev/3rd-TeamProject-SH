using System.Collections.Generic;

namespace TEST
{
    public class Rooms
    {
        Dictionary<int, Room> RoomDict = new Dictionary<int, Room>();
    }
    
    public class Room
    {
        public string Name;
        public string Code;
        
        public List<User> User = new List<User>();
        public int Id;
    }

    public class User
    {
        public int Id;
        public string Name;
    }

    public class UserState
    {
        public float X;
        public float Y;
        
    }

    public class MartData
    {
        public string ItemName;
    }

    public class ConferecneData
    {
    
    }

    public class Config
    {
    
    }
}