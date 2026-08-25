using System.Collections.Generic;
using UnityEngine;

namespace JunWon.Scripts
{
    public abstract class Interaction_Player : MonoBehaviour
    {
        [field: SerializeField] public Transform NickNamePivot { get; private set; }
        [field: SerializeField] public Transform ItemHeadPivot { get; private set; }
        [field: SerializeField] public Transform FindItemPivot { get; private set; }
        [field: SerializeField] public int PushPower { get; private set; }

        [field: SerializeField] public List<GameObject> SelectedObjects { get; private set; } //추후 GameObject에서 아이템 스크립트로 변경할 것

        public abstract void PushPlayer(Vector3 hitDirection, float power);
    }
}