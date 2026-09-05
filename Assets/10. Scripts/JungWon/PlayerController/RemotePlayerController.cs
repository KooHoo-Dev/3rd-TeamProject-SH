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
        //고무줄 현상 어떻게 해결해야 하는지 모르겠다 딜레이시간 직접넣자
        public float delayTime = 1f;
        private float delayTimer = 0f;
        
        [Header("넉백 설정")]
        [SerializeField] private float knockbackDrag = 12f; //CharacterController3D랑 동일하게 할것
        private Vector3 knockbackVelocity = Vector3.zero;
        public bool HasKnockback => knockbackVelocity.sqrMagnitude > 0.0001f;

        [Header("충돌 설정 (CharacterController3D의 값과 반드시 동일하게 맞출 것)")]
        private float skinWidth = 0.02f;
        public LayerMask collisionLayer;

        private Collider col;
        private Rigidbody rBody;
        
        protected override void OnAwake()
        {
            target = transform.position;
            lastPos = transform.position;
            col = GetComponent<Collider>();
            rBody = GetComponent<Rigidbody>();
            rBody.isKinematic = true;

            ItemInteraction = GetComponent<ItemInteractionHandler>();
            ItemInteraction.OnStartGetItem += GetBox;
            ItemInteraction.OnStartPutDownItem += PutDownBox;
        }

        public void SetTarget(Vector3 position, float smooth)
        {
            if (delayTimer > 0f) return;
            if (HasKnockback) return;
            
            target = position;
            this.smooth = smooth;
        }

        public void Teleport(Vector3 position)
        {
            delayTimer = delayTime;
            knockbackVelocity = Vector3.zero; 
            
            transform.position = position;
            target = position;
            lastPos = position;
        }
        
        // 서버가 승인한 밀치기 결과를 실제로 반영하는 부분
        public override void ApplyKnockback(Vector3 hitDirection, float power)
        {
            knockbackVelocity += hitDirection * power;
        }

        private void Update()
        {
            if (delayTimer > 0f) delayTimer -= Time.deltaTime;

            if (canMove == false) return;
            
            if (knockbackVelocity.sqrMagnitude > 0.0001f)
            {
                Vector3 desireMove = knockbackVelocity * Time.deltaTime;
                Vector3 moveVector;
                
                Bounds box = GetCastBox();

                float boxX = ResolveAxisMovement(box, desireMove.x, Vector3.right);
                box.center += new Vector3(boxX, 0, 0); // X를 먼저 반영해야 벽 모서리 미끄러짐이 로컬과 같아진다.
                float boxZ = ResolveAxisMovement(box, desireMove.z, Vector3.forward);

                moveVector = new Vector3(boxX, 0, boxZ);

                if (moveVector.x != desireMove.x) knockbackVelocity.x = 0.0f;
                if (moveVector.z != desireMove.z) knockbackVelocity.z = 0.0f;

                Place(transform.position + moveVector);
                target = transform.position; // 서버의 옛 좌표로 되돌아가지 않도록 목표도 같이 갱신
                
                knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDrag * Time.deltaTime);
                if(knockbackVelocity.sqrMagnitude < 0.0001f) knockbackVelocity = Vector3.zero;
            }
            else
            {
                float t = 1f - Mathf.Exp(-smooth * Time.deltaTime);
                Place(Vector3.Lerp(transform.position, target, t));
            }
            
            Vector3 moveDirection = transform.position - lastPos;
            moveDirection.y = 0f; // 위아래로 기울어지는 것 방지

            bool isMoving = moveDirection.sqrMagnitude > 0.0001f;
            Animator.SetBool(IS_MOVE, isMoving);

            if (isMoving)
                body.transform.forward = moveDirection.normalized;

            lastPos = transform.position;
        }

        private void Place(Vector3 next)
        {
            transform.position = new Vector3(next.x, transform.position.y, next.z); //3D공간이니 y값만 불변
        }
        
        #region 충돌 계산 (CharacterController3D와 동일)

        private float ResolveAxisMovement(Bounds box, float move, Vector3 axis)
        {
            if (move == 0.0f) return 0.0f;

            Vector3 dir = move > 0.0f ? axis : -axis;
            float distance = Mathf.Abs(move);

            RaycastHit hit;
            bool isHit = Physics.BoxCast(
                box.center,
                box.extents,
                dir,
                out hit,
                Quaternion.identity,
                distance,
                collisionLayer
            );

            if (!isHit) return move;

            float allowed = Mathf.Max(0, hit.distance - skinWidth);
            return Mathf.Sign(move) * allowed;
        }

        private Bounds GetCastBox()
        {
            Bounds box = col.bounds;

            Vector3 size = box.size;
            size.x -= skinWidth * 2;
            size.z -= skinWidth * 2;
            box.size = size;

            return box;
        }

        #endregion
        
        private void GetBox()
        {
            Animator.SetLayerWeight(1,1);
            Animator.SetBool(GET_ITEM, true);
        }

        private void PutDownBox()
        {
            Animator.SetLayerWeight(0,0);
            Animator.SetBool(GET_ITEM, false);
            Animator.SetTrigger(PUT_DOWN);
        }
    }
}