using Unity.VisualScripting;
using UnityEngine;

public class FollowingPlayer : MonoBehaviour
{
    public GameObject Target;               // 카메라가 따라다닐 타겟
    public float CameraSpeed = 10.0f;       // 카메라의 속도
    private Vector3 TargetPos;                      // 타겟의 위치

    private float offsetX;
    private float offsetY;
    private float offsetZ;
    
    private void Awake()
    {
        offsetX = transform.position.x;
        offsetY = transform.position.y;
        offsetZ = transform.position.z;
    }
    
    // Update is called once per frame
    private void FixedUpdate()
    {
        // 타겟의 x, y, z 좌표에 카메라의 좌표를 더하여 카메라의 위치를 결정
        TargetPos = new Vector3(
            Target.transform.position.x + offsetX,
            Target.transform.position.y + offsetY,
            Target.transform.position.z + offsetZ
        );

        // 카메라의 움직임을 부드럽게 하는 함수(Lerp)
        transform.position = Vector3.Lerp(transform.position, TargetPos, Time.deltaTime * CameraSpeed);
    }
}
