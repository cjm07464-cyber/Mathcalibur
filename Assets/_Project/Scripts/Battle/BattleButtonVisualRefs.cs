using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mathcalibur.Battle
{
    [DisallowMultipleComponent]
    public class BattleButtonVisualRefs : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image contentImage;
        [SerializeField] private TMP_Text label;

        public Image BackgroundImage
        {
            get => backgroundImage;
            set => backgroundImage = value;
        }

        public Image ContentImage
        {
            get => contentImage;
            set => contentImage = value;
        }

        public TMP_Text Label
        {
            get => label;
            set => label = value;
        }
    }
}
