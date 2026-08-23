using Study.Utilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimTest : MonoBehaviour
{
    private static readonly int IsMove = Animator.StringToHash("IsMove");
    private static readonly int Item = Animator.StringToHash("GetItem");
    private static readonly int PutDown = Animator.StringToHash("PutDown");

    public float moveSpeed = 3f;
    public Vector3 moveVec;
    public Animator anim;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        GetItem();
        PutDownItem();
        
        moveVec = new Vector3();
        moveVec.x = SimpleInput.GetAxisHorizontalRaw();
        moveVec.z = SimpleInput.GetAxisVerticalRaw();
        if(moveVec.sqrMagnitude > 1f) moveVec = moveVec.normalized;
        if (moveVec != Vector3.zero) anim.gameObject.transform.forward = moveVec;

        transform.Translate(moveVec * moveSpeed * Time.deltaTime);

        anim.SetBool(IsMove, (moveVec != Vector3.zero));
    }

    private void GetItem()
    {
        if (SimpleInput.GetKeyDown(Key.Digit1))
        {
            anim.SetLayerWeight(1,1);
            anim.SetBool(Item, true);
        }
    }

    private void PutDownItem()
    {
        if (SimpleInput.GetKeyDown(Key.Digit2))
        {
            anim.SetLayerWeight(0,0);
            anim.SetBool(Item, false);
            anim.SetTrigger(PutDown);
        }
    }
}
