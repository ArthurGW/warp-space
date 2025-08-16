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
        public float lookProportion = 0.5f;
        private float _lookAngle = 0f;
        public float lookSpeed = 0.1f;

        private void Awake()
        {
            if (!InputSystem.actions) return;
            _zoomAction = InputSystem.actions.FindAction("Player/Zoom");
            _lookAction = InputSystem.actions.FindAction("Player/Look");
            _playerTransform = FindFirstObjectByType<CharacterController>(FindObjectsInactive.Include).transform;
            _mainCameraTransform = Camera.main?.transform;
        }
        
        private void Update()
        {
            if (_zoomAction.ReadValueAsObject() is float amount)
            {
                offset = Mathf.Clamp(offset + amount * zoomSpeed * Time.deltaTime, 5, 250);
            }
            
            if (_lookAction.ReadValueAsObject() is Vector2 look)
            {
                lookProportion = Mathf.Clamp(lookProportion - look.y * lookSpeed * Time.deltaTime, 0f, 1f);
                _lookAngle = Mathf.LerpAngle(10, 80, lookProportion);
            }
            
            Follow();
        }

        private void Follow()
        {
            if (!_playerTransform || !_playerTransform.gameObject.activeInHierarchy) return;
            
            var sin = MathF.Sin(_lookAngle * MathF.PI/180);
            var cos = MathF.Cos(_lookAngle * MathF.PI/180);
            var vertical = _playerTransform.up * (offset * sin);
            var horizontal = -_playerTransform.forward * (offset * cos);
            _mainCameraTransform.position = _playerTransform.position + vertical + horizontal;
            _mainCameraTransform.LookAt(_playerTransform);
        }
    }
}