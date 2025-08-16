using MapObjects;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Editor
{
    [CustomEditor(typeof(GameMapController))]
    public class GameMapControllerEditor : UnityEditor.Editor
    {
        private Button _clearMapButton;
        
        public override VisualElement CreateInspectorGUI()
        {
            var customInspector = new VisualElement();
            InspectorElement.FillDefaultInspector(customInspector, new SerializedObject(target), this);
            _clearMapButton = new Button(ClearMap)
            {
                text = "Clear Map"
            };
            customInspector.Add(_clearMapButton);
            return customInspector;
        }

        private void ClearMap()
        {
            ((GameMapController)target).DestroyMap();
        }
    }
}