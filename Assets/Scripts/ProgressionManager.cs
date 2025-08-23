using System;
using System.Collections.Concurrent;
using System.Linq;
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

    public bool needsFadeOut = false;

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
        
        _mapGenerator = FindAnyObjectByType<GameMapGenerator>();
        _mapController = FindAnyObjectByType<GameMapController>();
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

    private async Awaitable WaitForFade(string trigger)
    {
        fadeAnimator.SetTrigger(trigger);
        while (true)
        {
            var state = fadeAnimator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsTag("SceneFade")) return;
            // Wait a few frames, which seems to make the animation smoother
            for (var i = 0; i < 20; ++i)
                await Awaitable.NextFrameAsync();
        }
    }
    
    private async Awaitable<MapResult> WaitForLevel()
    {
        MapResult result;
        while (!_results.TryDequeue(out result))
        {
            for (var i = 0; i < 10; ++i)
                await Awaitable.NextFrameAsync();
        }
        return result;
    }

    private async Awaitable StartGenerating()
    {
        // Keep a few levels queued up
        var toGenerate = 3 - _results.Count;
        
        // Generate sizes now as Random.Range can't be used in the background thread
        var sizes = Enumerable.Range(0, toGenerate).Select(_ => (width: (uint)Random.Range(16, 24), height: (uint)Random.Range(6, 9) * 2)).ToArray();

        await Awaitable.BackgroundThreadAsync();
        foreach (var size in sizes)
        {
            // Vary the levels - new seed each time, increasing breaches (up to a point)
            _mapGenerator.seed = _levelSeed++;
            _mapGenerator.numBreaches = Math.Min((uint)Mathf.FloorToInt(_breaches), maxBreaches);
            _mapGenerator.width = size.width;
            _mapGenerator.height = size.height;
            Debug.Log($"Generating: {_mapGenerator.width}, {_mapGenerator.height}, {_mapGenerator.numBreaches}, {_mapGenerator.seed}");
            
            var result = await _mapGenerator.GenerateNewLevel();
            _results.Enqueue(result);
            
            Debug.Log("Generated");

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
        updateText.text = "Warping...";
        NextLevel();
    }
}
