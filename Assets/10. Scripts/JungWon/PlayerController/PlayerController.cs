using System.Collections.Generic;
using JunWon.Scripts;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TeamSH
{
    public class PlayerController : MonoBehaviour
    {
        private static readonly int IS_MOVE = Animator.StringToHash("IsMove");
        private static readonly int GET_ITEM = Animator.StringToHash("GetItem");
        private static readonly int PUT_DOWN = Animator.StringToHash("PutDown");
        private static readonly int IS_PUSH = Animator.StringToHash("IsPush");
        private static readonly int IsSitting = Animator.StringToHash("IsSitting");

        public Animator Animator { get; private set; }
        private Player player;
        public CharacterController3D Controller3D { get; private set; }
        public ItemInteractionHandler ItemInteraction { get; private set; }
        
        [SerializeField] private bool canMove = true;
        // 각 상태를 제어하기 위한 멤버 변수들
        
        private Dictionary<int, PlayerAnimStateBase> StateDic { get; set; } = new();
        private PlayerAnimStateBase defaultState; // 예외처리를 위한 기본 상태
        private PlayerAnimStateBase currentState; // 상태객체를 담아서 제어하기 위한 빈 객체 변수 

        private void Awake()
        {
            Animator = GetComponentInChildren<Animator>();
            Controller3D = GetComponent<CharacterController3D>();
            ItemInteraction = GetComponent<ItemInteractionHandler>();
            
            ItemInteraction.OnStartGetItem += GetBox;
            ItemInteraction.OnStartPutDownItem += PutDownBox;

            /*
            defaultState = new MovementState(this);

            StateDic.Add(Animator.StringToHash(ANIM_TAG_MOVEMENT), defaultState);
            StateDic.Add(Animator.StringToHash(ANIM_TAG_ATTACK), new AttackState(this));
            StateDic.Add(Animator.StringToHash(ANIM_TAG_ATTACK_END), new AttackEndState(this));
            StateDic.Add(Animator.StringToHash(ANIM_TAG_JUMP), new JumpState(this));
            */
        }

        private void Update()
        {
            if (canMove == false)
            {
                // 나중에 여기에  return 빼고 회의실에서의 동작 넣어야함
                return;
            }
            TryGetItem();
            TryPutDownItem();
            
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
            if (SimpleInput.GetKeyDown(Key.Space))
            {
                
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
        
        public void PushPlayer(Vector3 hitDirection, float power)
        {
            Controller3D.ApplyPush(hitDirection, power);
        }

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
    }
}