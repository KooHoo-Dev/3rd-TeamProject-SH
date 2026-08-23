using UnityEngine;

namespace JunWon.Scripts
{
    public class PushBox : MonoBehaviour
    {
        private Player player;

        private void OnTriggerEnter(Collider collision)
        {
            if (collision.TryGetComponent<PushableBox>(out PushableBox other) == false) return;
        }
    }
}