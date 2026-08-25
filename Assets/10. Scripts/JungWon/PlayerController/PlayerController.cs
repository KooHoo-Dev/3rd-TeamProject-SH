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
        private static readonly int IsSitting = Animator.StringToHash("IsSitting");

        public Animator Animator { get; private set; }
        private Player player;
        public CharacterController3D Controller3D { get; private set; }
        public ItemInteractionHandler ItemInteraction { get; private set; }
        
        private ItemDef handItem;

        [SerializeField] private bool canMove = true;
        
        //private PushPlayer pushPlayer { get; set; }

        private void Awake()
        {
            Animator = GetComponentInChildren<Animator>();
            Controller3D = GetComponent<CharacterController3D>();
            ItemInteraction = GetComponent<ItemInteractionHandler>();
            
            ItemInteraction.OnStartGetItem += GetItem;
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
            if (SimpleInput.GetKeyDown(Key.Digit1) && handItem == null)
            {
                ItemInteraction.RequestGetItem();
            }
        }
        
        private void TryPutDownItem()
        {
            if (SimpleInput.GetKeyDown(Key.Digit2) && handItem != null)
            {
                handItem = null;
                Animator.SetLayerWeight(0,0);
                Animator.SetBool(GET_ITEM, false);
                Animator.SetTrigger(PUT_DOWN);
            }
        }
        
        private void GetItem(ItemDef Item)
        {
            handItem = Item;
            Animator.SetLayerWeight(1,1);
            Animator.SetBool(GET_ITEM, true);
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