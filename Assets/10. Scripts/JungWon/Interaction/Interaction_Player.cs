using System.Collections.Generic;
using UnityEngine;

namespace TeamSH
{
    public abstract class Interaction_Player : MonoBehaviour
    {
        [field: SerializeField] public Transform NickNamePivot { get; private set; }
        [field: SerializeField] public Transform ItemHeadPivot { get; private set; }
        [field: SerializeField] public Transform FindItemPivot { get; private set; }
        [field: SerializeField] public int PushPower { get; private set; }

        [field: SerializeField] public List<MartItem> SelectedObjects { get; private set; }

        public abstract void PushPlayer(Vector3 hitDirection, float power);
    }
}