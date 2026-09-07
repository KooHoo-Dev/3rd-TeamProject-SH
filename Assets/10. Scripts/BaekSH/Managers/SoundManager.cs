using System.Collections.Generic;
using Jay;
using UnityEngine;
using UnityEngine.Serialization;

namespace TeamSH
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance;
        
        // 추후 추가로 필요할 때 Enum 추가
        public enum GameBGM
        {
            Title,                  // 타이틀 BGM 0
            Lobby,                  // 로비 BGM 1 + 마트 진입 전까지 회의실 브금
            LiarGameBasic,          // LiarGame 기본 BGM 2
            LiarGameVoteOrPoint,    // LiarGame 지목/투표시 BGM 3
            Mart,                   // 마트 기본 BGM 4
            Victory,                // 승리시 BGM 5
            Defeat,                 // 패배시 BGM 6
        }

        public enum GameSfx
        {
            GetGenreKeyword,        // 장르, 키워드 받을 때 Sfx 0
            VoteButtonClick,        // 투표 버튼 클릭 Sfx 1
            PointAtPlayer,          // 지목시 Sfx 2
            PlayerPush,             // 밀칠 때 Sfx 3
            PlayerPushed,           // 밀쳐질 때 Sfx 4
            PopUpScreenOpened,      // 마트 아이템 팝업창 Open시 Sfx 5
            PopUpScreenClosed,      // 마트 아이템 팝업창 Close시 Sfx 6
            ItemPickUp,             // 아이템 집을시 Sfx 7
            ItemPutDown,            // 아이템 내려놓을시 Sfx 8
            LiarOutSuccess,         // 라밍아웃 버튼으로 라이어가 드러날 때 Sfx 9
            ScoreTally,             // 점수 집계시 Sfx 10
            QuestClear,             // 퀘스트 클리어시 Sfx 11
            QuestFail,              // 퀘스트 클리어 실패시 Sfx 12
            LiarRevealed,           // 라이어 밝혀질시 Sfx 13
            LiarConfirmedFail,      // 라이어가 밝혀지는데 일반 플레이어가 못잡았을시 Sfx 14
        }

        [Header("AudioSource")]
        [SerializeField] private AudioSource audioSource;

        [Header("AudioClip")]
        [SerializeField] private AudioClip[] bgmClips;
        [SerializeField] private AudioClip[] sfxClips;

        private Dictionary<GameBGM, AudioClip> bgmDict;
        private Dictionary<GameSfx, AudioClip> sfxDict;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else Destroy(gameObject);
            
            InitDictionaries();
        }
        
        // 딕셔너리 초기화
        private void InitDictionaries()
        {
            bgmDict = new Dictionary<GameBGM, AudioClip>();
            for (int i = 0; i < bgmClips.Length; i++)
            {
                bgmDict[(GameBGM)i] = bgmClips[i];
            }
            
            sfxDict = new Dictionary<GameSfx, AudioClip>();
            for (int i = 0; i < sfxClips.Length; i++)
            {
                sfxDict[(GameSfx)i] = sfxClips[i];
            }
        }
        
        // BGM 재생
        public void PlayBGM(GameBGM bgm)
        {
            if (bgmDict.TryGetValue(bgm, out var clip))
            {
                audioSource.clip = clip;
                audioSource.loop = true;
                audioSource.Play();
            }
            else
            {
                Debug.LogWarning("BGM not found");
            }
        }

        public void PlaySfx(GameSfx sfx)
        {
            if (sfxDict.TryGetValue(sfx, out var clip))
            {
                audioSource.PlayOneShot(clip);
            }
            else
            {
                Debug.LogWarning("SFX not found");
            }
        }

    }
}