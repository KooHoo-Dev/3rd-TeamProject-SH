using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JunWon.Scripts
{
    public class PlayerController : MonoBehaviour
    {
        private static readonly int IS_MOVE = Animator.StringToHash("IsMove");
        private static readonly int GET_ITEM = Animator.StringToHash("GetItem");
        private static readonly int PUT_DOWN = Animator.StringToHash("PutDown");
        private static readonly int IS_PUSH = Animator.StringToHash("IsPush");

        public Animator Animator { get; private set; }
        private Player player;
        private CharacterController3D Controller3D { get; set; }
        
        //private PushPlayer pushPlayer { get; set; }

        private void Awake()
        {
            Animator = GetComponentInChildren<Animator>();
            Controller3D = GetComponent<CharacterController3D>();
        }

        private void Update()
        {
            GetItem();
            PutDownItem();
            
            Vector3 moveVec = new Vector3();
            moveVec.x = SimpleInput.GetAxisHorizontalRaw();
            moveVec.z = SimpleInput.GetAxisVerticalRaw();
            moveVec = moveVec.normalized;
            Controller3D.SetMoveInput(moveVec);
            Animator.SetBool(IS_MOVE, moveVec.magnitude > 0);
            if (moveVec != Vector3.zero) Animator.gameObject.transform.forward = moveVec;
        }
        
        private void GetItem()
        {
            if (SimpleInput.GetKeyDown(Key.Digit1))
            {
                Animator.SetLayerWeight(1,1);
                Animator.SetBool(GET_ITEM, true);
            }
        }
        
        private void PutDownItem()
        {
            if (SimpleInput.GetKeyDown(Key.Digit2))
            {
                Animator.SetLayerWeight(0,0);
                Animator.SetBool(GET_ITEM, false);
                Animator.SetTrigger(PUT_DOWN);
            }
        }

        public void PushPlayer(Vector3 hitDirection, float power)
        {
            Controller3D.ApplyPush(hitDirection, power);
        }
    }
}