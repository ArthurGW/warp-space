using System.Threading.Tasks;
using Layout;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    [CustomEditor(typeof(GameMapGenerator))]
    public class GameMapEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement customInspector = new VisualElement();
            InspectorElement.FillDefaultInspector(customInspector, new SerializedObject(target), this);
            var button = new Button(() => Task.Run(GenerateNewLevel));
            button.text = "Generate New Level";
            customInspector.Add(button);
            return customInspector;
        }
        
        private async Awaitable GenerateNewLevel()
        {
            await Awaitable.MainThreadAsync();
            var level = (GameMapGenerator)target;
            level.LoadProgram();
            await level.GenerateNewLevel();
            await Awaitable.MainThreadAsync();
            level.PrintArrays();
        }
    }
}