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
        
        var gen = new LevelGenerator.LevelGenerator();
        Debug.Log(gen.SetHeight(height).SetWidth(width).SetMinRooms(minRooms).SetMaxRooms(maxRooms).SetSeed(seed).SetProgram(_program).SolveSafe());
        PrintLevel(gen);
        GC.Collect();
        PrintLevel(gen);
        // Debug.Log(level.NumAdjacencies);
        // Debug.Log(level.Cost);
        // foreach (var sq in gen.BestLevel.MapSquares)
        // {
        //     Debug.Log(sq.Type);
        // }
    }

    private void PrintLevel(LevelGenerator.LevelGenerator levelgen)
    {
        var lvl = levelgen.BestLevel();
        Debug.Log(levelgen.Height);
        Debug.Log(levelgen.Width);
        Debug.Log(levelgen.Program);
        Debug.Log(lvl.NumMapSquares());
        Debug.Log(lvl.NumRooms());
        Debug.Log(lvl.NumAdjacencies());
        foreach (var rm in lvl.Rooms())
        {
            Debug.Log(rm.X);
        }
    }
}
