using System.Collections.Generic;
using UnityEngine;

namespace TeamSH
{
    public class PushBox : MonoBehaviour
    {
        private Interaction_Player Owner { get; set; }

        private HashSet<PushableBox> checkList = new HashSet<PushableBox>();

        private void Awake()
        {
            Owner = GetComponentInParent<Interaction_Player>();

            if (Owner == null)
            {
                Debug.LogError($"{name} : 부모 계층에서 Interaction_Player를 찾지 못했습니다. 오브젝트를 삭제합니다.");
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            checkList.Clear();
        }

        public void ResetPushableBox()
        {
            checkList.Clear();
        }

        private void OnTriggerEnter(Collider collision)
        {
            // 3D Collider 등록 여부 확인
            PushableBox pushableBox = Interaction_System.Instance.GetPushableBoxOrNull(collision);
            if (pushableBox == null) return;

            // 자기 자신 밀치기 방지
            if (pushableBox.Owner == Owner) return;

            // 이미 처리된 녀석이면 중복 처리 방지
            if (checkList.Contains(pushableBox)) return;
            checkList.Add(pushableBox);

            Interaction_Player sender = Owner;
            Interaction_Player receiver = pushableBox.Owner;

            if (receiver == null) return;
            if (sender != LiarClient.Instance.Local) return; //미는게 나일때만 보고

            string receiverId = LiarClient.Instance.GetPlayerId(receiver); //미는사람 Id 받아오기
            if(receiverId == null) return;
            
            LiarClient.Instance.RequestPushQuery(receiverId);
        }
    }
}