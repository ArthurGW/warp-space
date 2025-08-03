using System.Collections;
using Layout;
using UnityEngine.TestTools;

namespace Tests.PlayMode.GameMapTests
{
    /// <summary>
    /// GameMap subclass to enable testing by implementing IMonoBehaviourTest
    /// </summary>
    public class GameMapToTest : GameMap, IMonoBehaviourTest
    {
        public bool IsTestFinished => isGenerated;

        private void OnEnable()
        {
            // Set valid values, this happens before the parent class' Start method starts level generation
            width = 10;
            height = 7;
            minRooms = 2;
            maxRooms = 6;
            maxNumLevels = 2;
            solverThreads = 1;
            seed = 1234;
        }
        
    }
    
    public class TestGameMap
    {
        [UnityTest]
        public IEnumerator GameMapGeneratesLevel()
        {
            yield return new MonoBehaviourTest<GameMapToTest>();
        }
    }
}
