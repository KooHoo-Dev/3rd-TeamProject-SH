using UnityEngine;

namespace TeamSH
{
    // 씬 이동해도 남게 플레이어 남게 하기
    public class LiarSceneBinder : MonoBehaviour
    {
        [SerializeField] private LocalPlayer localPlayer;
        [SerializeField] private RemotePlayer remotePrefab;

        private void Start()
        {
            LiarClient.Instance.DoBindScene(localPlayer, remotePrefab);
        }
    }
}
