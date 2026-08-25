using UnityEngine;

namespace TPS.Enemy.Presentation
{
    [DisallowMultipleComponent]
    public sealed class EnemyAnimationEventRelay : MonoBehaviour
    {
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private RangedZombieEnemyController rangedEnemyController;

        public void SetController(EnemyController controller)
        {
            enemyController = controller;
        }

        public void SetRangedController(RangedZombieEnemyController controller)
        {
            rangedEnemyController = controller;
        }

        private void Awake()
        {
            ResolveController();
        }

        private void OnValidate()
        {
            ResolveController();
        }

        public void DealAttackDamage()
        {
            ResolveController();
            if (enemyController != null)
            {
                enemyController.DealAttackDamage();
            }
            else
            {
                rangedEnemyController?.DealAttackDamage();
            }
        }

        private void ResolveController()
        {
            if (enemyController != null)
            {
                return;
            }

            enemyController = GetComponentInParent<EnemyController>(true);
            rangedEnemyController ??=
                GetComponentInParent<RangedZombieEnemyController>(true);
        }
    }
}
