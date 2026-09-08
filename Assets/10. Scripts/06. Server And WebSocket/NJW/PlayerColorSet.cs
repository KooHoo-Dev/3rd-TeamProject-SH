using UnityEngine;

namespace TeamSH
{
    public class PlayerColorSet : MonoBehaviour
    {
        [System.Serializable]
        public enum OutlineMode
        {
            SameFamilyBright,
            ComplementaryBright,
            FixedWhite,
        }

        [Header("팔레트 (인스펙터에서 변경 가능)")]
        [SerializeField]
        private Color[] palette = new Color[]
        {
            ColorFromHSV(  0f, 0.78f, 0.95f), // 빨강
            ColorFromHSV( 28f, 0.82f, 0.95f), // 주황
            ColorFromHSV( 50f, 0.62f, 0.90f), // 노랑
            ColorFromHSV(115f, 0.55f, 0.90f), // 초록
        };

        [Header("외곽선 색상 모드")]
        [SerializeField] private OutlineMode outlineMode = OutlineMode.SameFamilyBright;

        [Header("외곽선 셰이더 (Custom/StickmanColorOutline)")]
        [Tooltip("Shader.Find 폴백에만 의존하지 말고 여기 직접 셰이더를 드래그해서 연결하세요 (빌드 시 누락 방지)")]
        [SerializeField] public Shader outlineShader;

        [Tooltip("외곽선 두께. Range 상한 0.2 안에서 0.08~0.15 권장 (기본 0.06은 얇게 느껴질 수 있음)")]
        [SerializeField] private float outlineWidth = 0.1f;

        // 런타임 생성 아웃라인 머티리얼을 식별하기 위한 고정 이름.
        // 배열 길이만으로 "이미 아웃라인이 붙어있는지"를 판단하면
        // 원본 캐릭터가 재질 슬롯을 2개 이상 쓰는 경우 오작동하므로 이름으로 정확히 구분한다.
        private const string OutlineMatName = "Outline_Runtime";

        private static readonly int BaseColorId    = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId        = Shader.PropertyToID("_Color");        // Standard 셰이더 폴백
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");  // 셰이더 Properties와 이름 일치
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");  // 셰이더 Properties와 이름 일치

        public Color[] Palette         => palette;
        public OutlineMode CurrentMode => outlineMode;

        public float OutlineWidth
        {
            get => outlineWidth;
            set => outlineWidth = value;
        }

        public void SetPalette(Color[] colors) => palette = colors;
        public void SetOutlineMode(OutlineMode mode) => outlineMode = mode;

        private static Color ColorFromHSV(float h, float s, float v)
            => Color.HSVToRGB(h / 360f, s, v);

        // ================= ID → 팔레트 색 =================
        public Color GetColor(string id)
        {
            if (string.IsNullOrEmpty(id) || palette == null || palette.Length == 0)
                return Color.white;
            
            int orderIndex = -1;
            for (int i = 0; i < LiarClient.Instance.userOrder.Count; i++)
            {
                if (LiarClient.Instance.userOrder[i] == id) orderIndex = i;
            }
            
            // userOrder에서 찾지 못했을 경우 폴백 처리 (기본 ID 해시 또는 흰색)
            if (orderIndex < 0)
            {
                return Color.white;
            }

            // 순서(0, 1, 2, 3)를 직접 팔레트 인덱스로 사용
            int idx = orderIndex % palette.Length;
            return palette[idx];
        }

        // ================= 외곽선용 색 =================
        public Color GetOutlineColor(string id)
        {
            Color baseColor = GetColor(id);
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);

            switch (outlineMode)
            {
                case OutlineMode.SameFamilyBright:
                    return Color.HSVToRGB(h, Mathf.Max(0f, s - 0.05f), Mathf.Min(1f, v + 0.15f));
                case OutlineMode.ComplementaryBright:
                    return Color.HSVToRGB(Mathf.Repeat(h + 0.5f, 1f), Mathf.Max(0f, s - 0.05f), Mathf.Min(1f, v + 0.15f));
                case OutlineMode.FixedWhite:
                default:
                    return Color.white;
            }
        }

