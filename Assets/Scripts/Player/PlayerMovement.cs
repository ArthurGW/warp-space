using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerMovement : MonoBehaviour
    {
        private InputAction _move;
        private InputAction _look;
        
        public float movementSpeed;
        public float rotationSpeed;

        private void Awake()
        {
            if (!InputSystem.actions) return;
            _move = InputSystem.actions.FindAction("Player/Move");
            _look = InputSystem.actions.FindAction("Player/Look");
        }

        private void Update()
        {
            var moveAmount = _move.ReadValue<Vector2>() * (Time.deltaTime * movementSpeed);
            var lookAmount = _look.ReadValue<Vector2>() * (Time.deltaTime * rotationSpeed);;
            
            var moveInPlane = transform.forward * moveAmount.y + transform.right * moveAmount.x;
            var lookInPlane = Quaternion.AngleAxis(lookAmount.x, Vector3.up);
            transform.position += moveInPlane;
            transform.rotation *= lookInPlane;
            
            // transform.up = Vector3.up;
        }
    }
}