using UnityEngine;

namespace TeamSH
{
    public class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance;
        public Transform[] userSpawnPoints;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
    }
}