using UnityEngine;

namespace TeamSH
{
    public abstract class PlayerControllerBase : MonoBehaviour
    {
        protected static readonly int IS_MOVE = Animator.StringToHash("IsMove");
        protected static readonly int GET_ITEM = Animator.StringToHash("GetItem");
        protected static readonly int IS_PUSH = Animator.StringToHash("IsPush");
        protected static readonly int IsSitting = Animator.StringToHash("IsSitting");
        protected static readonly int Victory = Animator.StringToHash("Victory");
        protected static readonly int Defeat = Animator.StringToHash("Defeat");
        
        public Animator Animator { get; private set; }
        [SerializeField] protected bool canMove = true;
        
        private void Awake()
        {
            Animator = GetComponentInChildren<Animator>();
            OnAwake();
        }
        
        protected abstract void OnAwake();
        
        public void PlayPushAnimation()
        {
            if (canMove == false) return; // 이동 불가 상태(회의실 등)에서는 애니메이션도 재생하지 않음
            Animator.SetLayerWeight(1, 1);
            Animator.SetTrigger(IS_PUSH);
            SoundManager.Instance.PlaySfx(SoundManager.GameSfx.PlayerPush);
        }

        public void ApplyReadyPlayer()
        {
            canMove = false;
            Animator.SetLayerWeight(0, 0);
            Animator.SetBool(IS_MOVE, false);
            Animator.SetBool(GET_ITEM, false);
            Animator.SetBool(IsSitting, false);
        }
        
        public void ApplyMeetingPlayer()
        {
            canMove = false;
            Animator.SetLayerWeight(0, 0);
            Animator.SetBool(IS_MOVE, false);
            Animator.SetBool(GET_ITEM, false);
            Animator.SetBool(IsSitting, true);
        }
        
        public void ApplyMartPlayer()
        {
            canMove = true;
            Animator.SetBool(IsSitting, false);
        }

        public void ApplyVictoryMotion()
        {
            Animator.SetLayerWeight(1, 1);
            Animator.SetTrigger(Victory);
        }

        public void ApplyDefeatMotion()
        {
            Animator.SetLayerWeight(1, 1);
            Animator.SetTrigger(Defeat);
        }
        
        public abstract void ApplyKnockback(Vector3 hitDirection, float power);
    }
}