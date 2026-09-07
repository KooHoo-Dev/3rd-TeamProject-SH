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
    }
}
