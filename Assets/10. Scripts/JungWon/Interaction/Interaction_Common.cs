using UnityEngine;

namespace JunWon.Scripts
{
    public struct InteractionEvent
    {
        public Vector3 Position;
        public Vector3 HitDirection;
        public float PushPower;
    }
        
    public interface IInteractionObserver
    {
        void PushOther(Interaction_Player sender, Interaction_Player receiver, InteractionEvent @event);
    }
}