using UnityEngine;

public class StandaloneResolutionSetter : MonoBehaviour
{
    private void Awake()
    {
#if UNITY_STANDALONE
        Screen.SetResolution(540, 960, false);
#endif
    }
}