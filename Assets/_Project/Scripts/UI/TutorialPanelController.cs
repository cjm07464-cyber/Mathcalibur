using System.Collections.Generic;
using System.Linq;
using Mathcalibur.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Mathcalibur.UI
{
    public sealed class TutorialPanelController : MonoBehaviour
    {
        [Header("튜토리얼 패널")]
        [Tooltip("튜토리얼 전체를 감싸는 메인 패널입니다. 없으면 이 오브젝트를 패널로 사용합니다.")]
        [SerializeField] private GameObject panelRoot;

        [Tooltip("0번 인덱스부터 순서대로 보여줄 이미지입니다. 비워두면 panelRoot의 직계 자식 Image들을 위에서 아래 순서대로 자동 사용합니다.")]
        [SerializeField] private Image[] pages;

        private readonly List<Image> _resolvedPages = new();
        private int _currentIndex;
        private int _openedStartIndex;
        private int _openedEndIndex;
        private int _openedFrame = -1;
        private int _closedFrame = -1;
        private bool _isOpen;

        public bool IsOpen => _isOpen;
        public bool ClosedThisFrame => Time.frameCount == _closedFrame;

        private void Awake()
        {
            ResolvePages();
            Close();
        }

        private void OnEnable()
        {
            ResolvePages();
            RefreshPageVisibility();
        }

        private void Update()
        {
            if (!_isOpen || Time.frameCount <= _openedFrame)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0) || HasTouchBegan())
            {
                ShowNextPageOrClose();
            }
        }

        public void Open()
        {
            OpenRange(0, int.MaxValue);
        }

        public void OpenPage(int pageIndex)
        {
            OpenRange(pageIndex, pageIndex);
        }

        public void OpenRange(int startIndex, int endIndex)
        {
            ResolvePages();
            if (_resolvedPages.Count == 0)
            {
                Close();
                return;
            }

            var requestedStartIndex = Mathf.Min(startIndex, endIndex);
            var requestedEndIndex = Mathf.Max(startIndex, endIndex);
            if (requestedEndIndex < 0 || requestedStartIndex >= _resolvedPages.Count)
            {
                Close();
                return;
            }

            var maxIndex = _resolvedPages.Count - 1;
            _openedStartIndex = Mathf.Clamp(requestedStartIndex, 0, maxIndex);
            _openedEndIndex = Mathf.Clamp(requestedEndIndex, 0, maxIndex);
            _currentIndex = FindNextAvailablePageIndex(_openedStartIndex, _openedEndIndex);
            if (_currentIndex < 0)
            {
                Close();
                return;
            }

            _openedFrame = Time.frameCount;
            _isOpen = true;
            SetPanelActive(true);
            ResolvePanelRoot()?.transform.SetAsLastSibling();
            RefreshPageVisibility();
        }

        public void Close()
        {
            if (_isOpen)
            {
                _closedFrame = Time.frameCount;
            }

            _isOpen = false;
            _currentIndex = 0;
            RefreshPageVisibility();
            SetPanelActive(false);
        }

        public void ShowNextPageOrClose()
        {
            if (!_isOpen)
            {
                return;
            }

            GameAudioManager.Instance?.PlayButtonClickSfx();
            _currentIndex = FindNextAvailablePageIndex(_currentIndex + 1, _openedEndIndex);
            if (_currentIndex < 0)
            {
                Close();
                return;
            }

            RefreshPageVisibility();
        }

        private void ResolvePages()
        {
            _resolvedPages.Clear();
            if (pages != null)
            {
                _resolvedPages.AddRange(pages);
                if (_resolvedPages.Any(page => page != null))
                {
                    return;
                }

                _resolvedPages.Clear();
            }

            var root = ResolvePanelRoot();
            if (root == null)
            {
                return;
            }

            for (var i = 0; i < root.transform.childCount; i++)
            {
                if (root.transform.GetChild(i).TryGetComponent<Image>(out var page))
                {
                    _resolvedPages.Add(page);
                }
            }
        }

        private int FindNextAvailablePageIndex(int startIndex, int endIndex)
        {
            for (var i = Mathf.Max(0, startIndex); i <= endIndex && i < _resolvedPages.Count; i++)
            {
                if (_resolvedPages[i] != null)
                {
                    return i;
                }
            }

            return -1;
        }

        private void RefreshPageVisibility()
        {
            for (var i = 0; i < _resolvedPages.Count; i++)
            {
                if (_resolvedPages[i] != null)
                {
                    _resolvedPages[i].gameObject.SetActive(_isOpen && i == _currentIndex);
                }
            }
        }

        private GameObject ResolvePanelRoot()
        {
            return panelRoot != null ? panelRoot : gameObject;
        }

        private void SetPanelActive(bool active)
        {
            var root = ResolvePanelRoot();
            if (root != null)
            {
                root.SetActive(active);
            }
        }

        private static bool HasTouchBegan()
        {
            for (var i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Began)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
