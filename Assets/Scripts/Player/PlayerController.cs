using System;
using System.Collections.Generic;
using Enemy;
using Enemy.States;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Cursor = UnityEngine.Cursor;

namespace Player
{
    [RequireComponent(typeof(CharacterController), typeof(CameraFollowPlayer), typeof(AudioListener))]
    public class PlayerController : MonoBehaviour
    {
        private InputAction _move;
        private InputAction _look;
        
        public float movementSpeed;
        public float rotationSpeed;
        
        private CharacterController _controller;
        private CameraFollowPlayer _cameraFollow;
        private AudioListener _listener;

        public uint maxHealth = 100u;

        public uint particleDamage = 2u;

        public uint Health { get; private set; } = 100u;
        
        public UnityEvent playerDeath;

        private void Awake()
        {
            if (!InputSystem.actions) throw new Exception();
            _move = InputSystem.actions.FindAction("Player/Move");
            _look = InputSystem.actions.FindAction("Player/Look");
            _controller = GetComponent<CharacterController>();
            _cameraFollow = GetComponent<CameraFollowPlayer>();
            _listener = GetComponent<AudioListener>();
            
            playerDeath ??= new UnityEvent();
            
            Resurrect();
        }

        private void Update()
        {
            if (PauseController.instance.IsPaused || !EnableMovement) return;
            
            // Handle x-axis of look as player rotation
            var lookAmount = _look.ReadValue<Vector2>().x * (rotationSpeed * Time.deltaTime);
            var lookInPlane = Quaternion.AngleAxis(lookAmount, Vector3.up);
            transform.rotation *= lookInPlane;
            
            // Move player
            var moveAmount = _move.ReadValue<Vector2>() * (Time.deltaTime * movementSpeed);
            var moveInPlane = transform.forward * moveAmount.y + transform.right * moveAmount.x;
            _controller.Move(moveInPlane);
        }

        public bool EnableMovement
        {
            get => _controller.enabled;
            set => _controller.enabled = value;
        }

        public void Resurrect()
        {
            _cameraFollow.enabled = true;
            _listener.enabled = true;
            EnableMovement = true;
            Health = maxHealth;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void TakeDamage(uint damage)
        {
            if (Health > 0u && Health <= damage)
            {
                // Dead! Player turns into an enemy
                Health = 0u;
                
                var enemy = FindAnyObjectByType<EnemyController>();

                _cameraFollow.enabled = false;
                _listener.enabled = false;
                EnableMovement = false;
                var previousPos = transform.position;
                var previousRotation = transform.rotation;
                transform.Translate(1000f, 0f, 0f);  // Translate far offscreen to hide from enemy tracking

                // Make a fake player-turned-to-enemy for the camera to follow
                var fakePlayer = Instantiate(enemy, previousPos, previousRotation, null);
                fakePlayer.name = "FakePlayer";
                fakePlayer.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                fakePlayer.GetComponent<NavMeshAgent>().SetDestination(previousPos);
                fakePlayer.SetState(new PursueState(fakePlayer.transform, fakePlayer.GetComponent<NavMeshAgent>(), transform));
                fakePlayer.AddComponent<AudioListener>();
                var newFollow = fakePlayer.AddComponent<CameraFollowPlayer>();
                newFollow.CopyParams(_cameraFollow);
                
                Cursor.lockState = CursorLockMode.None;
                playerDeath.Invoke();
            }
			else
				Health -= damage;
        }

        private void OnParticleCollision(GameObject other)
        {
            if (!other.CompareTag("Enemy")) return;
            
            var system = other.GetComponentInChildren<ParticleSystem>();

			// GetSafeCollisionEventSize is only guaranteed to be >= the number of collisions, so we also fetch them
            List<ParticleCollisionEvent> collisions = new(system.GetSafeCollisionEventSize());
            var count = system.GetCollisionEvents(gameObject, collisions);
            TakeDamage((uint)count * particleDamage);
        }
    }
}
