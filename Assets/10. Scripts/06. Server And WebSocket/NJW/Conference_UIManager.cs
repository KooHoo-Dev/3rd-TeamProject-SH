using UnityEngine;
using DG.Tweening;

namespace TeamSH
{
    public class Conference_UIManager : MonoBehaviour
    {
        public GameObject voteUI;
        public GameObject skipButton;

        [Header("VoteUI 슬라이드 애니메이션 설정")]
        [SerializeField] private float slideDistance = 300f; // 기존 좌표에서 아래로 내려갈 거리(양수 입력)
        [SerializeField] private float tweenDuration = 1f; // 애니메이션 재생 시간(초)
        [SerializeField] private Ease showEase = Ease.OutQuad; // 내려갈 때 이징
        [SerializeField] private Ease hideEase = Ease.InQuad;  // 올라갈 때 이징
        
        private RectTransform voteRect;
        private Vector2 originPos; // "기존 좌표" - 에디터에 배치해 둔 원래 위치(숨김 상태의 기준점)


        private void Awake()
        {
            voteRect = voteUI.GetComponent<RectTransform>();
            originPos = voteRect.anchoredPosition;
        }
        
        // 씬 진입 시 ConferenceManager.Start()가 호출
        public void InitializeUI()
        {
            ApplySkipButton(false);
            ApplyVoteUI(false);
        }

        public void ApplySkipButton(bool isShow)
        {
            skipButton.SetActive(isShow);
        }

        public void ApplyVoteUI(bool isShow)
        {
            DoTweenVoteUI(isShow);
        }

        public void DoTweenVoteUI(bool value)
        {
            voteRect.DOKill();
            
            if (value)
            {
                voteUI.SetActive(true);
                voteRect.anchoredPosition = originPos;

                float targetY = originPos.y - slideDistance;
                voteRect.DOAnchorPosY(targetY, tweenDuration).SetEase(showEase);
            }
            else
            {
                voteRect.DOAnchorPosY(originPos.y, tweenDuration)
                    .SetEase(hideEase)
                    .OnComplete(() =>
                    {
                        voteUI.SetActive(false);
                    });
            }
        }
    }
}