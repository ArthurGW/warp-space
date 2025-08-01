using System;
using LevelGenerator;
using LevelGenerator.Extensions;
using UnityEngine;

public class GameLevel : MonoBehaviour
{
    #region Level Generator Params

    public uint height;
    public uint width;
    public uint minRooms;
    public uint maxRooms;
    public uint seed;

    private string _program;
    
    #endregion
    
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
            width, height, minRooms, maxRooms, seed, _program
        );
        Debug.Log(gen.SolveSafe());
        PrintLevel(gen);
    }

    private void PrintLevel(LevelGenerator.LevelGenerator levelgen)
    {
        var lvl = levelgen.BestLevel();
        Debug.Log(levelgen.Height);
        Debug.Log(levelgen.Width);
        Debug.Log(levelgen.Program);
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
