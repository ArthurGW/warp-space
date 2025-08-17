using System;
using System.Collections;
using Layout;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode.GameMapTests
{
    /// <summary>
    /// GameMapGenerator subclass to enable testing by implementing IMonoBehaviourTest
    /// </summary>
    public class GameMapGeneratorToTest : GameMapGenerator, IMonoBehaviourTest
    {
        private bool _isGenerated;
        
        public bool IsTestFinished => _isGenerated;

        private async void OnEnable()
        {
            try
            {
                onMapGenerated.AddListener(OnGenerated);
                onMapGenerationFailed.AddListener(OnFailed);
            
                width = 10;
                height = 8;
                minRooms = 1;
                maxRooms = 6;
                maxNumLevels = 2;
                solverThreads = 2;
                seed = 1234;

                await GenerateNewLevel();
            }
            catch (Exception e)
            {
                OnFailed();
            }
            
        }

        private void OnGenerated(MapResult _)
        {
            _isGenerated = true;
        }

        private static void OnFailed()
        {
            Assert.Fail("Failed to generate map");
        }
    }
    
    public class TestGameMapGenerator
    {
        [UnityTest]
        public IEnumerator GameMapGeneratesMap()
        {
            yield return new MonoBehaviourTest<GameMapGeneratorToTest>();
        }
    }
}
