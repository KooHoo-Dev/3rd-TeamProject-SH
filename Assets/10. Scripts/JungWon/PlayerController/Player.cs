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
    }
}