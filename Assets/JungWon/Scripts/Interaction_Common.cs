using UnityEngine;

namespace JunWon.Scripts
{
    public class Interaction_Common : MonoBehaviour
    {
        public struct InteractionEvent
        {
            public int Amount;
            public Vector3 Position;
        }
        
        public interface IInteractionObserver
        {
            void PushOther(Interaction_Player sender, Interaction_Player receiver, InteractionEvent @event);
        }
    }
}