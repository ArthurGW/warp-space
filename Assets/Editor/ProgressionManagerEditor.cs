using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Editor
{
    [CustomEditor(typeof(ProgressionManager))]
    public class ProgressionManagerEditor : UnityEditor.Editor
    {
        private Button _restartButton;

        public override VisualElement CreateInspectorGUI()
        {
            var customInspector = new VisualElement();
            InspectorElement.FillDefaultInspector(customInspector, new SerializedObject(target), this);
            _restartButton = new Button(((ProgressionManager)target).RestartGeneration)
            {
                text = "Restart Generation"
            };
            customInspector.Add(_restartButton);
            return customInspector;
        }
    }
}
