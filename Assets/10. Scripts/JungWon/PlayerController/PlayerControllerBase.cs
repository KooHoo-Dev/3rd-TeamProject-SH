using UnityEngine;

namespace TeamSH
{
    public abstract class PlayerControllerBase : MonoBehaviour
    {
        protected static readonly int IS_MOVE = Animator.StringToHash("IsMove");
        protected static readonly int GET_ITEM = Animator.StringToHash("GetItem");
        protected static readonly int PUT_DOWN = Animator.StringToHash("PutDown");
        protected static readonly int IS_PUSH = Animator.StringToHash("IsPush");
        protected static readonly int IsSitting = Animator.StringToHash("IsSitting");
        
        public Animator Animator { get; private set; }
        [SerializeField] protected bool canMove = true;
        
        private void Awake()
        {
            Animator = GetComponentInChildren<Animator>();
            OnAwake();
        }
        
        protected abstract void OnAwake();
        
        public void ApplyMartPlayer()
        {
            canMove = true;
            Animator.SetBool(IsSitting, false);
        }
        
        public void ApplyMeetingPlayer()
        {
            canMove = false;
            Animator.SetLayerWeight(0, 0);
            Animator.SetBool(IsSitting, true);
        }
        
        public abstract void ApplyKnockback(Vector3 hitDirection, float power);

    }
}