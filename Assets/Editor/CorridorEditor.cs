using Layout;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Editor
{
    [CustomEditor(typeof(Corridor))]
    public class CorridorEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement customInspector = new VisualElement();
            InspectorElement.FillDefaultInspector(customInspector, new SerializedObject(target), this);
            var button = new Button(UpdateEntrances);
            button.text = "Update Entrances";
            customInspector.Add(button);
            return customInspector;
        }

        private void UpdateEntrances()
        {
            ((Corridor)target).UpdateEntrances();
        } 
    }
}