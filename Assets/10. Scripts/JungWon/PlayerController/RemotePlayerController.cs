using UnityEngine;

namespace TeamSH
{
    public class RemotePlayerController : PlayerControllerBase
    {
        [SerializeField] private float smooth = 10f;

        public ItemInteractionHandler ItemInteraction { get; private set; }

        [field: SerializeField] private GameObject body;
        
        private Vector3 target;
        private Vector3 lastPos;
        
        [Header("씬 전환 직후 옛 좌표 무시용 쿨다운")]
        public float delayTime = 1f;
        private float delayTimer = 0f;

        [Header("비정상 좌표 무시 설정")]
        [SerializeField] private float maxWalkableSqrDistance = 9f; // 대략 3미터 이상은 비정상으로 간주


        protected override void OnAwake()
        {
            target = transform.position;
            lastPos = transform.position;

            ItemInteraction = GetComponent<ItemInteractionHandler>();
            ItemInteraction.OnStartGetItem += GetBox;
            ItemInteraction.OnStartPutDownItem += PutDownBox;
        }

        public void SetTarget(Vector3 position, float smooth)
        {
            if (delayTimer > 0f) return;
            
            // 2차 방어선(이번에 새로 추가하는 핵심): 마지막 좌표 기준으로 비정상적으로 먼 좌표(텔포) 가 들어오면,
            // 스냅해서 이동시키는 게 아니라 아예 값을 갱신하지 않고 버린다.
            // → 캐릭터는 그 자리에서 한 발짝도 움직이지 않고, 다음에 정상적인(가까운) 좌표가 올 때까지 기다린다.
            if ((position - target).sqrMagnitude > maxWalkableSqrDistance)return;
            
            target = position;
            this.smooth = smooth;
        }

        public void Teleport(Vector3 position)
        {
            delayTimer = delayTime;
            
            transform.position = position;
            target = position;
            lastPos = position;
        }
        
        public override void ApplyKnockback(Vector3 hitDirection, float power)
        {
            // 뭐 연출넣을거면 여기넣자
        }
        
        private void Update()
        {
            if (delayTimer > 0f) delayTimer -= Time.deltaTime;
            if (canMove == false) return;

            float t = 1f - Mathf.Exp(-smooth * Time.deltaTime);
            Place(Vector3.Lerp(transform.position, target, t));

            Vector3 moveDirection = target - transform.position;
            moveDirection.y = 0f;

            bool isMoving = (target - transform.position).sqrMagnitude > 0.0001f;
            Animator.SetBool(IS_MOVE, isMoving);

            if (isMoving)
                body.transform.forward = moveDirection.normalized;

        }

        private void Place(Vector3 next)
        {
            transform.position = new Vector3(next.x, transform.position.y, next.z);
        }

        private void GetBox()
        {
            Animator.SetLayerWeight(1, 1);
            Animator.SetBool(GET_ITEM, true);
        }

        private void PutDownBox()
        {
            Animator.SetLayerWeight(0, 0);
            Animator.SetBool(GET_ITEM, false);
        }
    }
}