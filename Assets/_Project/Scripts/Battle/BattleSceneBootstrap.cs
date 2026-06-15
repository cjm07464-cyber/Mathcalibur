using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mathcalibur.Battle
{
    public static class BattleSceneBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureBootstrapExists(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureBootstrapExists(scene);
        }

        private static void EnsureBootstrapExists(Scene scene)
        {
            
            if (scene.name != "BattleScene") return;

            if (Object.FindAnyObjectByType<BattleSceneController>() != null) return;

            var bootstrap = new GameObject("BattleBootstrap");
            bootstrap.AddComponent<BattleSceneController>();
        }
    }
}
