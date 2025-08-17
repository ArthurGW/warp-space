using System;
using Layout;
using MapObjects;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

namespace Editor
{
    [CustomEditor(typeof(GameMapGenerator))]
    public class GameMapGeneratorEditor : UnityEditor.Editor
    {
        private Button _generateButton;

        private bool _isGenerating;
        
        public override VisualElement CreateInspectorGUI()
        {
            var customInspector = new VisualElement();
            InspectorElement.FillDefaultInspector(customInspector, new SerializedObject(target), this);
            _generateButton = new Button(StartGenerating)
            {
                text = "Generate New Level"
            };
            _generateButton.SetEnabled(!_isGenerating);
            customInspector.Add(_generateButton);
            return customInspector;
        }

        private async void StartGenerating()
        {
            try
            {
                if (_isGenerating)
                {
                    return;
                }

                _isGenerating = true;
                _generateButton?.SetEnabled(false);
                var level = (GameMapGenerator)target;
                var result = await level.GenerateNewLevel();
                FindAnyObjectByType<GameMapController>().OnMapGenerated(result);
                _generateButton?.SetEnabled(true);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                FindAnyObjectByType<GameMapController>().OnMapGenerationFailed();
                _generateButton?.SetEnabled(true);
            }
        }
    }
}