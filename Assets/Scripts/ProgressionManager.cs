using System;
using Layout;
using MapObjects;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Singleton to manage difficulty progression throughout the game 
/// </summary>
public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager instance;

    public uint seed = 0;

    private uint _realSeed = 0;
    
    public bool resetSeedOnPlay = true;

    private GameMapGenerator _mapGenerator;
    private GameMapController _mapController;

    private AwaitableCompletionSource _completer;

    [SerializeField]
    private Animator fadeAnimator;

    private bool _firstFade;
    private bool _fading;

    private void Awake()
    {
        // If instance already exists, ensure we don't create a duplicate
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        
        _mapGenerator = FindFirstObjectByType<GameMapGenerator>();
        _mapController = FindFirstObjectByType<GameMapController>();
        if (resetSeedOnPlay)
        {
            seed = (uint)Random.Range(1, int.MaxValue / 2);
        }

        _realSeed = seed;
        _firstFade = true;
        _completer = new AwaitableCompletionSource();
    }

    private async void Start()
    {
        try
        {
            await NextLevel();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void Update()
    {
        if (!_fading) return;

        var state = fadeAnimator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("SceneFadeIn") || state.IsName("SceneFadeOut")) return;
        
        _fading = false;
        _completer.SetResult();
    }

    private async Awaitable WaitForFade(string trigger)
    {
        _completer.Reset();
        _fading = true;
        fadeAnimator.SetTrigger(trigger);
        await _completer.Awaitable;
    }

    public async Awaitable NextLevel()
    {
        try
        {
            _mapController.MovementEnabled = false;
            
            if (!_firstFade)
            {
                await WaitForFade("FadeOut");
            }

            _firstFade = false;
            
            _mapController.DestroyMap();
            await Awaitable.NextFrameAsync();
            
            _realSeed += 100;
            _mapGenerator.seed += _realSeed;
            await _mapGenerator.GenerateNewLevel();
            
            await WaitForFade("FadeIn");

            _mapController.MovementEnabled = true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public void OnMapComplete()
    {
        NextLevel();
    }
}
