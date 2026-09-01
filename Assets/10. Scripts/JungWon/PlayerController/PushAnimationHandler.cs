using UnityEngine;

namespace TeamSH
{
    public class PushAnimationHandler : MonoBehaviour
    {
        public Interaction_Player Owner;

        private void Awake()
        {
            Owner = GetComponentInParent<Interaction_Player>();
            Owner.PushBox.gameObject.SetActive(false);
        }

        public void Push_Start()
        {
            Owner.PushBox.gameObject.SetActive(true);
        }

        public void Push_End()
        {
            Owner.PushBox.gameObject.SetActive(false);
        }
    }
}