        // ================= 몸통 색 =================
        public void ApplyColor(SkinnedMeshRenderer skin, Color color)
        {
            if (skin == null)
            {
                Debug.LogWarning("[색상] skin 이 null 이다.");
                return;
            }

            Material mat = skin.material; // 최초 접근 시 인스턴스화, 이후 캐시된 동일 인스턴스 반환
            if (mat == null) return;

            if (mat.HasProperty(BaseColorId))
            {
                mat.SetColor(BaseColorId, color);
            }
            else if (mat.HasProperty(ColorId))
            {
                // Standard 등 _Color 프로퍼티를 쓰는 셰이더 대비 폴백
                mat.SetColor(ColorId, color);
            }
            else
            {
                Debug.LogWarning($"[색상 실패] {skin.gameObject.name} 의 셰이더 '{mat.shader.name}' 에 " +
                                  "_BaseColor / _Color 둘 다 없습니다. 실제 몸통 셰이더를 확인하세요.");
                return;
            }

            Debug.Log($"[색상] {skin.gameObject.name} → R{color.r:F2}G{color.g:F2}B{color.b:F2}");
        }

        // ================= 외곽선 (StickmanColorOutline, 머티리얼 배열 추가/제거) =================
        // on=true  : materials 배열 끝에 아웃라인 머티리얼을 1개 추가 (원본 슬롯들은 그대로 유지)
        // on=false : "StickmanColorOutline 셰이더를 쓰는" 머티리얼만 정확히 찾아 제거 후 Destroy
        //
        // [주의] 이름(mat.name)으로 비교하면 안 된다.
        // Unity가 렌더러에 머티리얼을 인스턴스화할 때 이름 뒤에 자동으로 " (Instance)"를 붙이기 때문에
        // == 비교가 항상 false가 되어 OFF가 동작하지 않는 버그가 생긴다.
        // 셰이더 참조는 이름과 무관하게 항상 동일한 에셋을 가리키므로 이 방식이 훨씬 견고하다.
        public void SetOutline(SkinnedMeshRenderer skin, bool on, Color rimColor)
        {
            if (skin == null) return;

            Material[] mats = skin.materials; // 접근 자체가 인스턴스 배열을 반환

            Shader targetShader = outlineShader != null
                ? outlineShader
                : Shader.Find("Custom/StickmanColorOutline");

            if (on)
            {
                Material last = mats.Length > 0 ? mats[mats.Length - 1] : null;

                // 이미 마지막 슬롯이 아웃라인 셰이더면 새로 만들지 말고 색/두께만 갱신
                if (last != null && last.shader == targetShader)
                {
                    last.SetColor(OutlineColorId, rimColor);
                    last.SetFloat(OutlineWidthId, outlineWidth);
                    return; // 배열 자체는 바뀌지 않았으므로 재할당 불필요
                }

                Material outlineMat = CreateOutlineMaterial(rimColor);
                if (outlineMat == null) return;

                Material[] newMats = new Material[mats.Length + 1];
                for (int i = 0; i < mats.Length; i++) newMats[i] = mats[i];
                newMats[mats.Length] = outlineMat;
                skin.materials = newMats;

                Debug.Log($"[외곽선] ON  {skin.gameObject.name}, width={outlineWidth}");
            }
            else
            {
                int removeIdx = -1;
                for (int i = mats.Length - 1; i >= 0; i--)
                {
                    // 이름이 아니라 "이 머티리얼이 아웃라인 셰이더를 쓰는가"로 판별
                    if (mats[i] != null && mats[i].shader == targetShader)
                    {
                        removeIdx = i;
                        break;
                    }
                }

                if (removeIdx < 0) return; // 제거할 아웃라인이 없으면 조용히 종료 (중복 OFF 호출 방어)

                Material toRemove = mats[removeIdx];

                Material[] newMats = new Material[mats.Length - 1];
                int dst = 0;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (i == removeIdx) continue;
                    newMats[dst++] = mats[i];
                }

                skin.materials = newMats;

                Destroy(toRemove); // 런타임 생성 인스턴스 메모리 정리
                Debug.Log($"[외곽선] OFF {skin.gameObject.name}");
            }
        }

        private Material CreateOutlineMaterial(Color color)
        {
            Shader shader = outlineShader != null ? outlineShader : Shader.Find("Custom/StickmanColorOutline");

            Material mat = new Material(shader) { name = OutlineMatName };
            mat.SetColor(OutlineColorId, color);
            mat.SetFloat(OutlineWidthId, outlineWidth);
            return mat;
        }
    }
    // ================= RichText 색상 문자열 변환 유틸 =================
    public static class RichTextUtils
    {
        public static string ToColoredRichText(string text, Color color)
        {
            string hex = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{hex}>{text}</color>";
        }
    }
}
