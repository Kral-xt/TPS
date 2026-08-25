using UnityEngine;

namespace TPS.Player.Presentation
{
    /// <summary>玩家动画展示器，处理攀爬动画的视觉补偿与状态管理</summary>
    public sealed class PlayerAnimatorPresenter
    {
        private readonly Animator animator;
        private readonly Transform animatorTransform;
        private readonly Transform hips;
        private readonly Vector3 animatorBaseLocalPosition;
        private Vector3 climbHipsOffset;
        private bool climbVisualCompensationActive;

        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DyingHash = Animator.StringToHash("Dying");
        private bool isDead;


        public PlayerAnimatorPresenter(Animator animator)
        {
            this.animator = animator;
            if (animator == null)
            {
                return;
            }

            animatorTransform = animator.transform;
            animatorBaseLocalPosition = animatorTransform.localPosition;
            if (animator.isHuman)
            {
                hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            }
        }

        /// <summary>动画器是否可用</summary>
        public bool IsAvailable => animator != null;

        /// <summary>是否已进入死亡状态</summary>
        public bool IsDead => isDead;

        /// <summary>播放受击动画，死亡状态下不播放</summary>
        public void PlayHitAnimation()
        {
            if (animator == null || isDead) return;
            animator.SetTrigger(HitHash);
        }

        /// <summary>播放死亡动画，仅能触发一次</summary>
        public void PlayDeathAnimation()
        {
            if (animator == null || isDead) return;
            isDead = true;
            animator.SetTrigger(DyingHash);
        }

        /// <summary>进入攀爬状态，记录骨骼偏移并激活攀爬层</summary>
        public void EnterClimb(
            Transform movementRoot,
            int climbLayer,
            int climbStateHash,
            int jumpHash,
            int rollHash)
        {
            if (animator == null) return;

            animatorTransform.localPosition = animatorBaseLocalPosition;
            if (movementRoot != null && hips != null)
            {
                climbHipsOffset = hips.position - movementRoot.position;
                climbVisualCompensationActive = true;
            }

            animator.SetBool(jumpHash, false);
            animator.SetBool(rollHash, false);
            animator.SetBool(climbStateHash, true);
            if (climbLayer >= 0 && climbLayer < animator.layerCount)
            {
                animator.SetLayerWeight(climbLayer, 1f);
            }
        }

        /// <summary>攀爬动画播放后释放 Bool 参数</summary>
        public void ReleaseClimbRequestWhenEntered(int climbLayer, int climbStateHash)
        {
            if (animator == null || climbLayer < 0 || climbLayer >= animator.layerCount)
                return;

            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(climbLayer);
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(climbLayer);
            if (currentState.shortNameHash == climbStateHash || nextState.shortNameHash == climbStateHash)
                animator.SetBool(climbStateHash, false);
        }


        /// <summary>退出攀爬状态，恢复动画器位置</summary>
        public void ExitClimb(int climbStateHash)
        {
            if (animator == null) return;

            animator.SetBool(climbStateHash, false);
            climbVisualCompensationActive = false;
            animatorTransform.localPosition = animatorBaseLocalPosition;
        }

        /// <summary>攀爬过程中稳定视觉位置，补偿骨骼偏移</summary>
        public void StabilizeClimbVisual(Transform movementRoot)
        {
            if (!climbVisualCompensationActive
                || movementRoot == null
                || animatorTransform == null
                || hips == null)
            {
                return;
            }

            animatorTransform.localPosition = animatorBaseLocalPosition;
            Vector3 visualOffset = (hips.position - movementRoot.position) - climbHipsOffset;
            animatorTransform.position -= Vector3.up * visualOffset.y;
        }

    }
}
