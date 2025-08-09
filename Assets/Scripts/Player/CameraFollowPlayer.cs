using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class CameraFollowPlayer : MonoBehaviour
    {
        private Transform _mainCameraTransform;
        private Transform _playerTransform;

        private InputAction _zoomAction;
        private InputAction _lookAction;

        public float offset = 50f;
        public float zoomSpeed = 4f;
        public float lookAngle = 60f;
        public float lookSpeed = 4f;

        private void Awake()
        {
            if (!InputSystem.actions) return;
            _zoomAction = InputSystem.actions.FindAction("Player/Zoom");
            _lookAction = InputSystem.actions.FindAction("Player/Look");
        }

        private void Start()
        {
            _mainCameraTransform = Camera.main?.transform;
            _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            
            Follow();
        }

        private void Update()
        {
            if (_zoomAction.ReadValueAsObject() is float amount)
            {
                offset = Mathf.Clamp(offset + amount * zoomSpeed * Time.deltaTime, 5, 250);
            }
            
            if (_lookAction.ReadValueAsObject() is Vector2 look)
            {
                lookAngle = Mathf.Clamp(lookAngle + look.y * lookSpeed * Time.deltaTime, 30, 80);
            }
            Follow();
        }

        private void Follow()
        {
            // var offsetDir = _playerTransform.up * 4 - _playerTransform.forward;
            // offsetDir = offsetDir.normalized;
            var sin = MathF.Sin(lookAngle * MathF.PI/180);
            var cos = MathF.Cos(lookAngle * MathF.PI/180);
            var vertical = _playerTransform.up * (offset * sin);
            var horizontal = -_playerTransform.forward * (offset * cos);
            _mainCameraTransform.position = _playerTransform.position + vertical + horizontal;
            _mainCameraTransform.LookAt(_playerTransform);
        }
    }
}