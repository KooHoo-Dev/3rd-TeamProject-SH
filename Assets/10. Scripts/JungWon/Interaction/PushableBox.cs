using UnityEngine;

namespace JunWon.Scripts
{
    [RequireComponent(typeof(Collider))]
    public class PushableBox : MonoBehaviour
    {
        [field: SerializeField] public Interaction_Player Owner { get; private set; }
        private Collider col3D;

        private void Awake()
        {
            Owner = GetComponent<Interaction_Player>();
            if (Owner == null) Owner = GetComponentInParent<Interaction_Player>();

            if (Owner == null)
            {
                Debug.LogError($"{name} : Interaction_Player를 찾지 못했습니다.");
            }

            col3D = GetComponent<Collider>();
        }

        private void Start()
        {
            if (Interaction_System.Instance != null)
            {
                Interaction_System.Instance.AddPushableBox(col3D, this);
            }
        }

        private void OnDestroy()
        {
            if (Interaction_System.Instance != null)
            {
                Interaction_System.Instance.RemovePushableBox(col3D);
            }
        }

        public void SetInteractionActive(bool active)
        {
            if (Interaction_System.Instance == null) return;

            if (active)
                Interaction_System.Instance.AddPushableBox(col3D, this);
            else
                Interaction_System.Instance.RemovePushableBox(col3D);
        }
    }
}