using UnityEngine;

namespace JunWon.Scripts
{
    public class Player : Interaction_Player
    {
        protected PlayerController playerController;
        
        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
        }
        
        
        public override void PushPlayer(Vector3 hitDirection, float power)
        {
            playerController.PushPlayer(hitDirection, power);
        }
    }
}