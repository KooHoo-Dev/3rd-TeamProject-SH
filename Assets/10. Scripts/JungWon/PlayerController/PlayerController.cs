using System.Collections.Generic;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TeamSH
{
    public class PlayerController : PlayerControllerBase
    {
        public CharacterController3D Controller3D { get; private set; }
        public ItemInteractionHandler ItemInteraction { get; private set; }

        public float pushCoolTime = 0.5f;
        public float pushTimer = 0.0f;
        
        // 각 상태를 제어하기 위한 멤버 변수들
        private Dictionary<int, PlayerAnimStateBase> StateDic { get; set; } = new();
        private PlayerAnimStateBase defaultState; // 예외처리를 위한 기본 상태
        private PlayerAnimStateBase currentState; // 상태객체를 담아서 제어하기 위한 빈 객체 변수 
        
        protected override void OnAwake()
        {
            Controller3D = GetComponent<CharacterController3D>();
            ItemInteraction = GetComponent<ItemInteractionHandler>();
            
            ItemInteraction.OnStartGetItem += GetBox;
            ItemInteraction.OnStartPutDownItem += PutDownBox;
        }

        private void Update()
        {
            if (canMove == false)
            {
                Controller3D.SetMoveInput(Vector3.zero);
                return;
            }

            TryGetItem();
            TryPutDownItem();
            TryPushPlayer();
            
            Vector3 moveVec = new Vector3();
            moveVec.x = SimpleInput.GetAxisHorizontalRaw();
            moveVec.z = SimpleInput.GetAxisVerticalRaw();
            moveVec = moveVec.normalized;
            Controller3D.SetMoveInput(moveVec);
            Animator.SetBool(IS_MOVE, moveVec.magnitude > 0);
            if (moveVec != Vector3.zero) Animator.gameObject.transform.forward = moveVec;
        }
        
        private void TryGetItem()
        {
            if (SimpleInput.GetKeyDown(Key.F))
            {
                ItemInteraction.RequestGetItem();
            }
        }
        
        private void TryPutDownItem()
        {
            if (SimpleInput.GetKeyDown(Key.F))
            {
                ItemInteraction.RequestPutDownItem();
            }
        }

        private void TryPushPlayer()
        {
            if (pushTimer > 0f)
            {
                pushTimer -= Time.deltaTime;
                return;
            }
            
            if (SimpleInput.GetKeyDown(Key.Space))
            {
                PlayPushAnimation();
                pushTimer += pushCoolTime;

                LiarClient.Instance.RequestPushAnimation();
            }
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
        
        public override void ApplyKnockback(Vector3 hitDirection, float power)
            => Controller3D.ApplyPush(hitDirection, power);
    }
}