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
            ResolvePages();
            if (_resolvedPages.Count == 0)
            {
                Close();
                return;
            }

            _currentIndex = 0;
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
            _currentIndex++;
            if (_currentIndex >= _resolvedPages.Count)
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
                _resolvedPages.AddRange(pages.Where(page => page != null));
            }

            if (_resolvedPages.Count > 0)
            {
                return;
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
