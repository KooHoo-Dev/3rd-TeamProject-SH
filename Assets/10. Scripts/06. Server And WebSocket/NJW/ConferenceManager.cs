using UnityEngine;

public class ConferenceManager : MonoBehaviour
{
    public static ConferenceManager Instance;

    public Transform[] userSpawnPoints;
    
    public void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}
