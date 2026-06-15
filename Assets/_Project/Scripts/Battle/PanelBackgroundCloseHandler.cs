using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Mathcalibur.Battle
{
    public class PanelBackgroundCloseHandler : MonoBehaviour, IPointerClickHandler
    {
        private UnityAction _closeAction;
        private readonly List<Transform> _ignoredRoots = new();

        public void Bind(UnityAction closeAction)
        {
            _closeAction = closeAction;
        }

        public void SetIgnoredRoots(IEnumerable<Transform> ignoredRoots)
        {
            _ignoredRoots.Clear();
            if (ignoredRoots == null)
            {
                return;
            }

            foreach (var ignoredRoot in ignoredRoots)
            {
                if (ignoredRoot != null && !_ignoredRoots.Contains(ignoredRoot))
                {
                    _ignoredRoots.Add(ignoredRoot);
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_closeAction == null)
            {
                return;
            }

            if (eventData == null)
            {
                return;
            }

            var raycastObject = eventData.pointerCurrentRaycast.gameObject;
            if (IsUnderIgnoredRoot(raycastObject?.transform) || IsUnderIgnoredRoot(eventData.pointerPress?.transform) || IsUnderIgnoredRoot(eventData.rawPointerPress?.transform))
            {
                return;
            }

            var pressedObject = eventData.pointerPress;
            var rawPressedObject = eventData.rawPointerPress;
            if (pressedObject != gameObject && rawPressedObject != gameObject)
            {
                return;
            }

            _closeAction.Invoke();
        }

        private bool IsUnderIgnoredRoot(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            foreach (var ignoredRoot in _ignoredRoots)
            {
                if (ignoredRoot != null && target.IsChildOf(ignoredRoot))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
