using System;
using System.Threading.Tasks;
using Layout;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Editor
{
    [CustomEditor(typeof(GameMapGenerator))]
    public class GameMapGeneratorEditor : UnityEditor.Editor
    {
        private UnityAction<MapResult> _onMapGenerated;
        private UnityAction _onMapFailed;
        
        private bool IsGenerating => ((GameMapGenerator)target).IsGenerating;

        private Button _generateButton;

        private void Awake()
        {
            _onMapFailed = OnMapGenerationFailed;
            _onMapGenerated = OnMapGenerated;
            
            var level = (GameMapGenerator)target;
            level.onMapGenerated.AddListener(_onMapGenerated);
            level.onMapGenerationFailed.AddListener(_onMapFailed);
        }

        public override VisualElement CreateInspectorGUI()
        {
            var customInspector = new VisualElement();
            InspectorElement.FillDefaultInspector(customInspector, new SerializedObject(target), this);
            _generateButton = new Button(StartGenerating)
            {
                text = "Generate New Level"
            };
            _generateButton.SetEnabled(!IsGenerating);
            customInspector.Add(_generateButton);
            return customInspector;
        }

        private void StartGenerating()
        {
            if (IsGenerating)
            {
                return;
            }

            _generateButton?.SetEnabled(false);
            Task.Run(GenerateNewLevel);
        }
        
        private void OnMapGenerated(MapResult _)
        {
            _generateButton?.SetEnabled(true);
            var level = (GameMapGenerator)target;
            level.PrintArrays();
        }


        private void OnMapGenerationFailed()
        {
            _generateButton?.SetEnabled(true);
        }
        
        private async Awaitable GenerateNewLevel()
        {
            await Awaitable.MainThreadAsync();
            var level = (GameMapGenerator)target;
            await level.GenerateNewLevel();
        }
    }
}