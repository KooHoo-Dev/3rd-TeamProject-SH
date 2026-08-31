using JunWon.Scripts;
using UnityEngine;

namespace TeamSH
{
    public class Player : Interaction_Player
    {
        private PlayerController playerController;
        private ItemInteractionHandler itemInteractionHandler;
        
        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            itemInteractionHandler = GetComponent<ItemInteractionHandler>();
        }
        
        
        public override void PushPlayer(Vector3 hitDirection, float power)
        {
            playerController.PushPlayer(hitDirection, power);
        }
    }
}