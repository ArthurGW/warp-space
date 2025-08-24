using System;
using UnityEngine;
using UnityEngine.InputSystem;

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

        public float health = 100f;

        private void Awake()
        {
            if (!InputSystem.actions) throw new Exception();
            _move = InputSystem.actions.FindAction("Player/Move");
            _look = InputSystem.actions.FindAction("Player/Look");
            _controller = GetComponent<CharacterController>();
            health = 100f;
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

        public void TakeDamage(float damage)
        {
            health -= damage;
            if (health <= 0f)
                PauseController.instance.IsPaused = true;
        }
    }
}
