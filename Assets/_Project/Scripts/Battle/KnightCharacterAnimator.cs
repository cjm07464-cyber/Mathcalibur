using UnityEngine;

namespace Mathcalibur.Battle
{
    public class KnightCharacterAnimator : MonoBehaviour
    {
        private static readonly int AttackTrigger = Animator.StringToHash("Attack");

        [SerializeField] private GameObject modelPrefab;
        [SerializeField] private RuntimeAnimatorController animatorController;
        [SerializeField] private Animator animator;

        private GameObject _modelInstance;

        private void Awake()
        {
            EnsureModelInstance();
            EnsureAnimator();
        }

        public void PlayAttack()
        {
            EnsureAnimator();
            if (animator == null) return;

            animator.ResetTrigger(AttackTrigger);
            animator.SetTrigger(AttackTrigger);
        }

        private void EnsureModelInstance()
        {
            if (_modelInstance != null || modelPrefab == null) return;
            if (animator != null && animator.transform != transform) return;

            var childAnimator = GetComponentInChildren<Animator>();
            if (childAnimator != null)
            {
                animator = childAnimator;
                _modelInstance = childAnimator.gameObject;
                return;
            }

            _modelInstance = Instantiate(modelPrefab, transform);
            _modelInstance.name = modelPrefab.name;
            _modelInstance.transform.localPosition = Vector3.zero;
            _modelInstance.transform.localRotation = Quaternion.identity;
            _modelInstance.transform.localScale = Vector3.one;
        }

        private void EnsureAnimator()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null) return;

            if (animatorController != null)
                animator.runtimeAnimatorController = animatorController;

            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;
        }
    }
}
