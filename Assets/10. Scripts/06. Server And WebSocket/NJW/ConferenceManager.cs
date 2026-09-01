using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace TeamSH
{
    public class ConferenceManager : MonoBehaviour
    {
        public static ConferenceManager Instance;

        public Conference_UIManager conferenceUI;
        public Vote_System voteSystem;
        
        public Transform[] userSpawnPoints;

        #region 화살표 설정 관련

        [Header("화살표 설정")]
        public PointArrow arrowPrefab; // LineRenderer + PointArrow가 붙은 프리팹
        
        [Header("화살표 연출 시간")]
        [SerializeField] private float drawDuration = 0.35f;  // 신규 지목: 0→1 그려지는 시간
        [SerializeField] private float eraseDuration = 0.3f;  // 순수 취소: 현재값→0 말려사라지는 시간
        
        [Header("화살표 이징")]
        [SerializeField] private Ease drawEase = Ease.OutQuad;  // 그려질 때 (끝에서 부드럽게 감속)
        [SerializeField] private Ease eraseEase = Ease.InQuad;  // 사라질 때 (서서히 빨라지며 사라짐
        
        private readonly Dictionary<string, PointArrow> activeArrows = new Dictionary<string, PointArrow>();
        // 코루틴 대신 Tween 객체를 추적한다. Kill() 한 번으로 즉시 중단 가능.
        private readonly Dictionary<string, Tween> activeTweens = new Dictionary<string, Tween>();      

        #endregion
  
        
        public void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            conferenceUI = GetComponent<Conference_UIManager>();
            voteSystem = GetComponent<Vote_System>();
            
            // 씬이 새로 로드될 때마다 UI를 항상 꺼진 상태로 초기화한다.
            conferenceUI.InitializeUI();
        }

        // 지목 단계 UI(스킵 버튼 등)를 켜거나 끈다.
        public void RequestSkipPointUI(bool isShow) => conferenceUI.ApplySkipButton(isShow);
        
        // 투표 단계 UI(VoteUI 패널)를 켜거나 끈다.
        public void RequestVoteUI(bool isShow) => conferenceUI.ApplyVoteUI(isShow);
        
        # region 발언자 표시 UI, 발언 순서 표시 UI, 장르/키워드 첫 알림 UI

        public void RequestSpeakerUI(bool isShow, string userNickname = "") => conferenceUI.ShowSpeakerUI(isShow, userNickname);

        public void RequestSpeakerNoticeUI(bool isShow) => conferenceUI.ApplySpeakerNoticeUI(isShow);

        public void RequestGenreKeywordNoticeUI(bool isShow, int currentRound, string genre, string keyword)
            => conferenceUI.ApplyGenreKeywordUI(isShow, currentRound, genre, keyword);
        
        # endregion

        #region 화살표 그리기

        // 신규 지목 / 지목 대상 교체
        public void DrawArrow(string selectId, Transform from, Transform to, Color color)
        {
            KillArrowInstant(selectId); // 교체 상황이면 연출 없이 즉시 사라짐
            
            PointArrow arrow = Instantiate(arrowPrefab);
            arrow.DrawProgress = 0f;
            arrow.Setup(from, to, color);
            activeArrows[selectId] = arrow;

            Tween tween = DOTween.To(
                    () => arrow.DrawProgress,
                    x => arrow.DrawProgress = x,
                    1f,
                    drawDuration)
                .SetEase(drawEase)
                .SetLink(arrow.gameObject); // 오브젝트가 먼저 파괴되면 트윈도 자동으로 Kill됨

            activeTweens[selectId] = tween;
        }

        // 순수 지목 취소.
        public void RemoveArrow(string selectId)
        {
            if (activeArrows.TryGetValue(selectId, out PointArrow arrow) == false)
            {
                activeArrows.Remove(selectId);
                activeTweens.Remove(selectId);
                return;
            }

            KillTrackedTween(selectId); // 그려지는 중이었을 수도 있는 이전 트윈을 먼저 정지

            float startProgress = arrow.DrawProgress; // 그려지는 도중 취소돼도 그 지점부터 자연스럽게 역행

            // 완전히 그려진 상태(1.0) 기준 eraseDuration을 진행도에 비례해 축소 →
            // 취소 시점과 무관하게 "말려 사라지는 속도"가 항상 일정하게 유지된다.
            float scaledDuration = eraseDuration * startProgress;

            Tween tween = DOTween.To(
                    () => arrow.DrawProgress,
                    x => arrow.DrawProgress = x,
                    0f,
                    scaledDuration)
                .SetEase(eraseEase)
                .SetLink(arrow.gameObject)
                .OnComplete(() =>
                {
                    Destroy(arrow.gameObject);
                    activeArrows.Remove(selectId);
                    activeTweens.Remove(selectId);
                });

            activeTweens[selectId] = tween;
        }

        // 지목 단계 종료 등, 연출 없이 전부 즉시 정리할 때
        public void ClearAllArrows()
        {
            List<string> keys = new List<string>(activeArrows.Keys);
            foreach (string key in keys) KillArrowInstant(key);
        }

        // 연출 없이 즉시 제거 (지목 교체 / 전체 정리용 내부 헬퍼)
        private void KillArrowInstant(string selectId)
        {
            KillTrackedTween(selectId);

            if (activeArrows.TryGetValue(selectId, out PointArrow arrow))
            {
                Destroy(arrow.gameObject);
                activeArrows.Remove(selectId);
            }
        }

        // 추적 중인 트윈만 안전하게 Kill하고 딕셔너리에서 제거 (오브젝트는 건드리지 않음)
        private void KillTrackedTween(string selectId)
        {
            if (activeTweens.TryGetValue(selectId, out Tween tween))
            {
                if (tween != null && tween.IsActive()) tween.Kill();
                activeTweens.Remove(selectId);
            }
        }

        #endregion
    }
}