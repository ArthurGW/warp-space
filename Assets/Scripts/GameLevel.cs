using LevelGenerator;
using UnityEngine;
using LevelGenerator.Extensions;

public class GameLevel : MonoBehaviour
{
    #region Level Generator Params

    public uint width;
    public uint height;
    public uint minRooms;
    public uint maxRooms;
    public uint maxNumLevels;
    public uint solverThreads;

    [Tooltip("seed=0 means use a random seed rather than a fixed value")]
    public uint seed = 0;

    private string _program;
    
    private Level _level;
    
    #endregion

    private void Start()
    {
        GenerateNewLevel();
        // Thread
    }

    private void OnDestroy()
    {
        Debug.Log("OnDestroy");
        if (_level != null)
        {
            _level.Dispose();
            _level = null;
        }
    }

    public void GenerateNewLevel()
    {
        if (_program == null)
        {
            var program = Resources.Load("programs/ship") as TextAsset;
            if (program != null)
            {
                _program = program.text;
            }
        }
        
        using var gen = new LevelGenerator.LevelGenerator(
            maxNumLevels, width, height, minRooms, maxRooms, seed, _program, solverThreads
        );
        Debug.Log(gen.SolveSafe());
        _level = gen.BestLevel();        
    }

    private void PrintLevel(LevelGenerator.LevelGenerator levelgen)
    {
        var lvl = levelgen.BestLevel();
        if (lvl != null)
        {
            Debug.Log(lvl.NumMapSquares);
            Debug.Log(lvl.NumRooms);
            Debug.Log(lvl.NumAdjacencies);
            
            foreach (var sq in lvl.MapSquares())
            {
                Debug.Log(sq.AsString());
            }
            foreach (var rm in lvl.Rooms())
            {
                Debug.Log(rm.AsString());
            }
            foreach (var adj in lvl.Adjacencies())
            {
                Debug.Log(adj.AsString());
            }
        }
        else
        {
            Debug.Log("NULL");
        }
        
    }
}
