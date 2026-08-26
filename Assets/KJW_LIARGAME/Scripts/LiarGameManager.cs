using System;
using Jay;
using UnityEngine;

namespace KJW_LIARGAME.Scripts
{
    public class LiarGameManager : SingletonBase<LiarGameManager>
    {
        public NewGameConfig NewGameConfig;
        
        public PlayerState  PlayerState1;
        public PlayerState PlayerState2;
        public PlayerState PlayerState3;
        public PlayerState playerState4;
        
        private void Start()
        {
                
        }
    }
}