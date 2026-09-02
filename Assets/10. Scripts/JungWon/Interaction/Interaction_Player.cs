using System.Collections.Generic;
using UnityEngine;

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

        [field: SerializeField] public List<MartItem> SelectedObjects { get; private set; }

        public void ApplyKnockback(Vector3 hitDirection, float power) => controller.ApplyKnockback(hitDirection, power);
        
        public void SetPlayerWithScenes(string sceneName)
        {
            switch (sceneName)
            {
                case "00. MeetingRoom": controller.ApplyMeetingPlayer(); break;
                case "02. NewMart": controller.ApplyMartPlayer(); break;
            }
        }
    }
}