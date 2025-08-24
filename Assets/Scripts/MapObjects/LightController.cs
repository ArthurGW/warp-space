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
#if UNITY_EDITOR
            _entryDetector = GetComponent<BoxCollider>();
            _lights = GetComponentsInChildren<Light>();
#endif
            _entryDetector.isTrigger = true;
            
            if (data.HasValue)
            {
                var roomData = data.Value;
                _entryDetector.size = roomData.ToSize();
                _entryDetector.center = roomData.ToLocalCenter();
            }
            
            // Turn off the lights - the entry detector will turn them back on
            _lights = GetComponentsInChildren<Light>();
            foreach (var child in _lights)
            {
                child.enabled = false;
            }
        }

        public void TurnOnLights()
        {
            // Player has entered room, turn on the lights
            _entryDetector.enabled = false;  // This is a one-time operation, no need to keep detecting
            Destroy(_entryDetector);
            _entryDetector = null;
            foreach (var child in _lights)
            {
                child.enabled = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.CompareTag("Player")) return;

            TurnOnLights();
        }
    }
}
