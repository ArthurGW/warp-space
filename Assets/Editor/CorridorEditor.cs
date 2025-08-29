using Layout;
using MapObjects;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Editor
{
    [CustomEditor(typeof(CorridorController))]
    public class CorridorEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var customInspector = new VisualElement();
            InspectorElement.FillDefaultInspector(customInspector, new SerializedObject(target), this);
            var button = new Button(UpdateEntrances)
            {
                text = "Update Entrances"
            };
            customInspector.Add(button);
            return customInspector;
        }

        private void UpdateEntrances()
        {
            ((CorridorController)target).UpdateCorridor();
        } 
    }
}