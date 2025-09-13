using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Animations;
using Enemy;
using Layout;
using Player;
using TMPro;
using static MapObjects.ObjectUtils;
using UnityEngine;
using UnityEngine.UI;
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

    private uint _levelsPerGeneration;
    public uint levelsFirstGeneration = 3u;
    public uint maxLevelsPerGeneration = 5u;
    public uint minRoomCount = 3u;
    public uint maxRoomCount = 10u;
    public uint numSolverThreads = 2u;
    
    private float _breaches = 1f;
    public float breachIncreaseRate = 0.5f;
    public uint maxBreaches = 5u;

    private float _portals = 0f;
    public float portalIncreaseRate = 0.5f;
    public uint maxPortals = 10u;
    
    [SerializeField]
    private Animator fadeAnimator;

    private FadeComplete _fadeComplete;

    public bool needsFadeOut = false;

    private int _fadeInHash;
    private int _fadeOutHash;

    private uint _numWarps = 0u;
    [SerializeField] private TextMeshProUGUI numWarpsText;
    [SerializeField] private Button restartButton;
    [SerializeField] private TextMeshProUGUI updateText;
    private string _initialUpdateText;

    [SerializeField] private AudioClip alarmSound;
    [SerializeField] private AudioClip warpSound;
    [SerializeField] private AudioClip[] music;

    private AudioSource _musicSource;
    private AudioSource _fxSource;
    
    private PlayerController _playerController;
    
    private static int _generationRunning = 0;
    
    private void Awake()
    {
        // If instance already exists, ensure we don't create a duplicate
        if (_instance != null)
        {
            DestroyGameObject(gameObject);
            return;
        }
        
        _instance = this;
        
        _musicSource  = GetComponentsInChildren<AudioSource>().First(src => src.CompareTag("Music"));
        _fxSource  = GetComponentsInChildren<AudioSource>().First(src => src.CompareTag("SoundFX"));
        
        _fadeInHash  = Animator.StringToHash("SceneFadeIn");
        _fadeOutHash  = Animator.StringToHash("SceneFadeOut");
        _fadeComplete = fadeAnimator.GetComponent<FadeComplete>();
        
        _mapGenerator = FindAnyObjectByType<GameMapGenerator>();
        _mapController = FindAnyObjectByType<GameMapController>();
        _mapController.mapComplete.AddListener(OnMapComplete);

        _playerController = FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(mv => mv.CompareTag("Player"));
        _playerController.playerDeath.AddListener(OnPlayerDeath);
        
        if (resetSeedOnPlay)
        {
            seed = (uint)Random.Range(1, int.MaxValue / 2);
        }

        Random.InitState((int)seed);  // All Unity random generation runs off the global seed...
        _levelSeed = seed;  // ...but the level seed changes on each level generation

        restartButton.onClick.AddListener(OnRestartGame);
    }

    private async void Start()
    {
        try
        {
            restartButton.gameObject.SetActive(false);
            PauseController.instance.IsPaused = true;
            
            _results = new ConcurrentQueue<MapResult>();
            _levelsPerGeneration = levelsFirstGeneration;
            RestartGeneration();
            
            _musicSource.clip = music[Random.Range(0, music.Length)];
            _musicSource.Play();
            _fxSource.PlayOneShot(alarmSound);
            
            _initialUpdateText = updateText.text;
            
            await NextLevel();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async Awaitable WaitForFade(int trigger)
    {
        fadeAnimator.SetTrigger(trigger);
        await _fadeComplete.onComplete;
    }
    
    private async Awaitable<MapResult> WaitForLevel()
    {
        MapResult result = null;
        while (!_results.TryDequeue(out result) && !_mapGenerator.CheckCancel())
        {
            for (var i = 0u; i < 10u; ++i)
                await Awaitable.NextFrameAsync();
            _mapGenerator.InterruptIfHasLevel();
        }
        return result;
    }

    public void RestartGeneration()
    {
        _mapGenerator.ResetTokens();
        StartGenerating();
    }

    private bool CheckCancel()
    {
        if (!_mapGenerator.CheckCancel()) return false;
        
        Interlocked.Exchange(ref _generationRunning, 0);
        return true;
    }

    private async Awaitable StartGenerating()
    {
        Interlocked.Exchange(ref _generationRunning, 1);
        while(!CheckCancel())
        {
            // Keep a few levels queued up for faster transitions
            var toGenerate = 5 - _results.Count;

            // Vary the levels - random width and height, new seed each time, increasing breaches (up to a point)
            // Generate params now as Random.Range can't be used in the background thread
            var parameters = Enumerable.Range(0, toGenerate).Select(_ =>
            {
                var numBreaches = Math.Min((uint)Mathf.FloorToInt(_breaches), maxBreaches);
                var numPortals = Math.Min((uint)Mathf.FloorToInt(_portals), maxPortals);
                _breaches += breachIncreaseRate;
                _portals += portalIncreaseRate;
                
                var paramTuple = (
                    numLevels: _levelsPerGeneration,
                    width: (uint)Random.Range(14, 18),
                    height: (uint)Random.Range(7, 8) * 2,
                    numBreaches,
                    numPortals,
                    levelSeed: _levelSeed++
                );
                _levelsPerGeneration = maxLevelsPerGeneration;  // All bar first gen get the max levels
                return paramTuple;
            }).ToArray();

            await Awaitable.BackgroundThreadAsync();
            
            foreach (var parameterSet in parameters)
            {
                if (CheckCancel()) return;
                Debug.Log($"Generating: {parameterSet}");
                var result = await _mapGenerator.GenerateNewLevel(
                    parameterSet.numLevels,
                    parameterSet.width,
                    parameterSet.height,
                    minRoomCount,
                    maxRoomCount,
                    parameterSet.numBreaches,
                    parameterSet.numPortals,
                    parameterSet.levelSeed,
                    numSolverThreads
                );
                if (CheckCancel()) return;
                _results.Enqueue(result);
                Debug.Log($"Generated {result.NumLevelsGenerated} levels with: {parameterSet}");
            }
            
            if (CheckCancel()) return;
            await Awaitable.MainThreadAsync();
            if (CheckCancel()) return;
            await Awaitable.WaitForSecondsAsync(5);
        }
    }

    private async Awaitable NextLevel()
    {
        try
        {
            _playerController.EnableMovement = false;
            PauseController.instance.IsPaused = true;

            if (needsFadeOut)
            {
                fadeAnimator.gameObject.SetActive(true);
                await WaitForFade(_fadeOutHash);
                needsFadeOut = false;
            }

            _mapController.DestroyMap();

            var level = await WaitForLevel();
            if (level != null)
            {
                _mapController.OnMapGenerated(level);
            }
            else
            {
                Debug.LogError("failed to generate map");
                _mapController.OnMapGenerationFailed();
            }
            
            await Awaitable.NextFrameAsync();
            await Awaitable.EndOfFrameAsync();
            await WaitForFade(_fadeInHash);
            fadeAnimator.gameObject.SetActive(false);
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
            _playerController.EnableMovement = true;
            updateText.text = "";
        }
    }

    private void OnMapComplete()
    {
        updateText.text = "Warping...";
        numWarpsText.text = $"Warps: {++_numWarps}";
        _fxSource.Stop();
        _fxSource.PlayOneShot(warpSound);
        NextLevel();
    }

    private void OnRestartGame()
    {
        WaitForGenerationExit();
    }

    private async Awaitable WaitForGenerationExit()
    {
        await Awaitable.NextFrameAsync();
        while (Interlocked.CompareExchange(ref _generationRunning, 1, 1) == 1)
            await Awaitable.NextFrameAsync();

        foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            DestroyGameObject(enemy.gameObject);
        }

        await Awaitable.NextFrameAsync();
        
        _breaches = 1f;
        _portals = 0f;
        _numWarps = 0u;
        numWarpsText.text = "Warps: 0";
        updateText.text = _initialUpdateText;
        _playerController.Resurrect();
        Start();
    }

    private void OnPlayerDeath()
    {
        _mapGenerator.DoCancel();
        restartButton.gameObject.SetActive(true);
    }
}
