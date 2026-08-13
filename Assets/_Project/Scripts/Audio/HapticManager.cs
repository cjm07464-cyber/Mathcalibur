using UnityEngine;

namespace Mathcalibur.Audio
{
    public sealed class HapticManager : MonoBehaviour
    {
        private const string VibrationEnabledPrefsKey = "Mathcalibur_VibrationEnabled";
        private const float LightCooldownSeconds = 0.06f;

        private static HapticManager _instance;
        private bool _isEnabled = true;
        private float _lastLightTime = -999f;

        public static HapticManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var managerObject = new GameObject(nameof(HapticManager));
                    _instance = managerObject.AddComponent<HapticManager>();
                }

                return _instance;
            }
        }

        public bool IsEnabled => _isEnabled;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            _isEnabled = PlayerPrefs.GetInt(VibrationEnabledPrefsKey, 1) != 0;
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            PlayerPrefs.SetInt(VibrationEnabledPrefsKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ToggleEnabled()
        {
            SetEnabled(!_isEnabled);
        }

        public void PlayLight()
        {
            if (Time.unscaledTime - _lastLightTime < LightCooldownSeconds)
            {
                return;
            }

            _lastLightTime = Time.unscaledTime;
            PlayAndroidHaptic(20, 50);
        }

        public void PlayMedium()
        {
            PlayAndroidHaptic(55, 120);
        }

        public void PlayHeavy()
        {
            PlayAndroidHaptic(90, 220);
        }

        private void PlayAndroidHaptic(long milliseconds, int amplitude)
        {
            if (!_isEnabled)
            {
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibrator == null || !vibrator.Call<bool>("hasVibrator"))
                {
                    return;
                }

                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                var sdkInt = version.GetStatic<int>("SDK_INT");
                if (sdkInt >= 26)
                {
                    using var vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");
                    var safeAmplitude = Mathf.Clamp(amplitude, 1, 255);
                    using var effect = vibrationEffect.CallStatic<AndroidJavaObject>(
                        "createOneShot",
                        milliseconds,
                        safeAmplitude);
                    vibrator.Call("vibrate", effect);
                    return;
                }

                vibrator.Call("vibrate", milliseconds);
            }
            catch
            {
                Handheld.Vibrate();
            }
#endif
        }
    }
}
