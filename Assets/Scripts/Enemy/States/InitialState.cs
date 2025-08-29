using UnityEngine;
using UnityEngine.AI;

namespace Enemy.States
{
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
            return this;
        }

        protected override void Exit()
        {
            
        }
    }
}