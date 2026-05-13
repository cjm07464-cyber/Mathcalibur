using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mathcalibur.Battle
{
    public static class BattleSceneBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrapExists()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "BattleScene") return;

            if (Object.FindAnyObjectByType<BattleSceneController>() != null) return;

            var bootstrap = new GameObject("BattleBootstrap");
            bootstrap.AddComponent<BattleSceneController>();
        }
    }
}
