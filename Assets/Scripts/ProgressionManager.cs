using System;
using System.Collections.Concurrent;
using System.Linq;
using Animations;
using Layout;
using MapObjects;
using TMPro;
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
    private static ProgressionManager _instance;

    public uint seed = 0;

    private uint _levelSeed = 0;
    
    public bool resetSeedOnPlay = true;

    private GameMapGenerator _mapGenerator;
    private GameMapController _mapController;

    private ConcurrentQueue<MapResult> _results;

    private float _breaches = 1f;
    public float breachIncreaseRate = 0.5f;
    public uint maxBreaches = 5u;
    
    [SerializeField]
    private Animator fadeAnimator;

    private FadeComplete _fadeComplete;

    public bool needsFadeOut = false;

    private int _fadeInHash;
    private int _fadeOutHash;

    [SerializeField] private TextMeshProUGUI updateText;

    private void Awake()
    {
        // If instance already exists, ensure we don't create a duplicate
        if (_instance != null)
        {
            DestroyGameObject(gameObject);
            return;
        }
        
        _instance = this;
        
        _fadeInHash  = Animator.StringToHash("SceneFadeIn");
        _fadeOutHash  = Animator.StringToHash("SceneFadeOut");
        _fadeComplete = fadeAnimator.GetComponent<FadeComplete>();
        
        _mapGenerator = FindAnyObjectByType<GameMapGenerator>();
        _mapController = FindAnyObjectByType<GameMapController>();
        _mapController.mapComplete.AddListener(OnMapComplete);
        _results = new ConcurrentQueue<MapResult>();
        
        if (resetSeedOnPlay)
        {
            seed = (uint)Random.Range(1, int.MaxValue / 2);
        }

        Random.InitState((int)seed);  // All Unity random generation runs off the global seed...
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

    private static async Awaitable WaitNFrames(uint n)
    {
        for (var i = 0u; i < n; ++i)
            await Awaitable.NextFrameAsync();
    }

    private async Awaitable WaitForFade(int trigger)
    {
        // fadeAnimator.runtimeAnimatorController.animationClips[0].events[0].
        fadeAnimator.SetTrigger(trigger);
        await _fadeComplete.onComplete;
        
        // while (true)
        // {
        //     // Wait a few frames, which seems to make the animation smoother
        //     await WaitNFrames(30);
        //     
        //     var state = fadeAnimator.GetCurrentAnimatorStateInfo(0);
        //     
        //     // State names are the same as the triggers causing them here
        //     if (state.shortNameHash != trigger) return;
        // }
    }
    
    private async Awaitable<MapResult> WaitForLevel()
    {
        MapResult result;
        while (!_results.TryDequeue(out result))
        {
            await WaitNFrames(10);
        }
        return result;
    }

    private async Awaitable StartGenerating()
    {
        // Keep a few levels queued up for faster transitions
        var toGenerate = 3 - _results.Count;
        
        // Vary the levels - random width and height, new seed each time, increasing breaches (up to a point)
        // Generate params now as Random.Range can't be used in the background thread
        var parameters = Enumerable.Range(0, toGenerate).Select(_ =>
        {
            var numBreaches = Math.Min((uint)Mathf.FloorToInt(_breaches), maxBreaches);
            _breaches += breachIncreaseRate;
            return (width: (uint)Random.Range(14, 18), height: (uint)Random.Range(6, 8) * 2, numBreaches, levelSeed: _levelSeed++);
        }).ToArray();

        await Awaitable.BackgroundThreadAsync();
        foreach (var parameterSet in parameters)
        {
            Debug.Log($"Generating: {parameterSet}");
            var result = await GameMapGenerator.GenerateNewLevel(10, parameterSet.width, parameterSet.height, 3, 8, parameterSet.numBreaches, parameterSet.levelSeed, 2);
            _results.Enqueue(result);
            Debug.Log($"Generated: {parameterSet}");
        }
    }

    private async Awaitable NextLevel()
    {
        try
        {
            PauseController.instance.IsPaused = true;

            if (needsFadeOut)
            {
                await WaitForFade(_fadeOutHash);
            }

            needsFadeOut = false;
            
            // await WaitNFrames(60);
            
            _mapController.DestroyMap();

            // await WaitNFrames(60);
            
            var level = await WaitForLevel();
            StartGenerating();  // Not awaiting this, just letting it run
            if (level != null)
                _mapController.OnMapGenerated(level);
            else
            {
                Debug.LogError("failed to generate map");
                _mapController.OnMapGenerationFailed();
            }
            
            await WaitForFade(_fadeInHash);

            // await WaitNFrames(10);

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
        updateText.text = "Warping...";
        NextLevel();
    }
}
