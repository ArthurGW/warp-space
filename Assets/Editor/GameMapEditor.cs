using Layout;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Editor
{
    [CustomEditor(typeof(GameMap))]
    public class GameMapEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement customInspector = new VisualElement();
            InspectorElement.FillDefaultInspector(customInspector, new SerializedObject(target), this);
            var button = new Button(GenerateNewLevel);
            button.text = "Generate New Level";
            customInspector.Add(button);
            return customInspector;
        }

        private void GenerateNewLevel()
        {
            var level = (GameMap)target;
            level.LoadProgram();
            level.GenerateNewLevel();
            level.PrintArrays();
        }
    }
}