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
        private readonly CharacterController _playerController;
        
        private readonly AudioSource _audioSource;
        private readonly ParticleSystem _particleSystem;

        private readonly AudioClip _shockSound;
        private readonly AudioClip _lockOnSound;

        private const uint LookAheadFrames = 5u;  // How far ahead to anticipate the player's movement
        private const uint UpdateFrames = 5u;  // How often to update the target calculation
        private uint _frame;

        private float _previousAgentSpeed;
        
        // Weapon cooldown
        private readonly float _coolDownTime;
        private float _coolDown;
        
        public PursueState(EnemyController enemy, Transform player) : base(enemy, player)
        {
            _playerController = player.GetComponent<CharacterController>();
            _audioSource = enemy.GetComponent<AudioSource>();
            _particleSystem = enemy.GetComponent<ParticleSystem>();

            _coolDownTime = enemy.coolDownTime;
            _coolDown = 0f;

            _shockSound = enemy.shockSound;
            _lockOnSound = enemy.lockOnSound;
        }
        
        protected override void Enter()
        {
            _audioSource.PlayOneShot(_lockOnSound);
            _frame = 0u;
            _previousAgentSpeed = EnemyAgent.speed;
            EnemyAgent.speed *= 2f;
            UpdateDestination();
        }

        private void UpdateDestination()
        {
            if (_frame != 0u) return;
            if (!NavMesh.SamplePosition(PlayerTransform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                return;  // Player is not on the nav mesh
            
            var targetPos = PlayerTransform.position + _playerController.velocity * LookAheadFrames;
            if (NavMesh.Raycast(EnemyTransform.position, targetPos, out _, NavMesh.AllAreas))
            {
                // targetPos is not on the nav mesh, so use the player position instead
                targetPos = PlayerTransform.position;
            }
            
            EnemyAgent.SetDestination(targetPos);
        }

        protected override EnemyState DoIteration()
        {
            if (_coolDown > 0f)
                _coolDown -= Time.deltaTime;
            
            if (!CanDetectPlayer)
            {
                // Continue towards the previous destination before exiting the state, in case this makes the player
                // become detectable again
                if (!IsAtDestination) return null;
                
                if (RoomController.GetRoomDataForPosition(EnemyTransform.position).HasValue)
                    // Patrol current room
                    return new PatrolState(Enemy, PlayerTransform);
                
                // Go to the closest room
                return new ChangeRoomState(Enemy, PlayerTransform, true);
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
    
        private void Fire()
        {
            _audioSource.PlayOneShot(_shockSound);
            _particleSystem.Play();
            _coolDown = _coolDownTime;
        }

        public override void OnTriggerEnter(Collider other)
        {
            OnTriggerStay(other);  // Same logic
        }

        public override void OnTriggerStay(Collider other)
        {
            if (_coolDown > 0f || other.gameObject != PlayerTransform.gameObject) return;
                
            Fire();
        }
    }
}