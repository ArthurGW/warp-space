using Layout;
using MapObjects;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy.States
{
    /// <summary>
    /// In this state the enemy pursues the player whilst the player is detectable
    /// </summary>
    public class PursueState : EnemyState
    {
        private Vector3[] _patrolPoints;
        private int _currentPatrolPoint;
        private RoomData _patrolRoom;
        private readonly CharacterController _playerController;

        private const uint LookAheadFrames = 5u;  // How far ahead to anticipate the player's movement
        private const uint UpdateFrames = 5u;  // How often to update the target calculation
        private uint _frame;

        private float _previousAgentSpeed;
        
        public PursueState(Transform enemy, NavMeshAgent enemyAgent, Transform player) : base(enemy, enemyAgent, player)
        {
            _playerController = player.GetComponent<CharacterController>();
        }
        
        protected override void Enter()
        {
            _frame = 0u;
            _previousAgentSpeed = EnemyAgent.speed;
            EnemyAgent.speed *= 2f;
            UpdateDestination();
        }

        private void UpdateDestination()
        {
            if (_frame != 0u) return;
            if (!NavMesh.SamplePosition(Player.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                return;  // Player is not on the nav mesh
            
            var targetPos = Player.position + _playerController.velocity * LookAheadFrames;
            if (NavMesh.Raycast(Enemy.position, targetPos, out _, NavMesh.AllAreas))
            {
                // targetPos is not on the nav mesh, so use the player position instead
                targetPos = Player.position;
            }
            
            EnemyAgent.SetDestination(targetPos);
        }

        protected override EnemyState DoIteration()
        {
            if (!CanDetectPlayer)
            {
                // Continue towards the previous destination before exiting the state, in case this makes the player
                // become detectable again
                if (!IsAtDestination) return null;
                
                if (RoomController.GetRoomDataForPosition(Enemy.position).HasValue)
                    // Patrol current room
                    return new PatrolState(Enemy, EnemyAgent, Player);
                
                // Go to the closest room
                return new ChangeRoomState(Enemy, EnemyAgent, Player, true);
            }
            
            ++_frame;
            _frame %= UpdateFrames;
            UpdateDestination();
            return null;
        }

        protected override void Exit()
        {
            EnemyAgent.speed = _previousAgentSpeed;
        }
    }
}