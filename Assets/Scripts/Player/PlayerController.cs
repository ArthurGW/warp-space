using System;
using System.Collections;
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
        private AudioListener _listener;
        
        public CameraFollowPlayer CameraFollow { get; private set; }

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
            CameraFollow = GetComponent<CameraFollowPlayer>();
            CameraFollow.MovementEnabled = true;
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
            get => _controller?.enabled ?? true;
            set
            {
                if (_controller != null) _controller.enabled = value;
                if (CameraFollow != null) CameraFollow.MovementEnabled = value;
            }
        }

        public void TeleportTo(Vector2 destination)
        {
            StartCoroutine(nameof(DoTeleport), destination);
        }
        
        public void TeleportTo(Vector2 destination, Quaternion orientation)
        {
            StartCoroutine(nameof(DoTeleport), (destination, orientation));
        }

        private IEnumerator DoTeleport(Vector2 destination)
        {
#if UNITY_EDITOR
            _controller ??= GetComponent<CharacterController>();
            CameraFollow ??= GetComponent<CameraFollowPlayer>();
            _listener ??= GetComponent<AudioListener>();
            
            playerDeath ??= new UnityEvent();
#endif
            EnableMovement = false;
            try
            {
                var posY = _controller.height / 2;
                transform.position = new Vector3(destination.x, posY, destination.y);
                yield return new WaitForSeconds(0.1f);
            }
            finally
            {
                EnableMovement = true;
            }
        }
        
        private IEnumerator DoTeleport((Vector2 destination, Quaternion orientation) dest)
        {
            EnableMovement = false;
            try
            {
                var posY = _controller.height / 2;
                transform.position = new Vector3(dest.destination.x, posY, dest.destination.y);
                transform.rotation = dest.orientation;
                CameraFollow.lookProportion = 1f;  // Look from above, to show the player where they are
                yield return new WaitForSeconds(0.1f);
            }
            finally
            {
                EnableMovement = true;
            }
        }

        public void Resurrect()
        {
            CameraFollow.enabled = true;
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

                CameraFollow.enabled = false;
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
                newFollow.CopyParams(CameraFollow);
                newFollow.MovementEnabled = true;
                
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
