using UnityEngine;

namespace TeamSH
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PointArrow : MonoBehaviour
    {
        [Header("크기")]
        [SerializeField] private float shaftWidth = 0.06f;
        [SerializeField] private float headWidth = 0.18f;
        [SerializeField] private float headLength = 0.16f;
        [SerializeField] private float heightOffset = 0.03f;

        [Header("곡선 분할")]
        [SerializeField, Range(3, 60)] private int segments = 24;

        [Header("곡률")]
        [SerializeField] private float sideCurveStrength = 0.45f;
        [SerializeField] private float frontLift = 0.05f;

        [Header("머티리얼")]
        [SerializeField, Range(0f, 1f)] private float alpha = 0.9f;

        [Header("진행도 구간 분배")]
        // 0~shaftPhaseEnd : 화살대가 곡선을 따라 자라남
        // shaftPhaseEnd~1 : 화살촉(삼각형)이 밑변 폭 0에서 완전한 크기로 펼쳐짐
        [SerializeField, Range(0.5f, 0.98f)] private float shaftPhaseEnd = 0.85f;

        // 0 = 화살표 없음, 1 = 완성된 화살표. ConferenceManager가 코루틴으로 갱신한다.
        [HideInInspector] public float DrawProgress = 1f;

        private MeshFilter mf;
        private MeshRenderer mr;
        private Mesh mesh;
        private Material runtimeMat;

        // Setup() 시점에 계산해 두는 "완성된" 곡선 데이터
        private Vector3 cachedStart;
        private Vector3 cachedEnd;
        private Vector3 cachedControl;
        private Vector3 cachedShaftEnd;
        private Vector3 cachedEndTangent;
        private bool isReady;
        private float lastBuiltProgress = -1f; // 값이 안 바뀌면 재생성을 건너뛰기 위한 캐시

        private void Awake()
        {
            mf = GetComponent<MeshFilter>();
            mr = GetComponent<MeshRenderer>();
            mesh = new Mesh { name = "PointArrow_Mesh" };
            mf.mesh = mesh;

            if (mr.sharedMaterial == null)
            {
                Shader sh = Shader.Find("Sprites/Default");
                if (sh == null) sh = Shader.Find("Unlit/Color");
                runtimeMat = new Material(sh) { name = "PointArrow_Runtime" };
                mr.sharedMaterial = runtimeMat;
            }
        }

        private void LateUpdate()
        {
            if (!isReady) return;
            if (Mathf.Approximately(DrawProgress, lastBuiltProgress)) return;
            RebuildMesh(DrawProgress);
            lastBuiltProgress = DrawProgress;
        }

        private void OnDestroy()
        {
            if (mesh != null) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
            if (runtimeMat != null) { if (Application.isPlaying) Destroy(runtimeMat); else DestroyImmediate(runtimeMat); }
        }

        private void LockTransformIdentity()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        // 화살표의 시작/끝/색을 확정한다.
        // DrawProgress는 이 메서드가 건드리지 않으므로, 호출 전에 원하는 초기값(보통 0)으로 세팅해 둘 것.
        public void Setup(Transform fromAnchor, Transform toAnchor, Color color)
        {
            if (mesh == null) Awake();
            LockTransformIdentity();

            Color finalColor = color; finalColor.a = alpha;
            Material m = mr.sharedMaterial;
            if (m != null)
            {
                if (m.HasProperty("_Color")) m.SetColor("_Color", finalColor);
                if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", finalColor);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", finalColor);
            }

            Vector3 start = fromAnchor.position;
            Vector3 end = toAnchor.position;
            float baseY = Mathf.Lerp(start.y, end.y, 0.5f) + heightOffset;
            start.y = baseY;
            end.y = baseY;

            Vector3 normal = Vector3.up;
            Vector3 chordXZ = new Vector3(end.x - start.x, 0f, end.z - start.z);
            float chordLen = Mathf.Max(chordXZ.magnitude, 1e-4f);
            Vector3 dirFlat = chordXZ / chordLen;

            // 원본 그대로: 지목자의 정면 방향에 따라 곡률(측면/정면)을 다르게 준다.
            Vector3 fwdFlat = new Vector3(fromAnchor.forward.x, 0f, fromAnchor.forward.z);
            if (fwdFlat.sqrMagnitude < 1e-6f) fwdFlat = Vector3.forward;
            fwdFlat.Normalize();
            float tSide = Mathf.InverseLerp(20f, 70f, Vector3.Angle(fwdFlat, dirFlat));

            Vector3 perp = Vector3.Cross(normal, dirFlat);
            if (perp.sqrMagnitude < 1e-6f) perp = Vector3.Cross(normal, Vector3.forward);
            perp.Normalize();

            Vector3 mid = (start + end) * 0.5f;
            Vector3 control = mid + perp * (chordLen * sideCurveStrength * tSide) + normal * (frontLift * (1f - tSide));

            Vector3 endTanRaw = (end - control) * 2f;
            endTanRaw.y = 0f;
            if (endTanRaw.sqrMagnitude < 1e-6f) endTanRaw = dirFlat;
            Vector3 endTangent = endTanRaw.normalized;

            Vector3 shaftEnd = end - endTangent * headLength;
            shaftEnd.y = baseY;

            cachedStart = start;
            cachedEnd = end;
            cachedControl = control;
            cachedShaftEnd = shaftEnd;
            cachedEndTangent = endTangent;
            isReady = true;

            lastBuiltProgress = -1f; // 첫 프레임에 즉시 재생성되게
            RebuildMesh(DrawProgress);
            lastBuiltProgress = DrawProgress;
        }

        // progress(0~1)에 맞춰 메시를 다시 만든다.
        private void RebuildMesh(float progress)
        {
            float t = Mathf.Clamp01(progress);

            if (t <= 0.001f)
            {
                mesh.Clear();
                return;
            }

            float shaftT = Mathf.Clamp01(t / shaftPhaseEnd);
            float headT = Mathf.Clamp01((t - shaftPhaseEnd) / (1f - shaftPhaseEnd));

            Vector3 normal = Vector3.up;
            Vector3 shaftEndNow;
            Vector3 controlNow;
            Vector3 endTangentNow;

            if (shaftT >= 1f)
            {
                shaftEndNow = cachedShaftEnd;
                controlNow = cachedControl;
                endTangentNow = cachedEndTangent;
            }
            else
            {
                // De Casteljau 분할: [0, shaftT] 구간만 남긴 새 곡선
                controlNow = Vector3.Lerp(cachedStart, cachedControl, shaftT);          // Q1
                shaftEndNow = Bezier2(cachedStart, cachedControl, cachedShaftEnd, shaftT); // Q2
                shaftEndNow.y = cachedStart.y;

                Vector3 tan = BezierTangent(cachedStart, cachedControl, cachedShaftEnd, shaftT);
                tan.y = 0f;
                if (tan.sqrMagnitude < 1e-8f) tan = cachedEndTangent;
                endTangentNow = tan.normalized;
            }

            int N = Mathf.Clamp(segments + 1, 3, 65);
            int shaftVerts = N * 2;
            Vector3[] verts = new Vector3[shaftVerts + 3];
            Vector2[] uvs = new Vector2[verts.Length];
            int[] tris = new int[(N - 1) * 6 + 3];

            for (int i = 0; i < N; i++)
            {
                float frac = i / (float)(N - 1);
                Vector3 p = Bezier2(cachedStart, controlNow, shaftEndNow, frac);
                p.y = cachedStart.y;

                Vector3 prev = Bezier2(cachedStart, controlNow, shaftEndNow, Mathf.Max(frac - 1f / (N - 1), 0f));
                Vector3 next = Bezier2(cachedStart, controlNow, shaftEndNow, Mathf.Min(frac + 1f / (N - 1), 1f));
                Vector3 tanXZ = next - prev;
                tanXZ.y = 0f;
                if (tanXZ.sqrMagnitude < 1e-8f) tanXZ = endTangentNow;
                tanXZ.Normalize();

                Vector3 widthDir = Vector3.Cross(normal, tanXZ).normalized;
                float halfWidth = shaftWidth * 0.5f;

                verts[i * 2] = p - widthDir * halfWidth;
                verts[i * 2 + 1] = p + widthDir * halfWidth;
                uvs[i * 2] = new Vector2(frac, 0f);
                uvs[i * 2 + 1] = new Vector2(frac, 1f);
            }

            int triIdx = 0;
            for (int i = 0; i < N - 1; i++)
            {
                int i0 = i * 2, i1 = i0 + 1, i2 = (i + 1) * 2, i3 = i2 + 1;
                tris[triIdx++] = i0; tris[triIdx++] = i2; tris[triIdx++] = i1;
                tris[triIdx++] = i1; tris[triIdx++] = i2; tris[triIdx++] = i3;
            }

            Vector3 headPerp = Vector3.Cross(normal, endTangentNow).normalized;
            int baseL = shaftVerts, baseR = shaftVerts + 1, tip = shaftVerts + 2;

            if (shaftT >= 1f && headT > 0f)
            {
                float curHeadWidth = Mathf.Lerp(0f, headWidth, headT);
                Vector3 tipPos = Vector3.Lerp(cachedShaftEnd, cachedEnd, headT);
                tipPos.y = cachedStart.y;

                verts[baseL] = cachedShaftEnd + headPerp * (curHeadWidth * 0.5f);
                verts[baseR] = cachedShaftEnd - headPerp * (curHeadWidth * 0.5f);
                verts[tip] = tipPos;
            }
            else
            {
                // 아직 머리 단계가 아니면 세 점을 shaft 끝 한곳에 모아 삼각형을 접어둔다 (안 보임)
                verts[baseL] = shaftEndNow;
                verts[baseR] = shaftEndNow;
                verts[tip] = shaftEndNow;
            }

            uvs[baseL] = new Vector2(1f, 0f);
            uvs[baseR] = new Vector2(1f, 1f);
            uvs[tip] = new Vector2(1.05f, 0.5f);

            tris[triIdx++] = baseL;
            tris[triIdx++] = baseR;
            tris[triIdx++] = tip;

            mesh.Clear();
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        private static Vector3 Bezier2(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }

        // 2차 베지어의 1차 도함수(접선 방향)
        private static Vector3 BezierTangent(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            return 2f * (1f - t) * (b - a) + 2f * t * (c - b);
        }
    }
}