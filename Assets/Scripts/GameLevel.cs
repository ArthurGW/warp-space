using System;
using LevelGenerator;
using LevelGenerator.Extensions;
using Unity.VisualScripting;
using UnityEngine;

public class GameLevel : MonoBehaviour
{
    #region Level Generator Params

    public uint numLevels;
    public uint width;
    public uint height;
    public uint minRooms;
    public uint maxRooms;
    
    [Tooltip("seed=0 means use a random seed rather than a fixed value")]
    public uint seed = 0;

    private string _program;
    
    #endregion

    private void Start()
    {
        GenerateNewLevel();
        // Thread
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
        
        var gen = new LevelGenerator.LevelGenerator(
            numLevels, width, height, minRooms, maxRooms, seed, _program
        );
        Debug.Log(gen.SolveSafe());
        PrintLevel(gen);
    }

    private void PrintLevel(LevelGenerator.LevelGenerator levelgen)
    {
        var lvl = levelgen.BestLevel();
        if (lvl != null)
        {
            Debug.Log(lvl.NumMapSquares());
            Debug.Log(lvl.NumRooms());
            Debug.Log(lvl.NumAdjacencies());
            
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
