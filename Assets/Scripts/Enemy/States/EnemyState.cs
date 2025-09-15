using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy.States
{
    // Based on https://learn.unity.com/course/artificial-intelligence-for-beginners/unit/finite-state-machines-1
    public abstract class EnemyState
    {
        protected readonly EnemyController Enemy;
        protected readonly Transform EnemyTransform;
        protected readonly NavMeshAgent EnemyAgent;
        protected readonly Transform PlayerTransform;

        private EnemyState _nextState;

        // Sensor range is in effect infinite, *but* is blocked by walls and doors
        private const float SensorRange = 10000f;

        private readonly int _detectionMask;

        private enum SubState
        {
            Entering,
            Iterating,
            Exiting
        }
        
        private SubState _subState;

        public void OverrideNextState(EnemyState newState)
        {
            _nextState = newState;
            _subState = SubState.Exiting;
        }

        /// <summary>
        /// The main external entry point for the state, should be called each frame
        /// </summary>
        /// <returns>A new state to change to, or null to stay in the same state</returns>
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

            return null;
        }

        public virtual void OnTriggerEnter(Collider other)
        {
        }

        public virtual void OnTriggerStay(Collider other)
        {
        }

        protected EnemyState(EnemyController enemy, Transform playerTransform)
        {
            _nextState = null;
            _subState = SubState.Entering;
            
            // Detect everything except other enemies
            _detectionMask = ~LayerMask.GetMask("Enemy");
            
            Enemy = enemy;
            EnemyTransform = enemy.transform;
            EnemyAgent = enemy.GetComponent<NavMeshAgent>();
            PlayerTransform = playerTransform;
        }
        
        protected bool IsAtDestination => !EnemyAgent.pathPending && EnemyAgent.hasPath
                                && EnemyAgent.remainingDistance <= EnemyAgent.stoppingDistance * 1.2f;
        
        protected int FindClosestPoint(Vector3[] points)
        {
            return points
                .Select((pt, ind) => (Vector3.Distance(pt, EnemyTransform.position), ind))
                .Aggregate(((float dist, int ind) first, (float dist, int ind) second) => first.dist <= second.dist ? first : second
                ).Item2;
        }

        protected bool CanDetectPlayer
        {
            get
            {
                var directionToPlayer = (PlayerTransform.position - EnemyTransform.position).normalized;
                if (
                    !Physics.Raycast(
                        EnemyTransform.position,
                        directionToPlayer,
                        out var hit,
                        SensorRange,
                        _detectionMask,
                        QueryTriggerInteraction.Ignore
                    )
                )
                    return false;
                return hit.transform == PlayerTransform;
            }
        }
        
        protected abstract void Enter();
        protected abstract EnemyState DoIteration();
        protected abstract void Exit();
    }
}
