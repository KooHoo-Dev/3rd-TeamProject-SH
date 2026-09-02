using System;
using Jay;
using UnityEngine;

namespace TeamSH
{
    // Interaction_System.cs / InteractionBinder.cs(JungWon)가 구독하는 이벤트 허브.
    public class NetworkManager : SingletonBase<NetworkManager>
    {
        public Action<Interaction_Player, Interaction_Player, InteractionEvent> InteractionEvent { get; set; }
        public Action<string, string, float, Vector2, Vector2> PushEvent { get; set; }
    }
}
