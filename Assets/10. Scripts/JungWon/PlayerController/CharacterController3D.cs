using UnityEngine;
using System;

namespace JunWon.Scripts
{
    public class CharacterController3D : MonoBehaviour
    {
        [Header("기본 설정")]
        public float     speed               = 2.0f;
        public float     skinWidth           = 0.02f;
        public LayerMask collisionLayer;

        private Collider col;
        private Rigidbody rBody;
        
        private float moveX;
        private float moveZ;
        
        [Header("외부 이동값 설정")]
        public float externalDrag = 12.0f;
        private Vector3 externalVelocity = Vector3.zero; //외부 충격 이동값

        [Header("입력 요청 설정")] 
        private bool getItemRequested = false;
        private bool pushRequested = false;
        
        [Header("내부 설정")]
        public float HorizontalVelocity { get; private set; } = 0.0f; // 최종 x값
        public float VerticalVelocity { get; private set; } = 0.0f; // 최종 z값

        #region 이벤트
        public event Action StartPush;
        public event Action EndPush;
        public event Action StartGetItem;
        public event Action EndGetItem;
        public event Action StartPutDownItem;
        public event Action EndPutDownItem;
        #endregion

        private void Awake()
        {
            col = GetComponent<Collider>();
            rBody = GetComponent<Rigidbody>();
            //rBody.isKinematic = true; 테스트중
        }

        private void FixedUpdate()
        {
            HorizontalVelocity = moveX * speed +  externalVelocity.x;
            VerticalVelocity = moveZ * speed + externalVelocity.z;
            
            Vector3 desireMove =
                new Vector3(HorizontalVelocity, 0, VerticalVelocity) * Time.fixedDeltaTime;

            Bounds box = GetCastBox();

            Vector3 moveVector = Vector3.zero;
            
            float boxX = ResolveAxisMovement(box, desireMove.x, Vector3.right);
            box.center += new Vector3(boxX, 0, 0);
            float boxZ = ResolveAxisMovement(box, desireMove.z, Vector3.forward);

            moveVector = new Vector3(boxX, 0, boxZ);

            if (moveVector.x != desireMove.x) HorizontalVelocity = 0.0f;
            if (moveVector.z != desireMove.z) VerticalVelocity = 0.0f;
            
            // 넉백은 잠깐 남게 두고 감쇠
            externalVelocity = Vector3.Lerp(
                externalVelocity,
                Vector3.zero,
                externalDrag * Time.fixedDeltaTime);
            
            if (externalVelocity.z < 0.0f) externalVelocity.z = 0.0f;

            rBody.MovePosition(transform.position + moveVector);
        }
        
        #region Public Methods (명령 통로)
        public void SetMoveInput(Vector3 moveInput)
        {
            moveX = Mathf.Clamp(moveInput.x, -1.0f, 1.0f);
            moveZ = Mathf.Clamp(moveInput.z, -1.0f, 1.0f);
        }
        
        public void ApplyPush(Vector3 direction, float power)
        {
            if (power <= 0.0f) return;

            moveX = 0.0f;
            moveZ = 0.0f;

            externalVelocity += direction * power;
        }

        public void RequestGetItem()
        {
            if (getItemRequested) return;
            getItemRequested = true;
        }
        
        public void RequestPush()
        {
            if (pushRequested) return;
            pushRequested = true;
        }
        
        #endregion
        
        #region 이동값 보정
        
        private float ResolveAxisMovement(Bounds box, float move, Vector3 axis) //기본 이동값 보정
        {
            if (move == 0.0f) return 0.0f;

            Vector3 dir = move > 0.0f ? axis : -axis;
            float distance = Mathf.Abs(move);

            //RaycastHit hit = Physics.BoxCast(box.center, box.size, 0.0f, dir, distance, collisionLayer);

            RaycastHit hit;
            // 3D에서는 box.size대신 extents를 사용한다고합니다.
            bool isHit = Physics.BoxCast(
                box.center,
                box.extents,
                dir,
                out hit,
                Quaternion.identity,
                distance,
                collisionLayer
            );
            
            if (!isHit) return move;
            
            float allowed = Mathf.Max(0, hit.distance - skinWidth);

            return Mathf.Sign(move) * allowed;
        }
        
        private Bounds GetCastBox()
        {
            Bounds box = col.bounds; 

            Vector3 size = box.size;
            size.x -= skinWidth * 2;
            size.z -= skinWidth * 2;
            box.size = size;

            return box;
        }
        
        #endregion
        
        public void TryGetItem() 
        {
            if (getItemRequested == false) return;
            getItemRequested = false;
            // 여기 방법이 2가지인데, 현재 collider설정 사용중이니 box충돌한 아이템 정보를 가져오거나
            // static map데이터 접근해서 현재 타일위치 및 정보 받아와서 해당 타일위치의 아이템 정보 가져오는건데, 
            // 현재 map데이터 코드가 없으므로 보류.
            TryGetItemFromStand(); 
        }
        
        public MartItem TryGetItemFromStand()
        {
            // 1. 플레이어의 현재 위치(target)를 기준으로 어떤 진열대에 들어와 있는지 확인
            DisplayStand currentStand = DisplayStandManager.Instance.CheckWhenStend(transform.position);

            // 2. 플레이어 위치에 진열대가 존재하는지 검사
            if (currentStand != null)
            {
                // 3. 해당 진열대에 배치된 아이템 가져오기
                MartItem item = currentStand.Item;

                if (item != null)
                {
                    Debug.Log($"[{currentStand.category}] 카테고리의 아이템을 찾았습니다!");
                    return item;
                }
                else
                {
                    Debug.Log("진열대는 존재하지만 아이템이 비어있습니다.");
                }
            }
            else
            {
                Debug.Log("현재 위치에는 어떠한 진열대도 없습니다.");
            }

            return null;
        }
        
