using UnityEngine;

namespace TeamSH
{
    public class RemotePlayerController : PlayerControllerBase
    {
        [SerializeField] private float smooth = 10f;
        [field: SerializeField] private GameObject body;
        // public CharacterController3D Controller3D; 지금 밀치기가 적용되려면 Controller3D가 있는 캐릭터끼리만 적용되기때문에
        // 다른 방법을 찾아볼 것
        private Vector3 target;
        private Vector3 lastPos;
        public float delayTime = 0f;

        protected override void OnAwake()
        {
            target = transform.position;
            lastPos = transform.position;
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

        private void Update()
        {
            if(delayTime > 0f)
            {
                delayTime -= Time.deltaTime;
                return;
            }
            if (canMove == false) return;
            // 프레임 독립적 지수 감쇠 보간. 물리 재계산 없음.
            float t = 1f - Mathf.Exp(-smooth * Time.deltaTime);
            Place(Vector3.Lerp(transform.position, target, t));
            
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
            transform.position = new Vector3(next.x, transform.position.y , next.z); //3D공간이니 y값만 불변
        }
        
        public override void ApplyKnockback(Vector3 hitDirection, float power) { }
    }
}