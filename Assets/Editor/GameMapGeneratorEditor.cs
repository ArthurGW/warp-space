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
        private Button _cancelButton;
        private Button _resetButton;

        private bool _isGenerating;
        
        public override VisualElement CreateInspectorGUI()
        {
            var gameMapGenerator = (GameMapGenerator)target;
            var customInspector = new VisualElement();
            InspectorElement.FillDefaultInspector(customInspector, new SerializedObject(target), this);
            _generateButton = new Button(StartGenerating)
            {
                text = "Generate New Level"
            };
            _generateButton.SetEnabled(!_isGenerating && !gameMapGenerator.CheckCancel());
            _cancelButton = new Button(CancelGenerating)
            {
                text = "Cancel Generation"
            };
            _cancelButton.SetEnabled(_isGenerating);
            _resetButton = new Button(ResetCancellation)
            {
                text = "Reset Cancellation"
            };
            customInspector.Add(_generateButton);
            customInspector.Add(_cancelButton);
            customInspector.Add(_resetButton);
            return customInspector;
        }

        private async void StartGenerating()
        {
            if (_isGenerating) return;

            try
            {
                _isGenerating = true;
                _generateButton?.SetEnabled(false);
                _cancelButton?.SetEnabled(true);
                var level = (GameMapGenerator)target;
                if (level.CheckCancel()) return;
                var result = await level.GenerateNewLevel();
                FindAnyObjectByType<GameMapController>()?.OnMapGenerated(result);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                FindAnyObjectByType<GameMapController>()?.OnMapGenerationFailed();
            }
            finally
            {
                _isGenerating = false;
                _generateButton?.SetEnabled(true);
                _cancelButton?.SetEnabled(false);
            }
        }

        private void CancelGenerating()
        {
            ((GameMapGenerator)target).DoCancel();
            _isGenerating = false;
            _generateButton?.SetEnabled(false);  // Need to reset before generating again
            _cancelButton?.SetEnabled(false);
        }

        private void ResetCancellation()
        {
            var gameMapGenerator = (GameMapGenerator)target;
            if (gameMapGenerator.CheckCancel()) gameMapGenerator.ResetTokens();
            _generateButton?.SetEnabled(!_isGenerating);
        }
    }
}