        public void ApplyPush()
        {
            if (pushRequested == false) return;
            pushRequested = false;
        }
        
        #region ResolveAxisMovement 시각화 코드

        [Header("캐스트 시각화 설정")]
        // 시각화 표시 on/off
        public bool showCastGizmos = true;

        // 시각화에서 박스를 쏴 볼 거리 (보기 좋게, 실제 이동 거리와는 무관)
        public float castPreviewDistance = 0.5f;

        // 선/박스를 그릴 때 쓰는 1x1 흰색 텍스처
        private static Texture2D whiteTexture;

        // # OnGUI : 매 프레임 화면(게임 뷰)에 GUI를 그리는 이벤트 함수
        private void OnGUI()
        {
            if (showCastGizmos == false) return;

            Camera cam = Camera.main;
            if (cam == null || col == null) return;

            // 캐스트에 쓸 박스(콜라이더보다 살짝 작게)를 여기서 직접 계산합니다.
            Bounds bounds = col.bounds;
            Vector2 boxCenter = bounds.center;
            Vector2 boxSize = (Vector2)bounds.size - Vector2.one * (skinWidth * 2.0f);

            // 상/하/좌/우 네 방향으로 박스를 쏴 보고 그 결과를 그립니다.
            DrawBoxCast(cam, boxCenter, boxSize, Vector2.up);
            DrawBoxCast(cam, boxCenter, boxSize, Vector2.down);
            DrawBoxCast(cam, boxCenter, boxSize, Vector2.left);
            DrawBoxCast(cam, boxCenter, boxSize, Vector2.right);
        }

        // 한 방향으로 BoxCast를 직접 수행하고, 그 모양을 박스로 그립니다.
        private void DrawBoxCast(Camera cam, Vector2 center, Vector2 size, Vector2 dir)
        {
            RaycastHit2D hit = Physics2D.BoxCast(
            center, size, 0.0f, dir, castPreviewDistance, collisionLayer);

            Rect startRect = WorldBoxToGuiRect(cam, center, size);
            Rect endRect = WorldBoxToGuiRect(cam, center + dir * castPreviewDistance, size);

            // 검사 영역(시작~끝 전체) : 노란색
            DrawRectOutline(Encapsulate(startRect, endRect), 2.0f, Color.yellow);
            // 캐스트 시작 박스 : 흰색
            DrawRectOutline(startRect, 2.0f, Color.white);

            if (hit.collider != null)
            {
                // 무언가 걸렸으면 걸린 자리에 빨간 박스
                Rect hitRect = WorldBoxToGuiRect(cam, center + dir * hit.distance, size);
                DrawRectOutline(hitRect, 2.0f, Color.red);
            }
            else
            {
                // 아무것도 없으면 끝까지 간 자리에 초록 박스
                DrawRectOutline(endRect, 2.0f, Color.green);
            }
        }

        // 월드 공간의 축정렬 박스(center, size)를 GUI 좌표 Rect로 변환합니다.
        private Rect WorldBoxToGuiRect(Camera cam, Vector2 center, Vector2 size)
        {
            float z = transform.position.z;
            Vector3 worldMin = new Vector3(center.x - size.x * 0.5f, center.y - size.y * 0.5f, z);
            Vector3 worldMax = new Vector3(center.x + size.x * 0.5f, center.y + size.y * 0.5f, z);

            // 월드 좌표 -> 스크린 좌표 (스크린은 좌하단이 원점)
            Vector3 a = cam.WorldToScreenPoint(worldMin);
            Vector3 b = cam.WorldToScreenPoint(worldMax);

            float left = Mathf.Min(a.x, b.x);
            float right = Mathf.Max(a.x, b.x);
            // GUI 좌표는 좌상단이 원점이고 y가 아래로 증가 -> y를 뒤집어 줍니다.
            float top = Screen.height - Mathf.Max(a.y, b.y);
            float bottom = Screen.height - Mathf.Min(a.y, b.y);

            return new Rect(left, top, right - left, bottom - top);
        }

        // 두 Rect를 모두 감싸는 가장 작은 Rect를 만듭니다.
        private Rect Encapsulate(Rect a, Rect b)
        {
            return Rect.MinMaxRect(
                Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin),
                Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
        }

        // 사각형 "테두리"를 얇은 박스 4개(위/아래/왼/오른)로 그립니다.
        private void DrawRectOutline(Rect r, float thickness, Color color)
        {
            if (whiteTexture == null)
            {
                whiteTexture = new Texture2D(1, 1);
                whiteTexture.SetPixel(0, 0, Color.white);
                whiteTexture.Apply();
            }

            Color prev = GUI.color;
            GUI.color = color;

            GUI.DrawTexture(new Rect(r.xMin, r.yMin, r.width, thickness), whiteTexture); // 위
            GUI.DrawTexture(new Rect(r.xMin, r.yMax - thickness, r.width, thickness), whiteTexture); // 아래
            GUI.DrawTexture(new Rect(r.xMin, r.yMin, thickness, r.height), whiteTexture); // 왼쪽
            GUI.DrawTexture(new Rect(r.xMax - thickness, r.yMin, thickness, r.height), whiteTexture); // 오른쪽

            GUI.color = prev;
        }

        #endregion
    }

}