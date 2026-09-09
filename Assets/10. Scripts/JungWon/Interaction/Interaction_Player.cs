using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TeamSH
{
    public abstract class Interaction_Player : MonoBehaviour
    {
        [SerializeField] protected PlayerControllerBase controller;

        [field: SerializeField] public Transform NickNamePivot { get; private set; }
        [field: SerializeField] public Transform ItemHeadPivot { get; private set; }
        [field: SerializeField] public Transform FindItemPivot { get; private set; }
        
        [field: SerializeField] public GameObject Body { get; private set; }
        [field: SerializeField] public Collider PushBox { get; private set; }
        [field: SerializeField] public int PushPower { get; private set; }

        [field: SerializeField] public List<MartItem> SelectedItems { get; private set; }
        
        public void ApplyKnockback(Vector3 hitDirection, float power) => controller.ApplyKnockback(hitDirection, power);

        public void SetPlayerWithScenes(string playerState)
        {
            switch (playerState)
            {
                case "GameReady": controller.ApplyReadyPlayer(); break;
                case "applyMeeting": controller.ApplyMeetingPlayer(); break;
                case "applyMart": controller.ApplyMartPlayer(); break;
                case "applyVictory": controller.ApplyVictoryMotion(); break;
                case "applyDefeat" : controller.ApplyDefeatMotion(); break;
                default: controller.ApplyReadyPlayer(); break;
            }
        }

        public void ClearSelectedItems() => SelectedItems.Clear();
    }
}