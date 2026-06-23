using UnityEngine;
using System.Collections;

namespace Mathcalibur.Audio
{
    public class GameAudioManager : MonoBehaviour
    {
        private const float GeneralTouchButtonSuppressSeconds = 2f;

        public static GameAudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource uiSfxSource;

        [Header("BGM")]
        [SerializeField] private AudioClip titleBgm;
        [SerializeField] private AudioClip battleBgm;

        [Header("UI SFX")]
        [SerializeField] private AudioClip buttonClickSfx;
        [SerializeField] private AudioClip tileSelectSfx;
        [SerializeField] private AudioClip expressionConfirmSfx;
        [SerializeField] private AudioClip invalidSelectionSfx;
        [SerializeField] private AudioClip generalTouchSfx;
        [SerializeField] private AudioClip dragTouchSfx;
        [SerializeField] private AudioClip releaseTouchSfx;
        [SerializeField] private AudioClip stageVictorySfx;
        [SerializeField] private AudioClip defeatSfx;
        [SerializeField] private AudioClip combatModeSwitchSfx;

        [SerializeField, Range(0f, 1f)] private float generalTouchVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float dragTouchVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float releaseTouchVolume = 0.7f;
        private float _defaultMusicVolume = 1f;
        private float _lastGeneralTouchSfxTime = -999f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSources();
            _defaultMusicVolume = musicSource != null ? musicSource.volume : 1f;
        }

        public void PlayTitleBgm() => PlayMusic(titleBgm);
        public void PlayBattleBgm() => PlayMusic(battleBgm);
        public void PlayButtonClickSfx()
        {
            if (generalTouchSfx != null)
            {
                if (Time.unscaledTime - _lastGeneralTouchSfxTime > GeneralTouchButtonSuppressSeconds)
                {
                    PlayGeneralTouchSfx();
                }

                return;
            }

            PlayUiSfx(buttonClickSfx);
        }

        public void PlayTileSelectSfx() => PlayUiSfx(tileSelectSfx);
        public void PlayExpressionConfirmSfx() => PlayUiSfx(expressionConfirmSfx);
        public void PlayInvalidSelectionSfx() => PlayUiSfx(invalidSelectionSfx);
        public void PlayGeneralTouchSfx()
        {
            if (generalTouchSfx == null)
            {
                return;
            }

            _lastGeneralTouchSfxTime = Time.unscaledTime;
            PlayUiSfx(generalTouchSfx);
        }

        public void PlayDragTouchSfx() => PlayUiSfx(dragTouchSfx);
        public void PlayReleaseTouchSfx() => PlayUiSfx(releaseTouchSfx);
        public void PlayStageVictorySfx() => PlayUiSfx(stageVictorySfx);
        public void PlayDefeatSfx() => PlayUiSfx(defeatSfx);
        public void PlayCombatModeSwitchSfx() => PlayUiSfx(combatModeSwitchSfx);

        public float MusicVolume
        {
            get
            {
                EnsureSources();
                return musicSource != null ? musicSource.volume : _defaultMusicVolume;
            }
            set
            {
                EnsureSources();
                _defaultMusicVolume = Mathf.Clamp01(value);
                if (musicSource != null)
                {
                    musicSource.volume = _defaultMusicVolume;
                }
            }
        }

        public void ResetMusicVolume()
        {
            EnsureSources();
            if (musicSource != null)
            {
                musicSource.volume = _defaultMusicVolume;
            }
        }

        public IEnumerator FadeOutMusic(float duration)
        {
            EnsureSources();
            if (musicSource == null || musicSource.clip == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                StopMusic();
                yield break;
            }

            var startVolume = musicSource.volume;
            var elapsed = 0f;
            while (elapsed < duration && musicSource != null)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            StopMusic();
            if (musicSource != null)
            {
                musicSource.volume = _defaultMusicVolume;
            }
        }

        public void StopMusic()
        {
            EnsureSources();
            if (musicSource == null)
            {
                return;
            }

            musicSource.Stop();
            musicSource.clip = null;
        }

        private void PlayMusic(AudioClip clip)
        {
            EnsureSources();
            if (clip == null || musicSource == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.volume = _defaultMusicVolume;
            musicSource.Play();
        }

        private void PlayUiSfx(AudioClip clip)
        {
            EnsureSources();
            if (clip == null || uiSfxSource == null) return;
            uiSfxSource.PlayOneShot(clip);
        }

        private void EnsureSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }
            if (uiSfxSource == null)
            {
                uiSfxSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }
}
