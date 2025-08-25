using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cursor = UnityEngine.Cursor;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        private InputAction _move;
        private InputAction _look;
        
        public float movementSpeed;
        public float rotationSpeed;
        
        private CharacterController _controller;

        public uint maxHealth = 100u;

        public uint Health { get; private set; } = 100u;

        private void Awake()
        {
            if (!InputSystem.actions) throw new Exception();
            _move = InputSystem.actions.FindAction("Player/Move");
            _look = InputSystem.actions.FindAction("Player/Look");
            _controller = GetComponent<CharacterController>();
            Health = maxHealth;
            Cursor.lockState = CursorLockMode.Locked;
            
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

        private void TakeDamage(uint damage)
        {
            if (Health <= damage)
            {
                PauseController.instance.IsPaused = true;
				Health = 0u;
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
            TakeDamage((uint)count);
        }
    }
}
