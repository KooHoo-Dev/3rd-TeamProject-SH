using System.Collections.Generic;
using Study.Utilities;
using UnityEngine;

namespace TeamSH
{
    public class PointerHighlightController : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private LayerMask raycastMask = ~0;

        [Header("외곽선")]
        [SerializeField] private Color fallbackHighlightColor = new Color(1f, 0.85f, 0.3f, 1f);

        // Material이 아니라 RemotePlayer 기준으로 추적한다.
        // (아웃라인 머티리얼 인스턴스가 ON/OFF마다 새로 생성/파괴되기 때문)
        private RemotePlayer hoveredRemote;
        private bool active;

        private void OnEnable()
        {
            ParseNetworkManager.NetworkManager.OnPointAtSuspectState += HandlePointStart;
            ParseNetworkManager.NetworkManager.OnPointAtSuspectEndState += HandlePointEnd;
        }

        private void OnDisable()
        {
            ParseNetworkManager.NetworkManager.OnPointAtSuspectState -= HandlePointStart;
            ParseNetworkManager.NetworkManager.OnPointAtSuspectEndState -= HandlePointEnd;
            ClearAllHighlights();
        }

        private void HandlePointStart(float timerMs, int cycle, int round) => active = true;

        private void HandlePointEnd(float timerMs, int cycle, int round, string mostSelectedId)
        {
            active = false;
            ClearAllHighlights();
        }

        private void Update()
        {
            if (active == false) return;

            Camera cam = raycastCamera != null ? raycastCamera : Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(SimpleInput.GetMousePosition());

            RemotePlayer next = null;
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, raycastMask, QueryTriggerInteraction.Ignore))
                next = hit.collider.GetComponentInParent<RemotePlayer>();

            SetHovered(next);
        }

        private void SetHovered(RemotePlayer next)
        {
            if (next == hoveredRemote) return;

            if (hoveredRemote != null) ApplyOutline(hoveredRemote, false);

            hoveredRemote = next;

            if (hoveredRemote != null) ApplyOutline(hoveredRemote, true);
        }

        private void ApplyOutline(RemotePlayer remote, bool on)
        {
            if (remote == null || remote.skin == null) return;

            PlayerColorSet colorSet = LiarClient.Instance?.colorSet;
            if (colorSet == null) return;

            Color rimColor = remote.User != null
                ? colorSet.GetOutlineColor(remote.User.Id)
                : fallbackHighlightColor;

            colorSet.SetOutline(remote.skin, on, rimColor);
        }

        private void ClearAllHighlights()
        {
            SetHovered(null);

            if (LiarClient.Instance == null) return;
            foreach (KeyValuePair<string, RemotePlayer> kv in LiarClient.Instance.remotes)
                ApplyOutline(kv.Value, false);
        }
    }
}