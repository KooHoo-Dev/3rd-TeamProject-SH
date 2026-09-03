using UnityEngine;

namespace TeamSH
{
    public class RemotePlayerController : PlayerControllerBase
    {
        [SerializeField] private float smooth = 10f;

        public ItemInteractionHandler ItemInteraction { get; private set; }

        [field: SerializeField] private GameObject body;

        // public CharacterController3D Controller3D; 지금 밀치기가 적용되려면 Controller3D가 있는 캐릭터끼리만 적용되기때문에
        // 다른 방법을 찾아볼 것
        private Vector3 target;
        private Vector3 lastPos;
        public float delayTime = 0f;
        
        [Header("넉백 설정")]
        [SerializeField] private float knockbackDrag = 8f;
        private Vector3 knockbackVelocity = Vector3.zero;

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
            target = position;
            this.smooth = smooth;
        }

        public void Teleport(Vector3 position)
        {
            delayTime = 1f; //씬이동까지의 딜레이시간
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
            if (delayTime > 0f)
            {
                delayTime -= Time.deltaTime;
                return;
            }

            if (canMove == false) return;
            
            if (knockbackVelocity.sqrMagnitude > 0.0001f)
            {
                Place(transform.position + knockbackVelocity * Time.deltaTime);
                target = transform.position;
                knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDrag * Time.deltaTime);
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
            {
                body.transform.forward = moveDirection.normalized;
            }

            lastPos = transform.position;
        }

        private void Place(Vector3 next)
        {
            transform.position = new Vector3(next.x, transform.position.y, next.z); //3D공간이니 y값만 불변
        }
        
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