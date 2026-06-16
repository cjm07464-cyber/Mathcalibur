using UnityEngine;

namespace Mathcalibur.Audio
{
    public class GameAudioManager : MonoBehaviour
    {
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
        }

        public void PlayTitleBgm() => PlayMusic(titleBgm);
        public void PlayBattleBgm() => PlayMusic(battleBgm);
        public void PlayButtonClickSfx() => PlayUiSfx(buttonClickSfx);
        public void PlayTileSelectSfx() => PlayUiSfx(tileSelectSfx);
        public void PlayExpressionConfirmSfx() => PlayUiSfx(expressionConfirmSfx);
        public void PlayInvalidSelectionSfx() => PlayUiSfx(invalidSelectionSfx);

        private void PlayMusic(AudioClip clip)
        {
            EnsureSources();
            if (clip == null || musicSource == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;
            musicSource.clip = clip;
            musicSource.loop = true;
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
