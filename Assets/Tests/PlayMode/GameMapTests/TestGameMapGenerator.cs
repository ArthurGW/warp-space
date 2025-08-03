using System.Collections;
using Layout;
using UnityEngine.TestTools;

namespace Tests.PlayMode.GameMapTests
{
    /// <summary>
    /// GameMapGenerator subclass to enable testing by implementing IMonoBehaviourTest
    /// </summary>
    public class GameMapGeneratorToTest : GameMapGenerator, IMonoBehaviourTest
    {
        private bool _isGenerated = false;
        
        public bool IsTestFinished => _isGenerated;

        private void OnEnable()
        {
            onMapGenerated.AddListener(OnGenerated);
            
            // Set valid values, this happens before the parent class' Start method starts level generation
            width = 10;
            height = 7;
            minRooms = 2;
            maxRooms = 6;
            maxNumLevels = 2;
            solverThreads = 1;
            seed = 1234;
        }

        private void OnGenerated(MapResult _)
        {
            _isGenerated = true;
        }
    }
    
    public class TestGameMapGenerator
    {
        [UnityTest]
        public IEnumerator GameMapGeneratesLevel()
        {
            yield return new MonoBehaviourTest<GameMapGeneratorToTest>();
        }
    }
}
