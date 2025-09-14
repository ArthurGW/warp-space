using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class CameraFollowPlayer : MonoBehaviour
    {
        private Transform _mainCameraTransform;

        private InputAction _zoomAction;
        private InputAction _lookAction;

        public float offset = 50f;
        public float zoomSpeed = 4f;
        public float lookProportion = 0.5f;
        private float _lookAngle = 0.6f;
        public float lookSpeed = 0.1f;
        public float minAngle = 10f;
        public float maxAngle = 89f;  // Not 90 as then the y-rotation is undefined
        
        public bool MovementEnabled { get; set; }

        public void CopyParams(CameraFollowPlayer other)
        {
            offset = other.offset;
            zoomSpeed = other.zoomSpeed;
            lookProportion = other.lookProportion;
            _lookAngle = other._lookAngle;
            lookSpeed = other.lookSpeed;
        }

        private void Awake()
        {
            if (!InputSystem.actions) return;
            _zoomAction = InputSystem.actions.FindAction("Player/Zoom");
            _lookAction = InputSystem.actions.FindAction("Player/Look");
            _mainCameraTransform = Camera.main?.transform;
        }
        
        private void Update()
        {
            if (MovementEnabled)
            {
                if (_zoomAction.ReadValueAsObject() is float amount)
                {
                    offset = Mathf.Clamp(offset + amount * zoomSpeed * Time.deltaTime, 5, 250);
                }

                if (_lookAction.ReadValueAsObject() is Vector2 look)
                {
                    lookProportion = Mathf.Clamp(lookProportion - look.y * lookSpeed * Time.deltaTime, 0f, 1f);
                }
            }
            Follow();
        }

        private void Follow()
        {
            if (!gameObject.activeInHierarchy) return;
            
            _lookAngle = Mathf.LerpAngle(minAngle, maxAngle, lookProportion);
            var sin = MathF.Sin(_lookAngle * MathF.PI/180);
            var cos = MathF.Cos(_lookAngle * MathF.PI/180);
            var vertical = transform.up * (offset * sin);
            var horizontal = -transform.forward * (offset * cos);
            _mainCameraTransform.position = transform.position + vertical + horizontal;
            _mainCameraTransform.LookAt(transform);
        }
    }
}