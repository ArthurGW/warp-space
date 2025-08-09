using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class CameraFollowPlayer : MonoBehaviour
    {
        private Transform _mainCameraTransform;
        private Transform _playerTransform;

        private InputAction _zoom;

        public float offset = 50f;
        public float zoomSpeed = 4f;

        private void Awake()
        {
            if (!InputSystem.actions) return;
            _zoom = InputSystem.actions.FindAction("Player/Zoom");
        }

        private void Start()
        {
            _mainCameraTransform = Camera.main?.transform;
            _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            
            Follow();
        }

        private void Update()
        {
            var zoom = _zoom.ReadValueAsObject();
            if (zoom is float amount)
            {
                offset = Mathf.Clamp(offset + amount * zoomSpeed, 5, 250);
            }
            Follow();
        }

        private void Follow()
        {
            var offsetDir = _playerTransform.up * 4 - _playerTransform.forward;
            offsetDir = offsetDir.normalized;
            _mainCameraTransform.position = _playerTransform.position + offsetDir * offset;
            _mainCameraTransform.LookAt(_playerTransform);
        }
    }
}