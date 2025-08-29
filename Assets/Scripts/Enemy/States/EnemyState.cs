using UnityEngine;
using UnityEngine.AI;

namespace Enemy.States
{
    // Based on https://learn.unity.com/course/artificial-intelligence-for-beginners/unit/finite-state-machines-1
    public abstract class EnemyState
    {
        protected readonly Transform Enemy;
        protected readonly NavMeshAgent EnemyAgent;
        protected readonly Transform Player;

        private EnemyState _nextState;

        private enum SubState
        {
            Entering,
            Iterating,
            Exiting
        }
        
        private SubState _subState;

        public EnemyState Update()
        {
            switch (_subState)
            {
                case SubState.Entering:
                    Enter();
                    _subState = SubState.Iterating;
                    break;
                case SubState.Iterating:
                    var newState = DoIteration();
                    if (newState != null)
                    {
                        _subState = SubState.Exiting;
                        _nextState  = newState;
                    }
                    break;
                case SubState.Exiting:
                    Exit();
                    return _nextState;
                default:
                    break;
            }

            return this;
        }

        protected EnemyState(Transform enemy, NavMeshAgent enemyAgent, Transform player)
        {
            _nextState = null;
            _subState = SubState.Entering;

            Enemy = enemy;
            EnemyAgent = enemyAgent;
            Player = player;
        }
        
        protected abstract void Enter();
        protected abstract EnemyState DoIteration();
        protected abstract void Exit();
    }
}