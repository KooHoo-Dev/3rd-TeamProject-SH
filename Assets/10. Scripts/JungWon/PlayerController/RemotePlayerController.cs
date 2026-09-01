using UnityEngine;

namespace TeamSH
{
    public class RemotePlayerController : PlayerControllerBase
    {
        [SerializeField] private float smooth = 10f;
        private Vector3 target;
        private Vector3 lastPos;

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
            transform.position = position;
            target = position;
            lastPos = position;
        }

        private void Update()
        {
            if (canMove == false) return;
            // 프레임 독립적 지수 감쇠 보간. 물리 재계산 없음.
            float t = 1f - Mathf.Exp(-smooth * Time.deltaTime);
            Place(Vector3.Lerp(transform.position, target, t));

            bool isMoving = (transform.position - lastPos).sqrMagnitude > 0.0001f;
            Animator.SetBool(IS_MOVE, isMoving);
            lastPos = transform.position;
        }
        
        private void Place(Vector3 next)
        {
            transform.position = new Vector3(next.x, transform.position.y , next.z); //3D공간이니 y값만 불변
        }
        
        public override void ApplyKnockback(Vector3 hitDirection, float power) { }
    }
}