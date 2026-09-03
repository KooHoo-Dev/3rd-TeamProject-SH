using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.Serialization;

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

        [Header("발언 설정")]
        public TextMeshProUGUI speakerNoticeText;

        [Header("카테고리 설정")]
        public GameObject categoryUI;
        public TextMeshProUGUI categoryText;

        [Header("장르, 키워드 알림 설정")] 
        public GameObject genreKeywordNoticeUI;
        public TextMeshProUGUI currentRoundText;
        public TextMeshProUGUI genreText;
        public TextMeshProUGUI keywordText;

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
            ApplySpeakerNoticeUI(false);
            ApplyCategoryCycleUI(false);
            ApplyGenreKeywordUI(false);
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
        
        # region 발언자 표시 UI, 발언 순서 표시 UI, 장르/키워드 첫 알림 UI
        
        // 발언자 차례 표시
        public void ApplySpeakerNoticeUI(bool isShow, string userNickname = "", string itemName = "")
        {
            if (isShow == false)
            {
                speakerNoticeText.gameObject.SetActive(false);
                return;
            }

            if (string.IsNullOrEmpty(userNickname))
            {
                Debug.LogWarning("유저 닉네임이 존재하지 않습니다.");
                speakerNoticeText.gameObject.SetActive(false);
                return;
            }
            
            speakerNoticeText.SetText($"{userNickname}이 발언 중입니다.\n 공개된 아이템 : {itemName}");
            speakerNoticeText.gameObject.SetActive(true);
        }
        
        // 카테고리 표시
        public void ApplyCategoryCycleUI(bool isShow, string categoryName = "", int currentCycle = 1)
        {
            if (isShow == false)
            {
                categoryUI.SetActive(false);
            }

            categoryText.SetText($"카테고리 : {categoryName} \n 사이클 {currentCycle} / 3");
            categoryUI.SetActive(true);
        }
        
        // 장르 & 키워드 첫 알림 UI
        public void ApplyGenreKeywordUI(bool isShow, int currentRound = 1, string genre = "", string keyword = "")
        {

            if (isShow == false)
            {
                genreKeywordNoticeUI.SetActive(false);
                return;
            }

            if (string.IsNullOrEmpty(genre) || string.IsNullOrEmpty(keyword))
            {
                genreKeywordNoticeUI.SetActive(false);
                return;
            }

            currentRoundText.SetText($"라운드 {currentRound}");
            genreText.SetText($"장르 : {genre}");
            keywordText.SetText($"키워드 : {keyword}");

            genreKeywordNoticeUI.SetActive(true);
        }


        # endregion
    }
}