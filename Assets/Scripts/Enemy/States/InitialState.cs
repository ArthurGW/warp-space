using UnityEngine;
using UnityEngine.AI;

namespace Enemy.States
{
    /// <summary>
    /// This initial state just gets the enemy into the adjacent room to its breach
    /// </summary>
    public class InitialState : EnemyState
    {
        private const uint InitialMoveDist = 20u;
        
        public InitialState(Transform enemy, NavMeshAgent enemyAgent, Transform player) : base(enemy, enemyAgent, player)
        {
        }

        protected override void Enter()
        {
            EnemyAgent.SetDestination(Enemy.position + Enemy.forward * InitialMoveDist);
        }

        protected override EnemyState DoIteration()
        {
            return IsAtDestination
                ? new PatrolState(Enemy, EnemyAgent, Player)
                : null;
        }
        
        protected override void Exit()
        {
        }
    }
}