using UnityEngine;

namespace TeamSH
{
    public class MartManager : MonoBehaviour
    {
        public static MartManager Instance;

        public Transform[] userSpawnPoints;
    
        public void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
    }
}