using DG.Tweening;
using UnityEngine;

namespace TeamSH
{
    public class LightManager : MonoBehaviour
    {
        public static LightManager Instance;
    
        [Header("Lights")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private Light spotLight;

        private float zeroIntensity = 0f;
        private float fullIntensity = 1f;
        
        // 조명 밝기 Intensity 인스펙터 필드 (추가함)
        [SerializeField] private float goalIntensity = 650f;
        [SerializeField] private float tweenTime = 5f;
        [SerializeField] private float focusRange = 10f;
        [SerializeField] private float tableRange = 30f;

        [SerializeField] private Transform tableTransform;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(this);

            InitLights();
        }

        private void InitLights()
        {
            directionalLight.intensity = fullIntensity;
            RenderSettings.ambientIntensity = fullIntensity;
            spotLight.enabled = false;
        }

        // 발언자를 매개변수로 넣어 발언할 때 주변 조명을 끄고 해당 발언자에 스포트라이트를 켜게 해준다.
        public void TurnOnSpotLight(Transform currentSpeaker)
        {
            directionalLight.intensity = zeroIntensity;
            RenderSettings.ambientIntensity = zeroIntensity;
            spotLight.transform.position =
                new Vector3(currentSpeaker.position.x, 10f, currentSpeaker.position.z);
            spotLight.enabled = true;
        }
    
        // 모든 발언이 끝나면 통상적인 상태로 초기화해준다.
        public void TurnOffSpotLight()
        {
            InitLights();
        }

        // Dotween 기반 조명 특정 트랜스폼의 조명 켜기
        public void TurnOnDotween(Transform currentSpeaker) // 0 -> 목표 밝기까지 3초동안 천천히 증가 (OutQuad)
        {
            directionalLight.intensity = zeroIntensity;
            RenderSettings.ambientIntensity = zeroIntensity;
            spotLight.transform.position =
                new Vector3(currentSpeaker.position.x, 10f, currentSpeaker.position.z);
            spotLight.intensity = zeroIntensity; // 0 밝기로 강제 지정
            spotLight.range = focusRange; // 특정 사람 전용 Range로 변경
            spotLight.color = Color.white;
            spotLight.enabled = true;
            spotLight.DOIntensity(goalIntensity, tweenTime).SetEase(Ease.OutQuad);
        }
        
        // Dotween 기반 특정 트랜스폼의 조명 끄기
        public void TurnoffDotween(Transform currentSpeaker) // 현재 밝기 -> 0까지 3초동안 천천히 감소 (OutQuad)
        {
            directionalLight.intensity = zeroIntensity;
            RenderSettings.ambientIntensity = zeroIntensity;
            spotLight.transform.position =
                new Vector3(currentSpeaker.position.x, 10f, currentSpeaker.position.z);
            spotLight.intensity = goalIntensity; // 목표 밝기로 강제 지정
            spotLight.enabled = true;
            spotLight.DOIntensity(zeroIntensity, tweenTime).SetEase(Ease.OutQuad);
        }

        public void TurnOnFocusTable()
        {
            directionalLight.intensity = zeroIntensity;
            RenderSettings.ambientIntensity = zeroIntensity;
            spotLight.transform.position =
                new Vector3(tableTransform.position.x, 20f, tableTransform.position.z);
            spotLight.range = tableRange; // 테이블 전용 Range로 변경
            spotLight.intensity = zeroIntensity; // 0 밝기로 강제 지정
            spotLight.color = Color.white;
            spotLight.enabled = true;
            spotLight.DOIntensity(goalIntensity, tweenTime).SetEase(Ease.OutQuad);
        }

        [SerializeField] public float LiarPlusIntensity = 500f; // 라이어용 추가 빛 값
        
        public void TurnOnLiar(Transform currentSpeaker)
        {
            directionalLight.intensity = zeroIntensity;
            RenderSettings.ambientIntensity = zeroIntensity;
            spotLight.transform.position =
                new Vector3(currentSpeaker.position.x, 10f, currentSpeaker.position.z);
            spotLight.range = focusRange; // 특정 사람 전용 range로 설정
            spotLight.intensity = zeroIntensity; // 0 밝기로 강제 지정
            spotLight.color = Color.darkRed; // 라이어 전용 빨간색
            spotLight.enabled = true;
            
            // 라이어는 추가 빛 량을 추가한다.
            spotLight.DOIntensity(goalIntensity + LiarPlusIntensity, tweenTime).SetEase(Ease.OutQuad); 
        }
    }
}
