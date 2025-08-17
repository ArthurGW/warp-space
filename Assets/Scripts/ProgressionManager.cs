using System;
using Layout;
using MapObjects;
using static MapObjects.ObjectUtils;
using UnityEngine;
using Random = UnityEngine.Random;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

/// <summary>
/// Singleton to manage difficulty progression throughout the game 
/// </summary>
[RequireComponent(typeof(PauseController))]
public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager instance;

    public uint seed = 0;

    private uint _levelSeed = 0;
    
    public bool resetSeedOnPlay = true;

    private GameMapGenerator _mapGenerator;
    private GameMapController _mapController;
    
    [SerializeField]
    private Animator fadeAnimator;

    public bool needsFadeOut = false;

    private void Awake()
    {
        // If instance already exists, ensure we don't create a duplicate
        if (instance != null)
        {
            DestroyGameObject(gameObject);
            return;
        }
        
        instance = this;
        
        _mapGenerator = FindAnyObjectByType<GameMapGenerator>();
        _mapController = FindAnyObjectByType<GameMapController>();
        if (resetSeedOnPlay)
        {
            seed = (uint)Random.Range(1, int.MaxValue / 2);
        }
        Random.InitState((int)seed);  // All random generation runs off the global seed

        _levelSeed = seed;  // But the level seed changes on each level generation
    }

    private async void Start()
    {
        try
        {
            PauseController.instance.IsPaused = true;
            await NextLevel();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            PauseController.instance.IsPaused = false;
        }
    }

    private async Awaitable WaitForFade(string trigger)
    {
        fadeAnimator.SetTrigger(trigger);
        while (true)
        {
            var state = fadeAnimator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName("SceneFadeIn") && !state.IsName("SceneFadeOut")) return;
            await Awaitable.NextFrameAsync();
        }
    }

    private async Awaitable NextLevel()
    {
        try
        {
            PauseController.instance.IsPaused = true;

            if (needsFadeOut)
            {
                await WaitForFade("FadeOut");
            }

            needsFadeOut = false;
            
            _mapController.DestroyMap();
            await Awaitable.NextFrameAsync();
            
            _levelSeed += 100;
            _mapGenerator.seed += _levelSeed;
            await _mapGenerator.GenerateNewLevel();
            
            await WaitForFade("FadeIn");

            needsFadeOut = true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            PauseController.instance.IsPaused = false;
        }
    }

    public void OnMapComplete()
    {
        NextLevel();
    }
}
