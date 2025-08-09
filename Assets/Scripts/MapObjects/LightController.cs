using System;
using System.Collections.Generic;
using System.Linq;
using Layout;
using static Layout.LayoutUtils;
using UnityEngine;

namespace MapObjects
{
    [RequireComponent(typeof(BoxCollider))]
    public class LightController : MonoBehaviour
    {
        private BoxCollider _entryDetector;
        private Light[]  _lights;

        private void Awake()
        {
            _entryDetector = GetComponent<BoxCollider>();
            _lights = GetComponentsInChildren<Light>();
        }

        public void SetUpLights(RoomData? data)
        {
            if (data.HasValue)
            {
                var roomData = data.Value;
                _entryDetector ??= GetComponent<BoxCollider>();

                var roomSize = GridToSize((roomData.Width, roomData.Height));
                roomSize.y = 5f; // Standard room height
                _entryDetector.size = roomSize;
                var roomCenter = GridToPosition(((roomData.Width - 1), (roomData.Height - 1))) / 2f;
                roomCenter.y = 2.5f;
                _entryDetector.center = roomCenter;
                _entryDetector.isTrigger = true;
            }
            
            // Turn off the lights - the entry detector will turn them back on
            _lights = GetComponentsInChildren<Light>();
            foreach (var child in _lights)
            {
                child.enabled = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.CompareTag("Player")) return;
            
            // Player has entered room, turn on the lights
            _entryDetector.enabled = false;  // This is a one-time operation, no need to keep detecting
            foreach (var child in _lights)
            {
                child.enabled = true;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            OnTriggerEnter(other);
        }
    }
}
