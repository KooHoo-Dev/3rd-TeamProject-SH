using System.Collections.Generic;
using Jay;
using UnityEngine;

namespace TeamSH
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance;
        
        // 추후 추가로 필요할 때 Enum 추가
        public enum GameBGM
        {
            Lobby,                  // 로비 BGM
            LiarGameBasic,          // LiarGame 기본 BGM
            LiarGameVoteOrPoint,    // LiarGame 지목/투표시 BGM
            Mart,                   // 마트 기본 BGM
            Result                  // 결과 표시 BGM
        }

        public enum GameSfx
        {
            ButtonClick,            // 버튼 클릭 Sfx
            PointAtPlayer,          // 지목시 Sfx
            PlayerPush,             // 밀칠 때 Sfx
            PlayerPushed,           // 밀쳐질 때 Sfx
            BoxOpened,              // 가방 Open시 Sfx
            BoxClosed,              // 가방 Close시 Sfx
            PopUpScreenOpened,      // 팝업창 Open시 Sfx
            PopUpScreenClosed,      // 팝업창 Close시 Sfx
            ItemPickUp,             // 아이템 집을시 Sfx
            ItemPutDown,            // 아이템 내려놓을시 Sfx
        }

        [Header("AudioSource")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

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
                bgmSource.clip = clip;
                bgmSource.loop = true;
                bgmSource.Play();
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
                sfxSource.PlayOneShot(clip);
            }
            else
            {
                Debug.LogWarning("SFX not found");
            }
        }

    }
}