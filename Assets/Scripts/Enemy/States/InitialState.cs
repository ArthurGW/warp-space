using UnityEngine;

namespace Enemy.States
{
    /// <summary>
    /// This initial state just gets the enemy into the adjacent room to its breach
    /// </summary>
    public class InitialState : EnemyState
    {
        private const uint InitialMoveDist = 20u;
        
        public InitialState(EnemyController enemy, Transform player) : base(enemy, player)
        {
        }

        protected override void Enter()
        {
            EnemyAgent.SetDestination(EnemyTransform.position + EnemyTransform.forward * InitialMoveDist);
        }

        protected override EnemyState DoIteration()
        {
            return IsAtDestination
                ? new PatrolState(Enemy, PlayerTransform)
                : null;
        }
        
        protected override void Exit()
        {
        }
    }
}