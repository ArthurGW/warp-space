using System;
using System.Collections.Concurrent;
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

    private ConcurrentQueue<MapResult> _results;

    private float _breaches = 1f;
    public float breachIncreaseRate = 0.5f;
    
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
        _results = new ConcurrentQueue<MapResult>();
        
        if (resetSeedOnPlay)
        {
            seed = (uint)Random.Range(1, int.MaxValue / 2);
        }

        Random.InitState((int)seed);  // All random generation runs off the global seed...
        _levelSeed = seed;  // ...but the level seed changes on each level generation
    }

    private async void Start()
    {
        try
        {
            PauseController.instance.IsPaused = true;
            StartGenerating();
            await NextLevel();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
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
    
    private async Awaitable<MapResult> WaitForLevel()
    {
        MapResult result;
        while (!_results.TryDequeue(out result))
        {
            await Awaitable.NextFrameAsync();
        }
        return result;
    }

    private async Awaitable StartGenerating()
    {
        while (_results.Count < 10)
        {
            // Vary the levels - new seed each time, random width and height, increasing breaches
            _mapGenerator.seed = _levelSeed++;
            _mapGenerator.width = (uint)Random.Range(16, 24);
            _mapGenerator.height = (uint)Random.Range(6, 9) * 2;
            _mapGenerator.numBreaches = (uint)Mathf.FloorToInt(_breaches);
            
            var result = await _mapGenerator.GenerateNewLevel();
            _results.Enqueue(result);

            _breaches += breachIncreaseRate;
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
            
            var level = await WaitForLevel();
            StartGenerating();  // Not awaiting this, just letting it run
            if (level != null)
                _mapController.OnMapGenerated(level);
            else
            {
                Debug.LogError("failed to generate map");
                _mapController.OnMapGenerationFailed();
            }
            
            await WaitForFade("FadeIn");

            needsFadeOut = true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            _mapController.OnMapGenerationFailed();
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
