using System;
using System.Collections.Concurrent;
using System.Linq;
using Animations;
using Layout;
using MapObjects;
using Player;
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
    public Vector3 currentStartPos;
    public Vector3 currentFinishPos;
    
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

    [SerializeField] private AudioClip alarmSound;
    [SerializeField] private AudioClip warpSound;
    [SerializeField] private AudioClip[] music;

    private AudioSource _musicSource;
    private AudioSource _fxSource;
    
    private PlayerMovement _playerMovement;
    
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
        _results = new ConcurrentQueue<MapResult>();

        _playerMovement = FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(mv => mv.CompareTag("Player"));
        
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
            _musicSource.clip = music[Random.Range(0, music.Length)];
            _musicSource.Play();
            _fxSource.PlayOneShot(alarmSound);
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
        }
        return result;
    }

    public void RestartGeneration()
    {
        _mapGenerator.ResetTokens();
        StartGenerating();
    }

    private async Awaitable StartGenerating()
    {
        while(!_mapGenerator.CheckCancel())
        {
            // Keep a few levels queued up for faster transitions
            var toGenerate = 3 - _results.Count;

            // Vary the levels - random width and height, new seed each time, increasing breaches (up to a point)
            // Generate params now as Random.Range can't be used in the background thread
            var parameters = Enumerable.Range(0, toGenerate).Select(_ =>
            {
                var numBreaches = Math.Min((uint)Mathf.FloorToInt(_breaches), maxBreaches);
                _breaches += breachIncreaseRate;
                return (width: (uint)Random.Range(14, 18), height: (uint)Random.Range(6, 8) * 2, numBreaches,
                    levelSeed: _levelSeed++);
            }).ToArray();

            await Awaitable.BackgroundThreadAsync();
            foreach (var parameterSet in parameters)
            {
                if (_mapGenerator.CheckCancel()) return;
                Debug.Log($"Generating: {parameterSet}");
                var result = await _mapGenerator.GenerateNewLevel(5, parameterSet.width, parameterSet.height, 3, 10,
                    parameterSet.numBreaches, parameterSet.levelSeed, 2);
                _results.Enqueue(result);
                Debug.Log($"Generated {result.NumLevelsGenerated} levels with: {parameterSet}");
            }
            if (_mapGenerator.CheckCancel()) return;
            await Awaitable.MainThreadAsync();
            if (_mapGenerator.CheckCancel()) return;
            await Awaitable.WaitForSecondsAsync(5);
        }
    }

    private async Awaitable NextLevel()
    {
        try
        {
            _playerMovement.EnableMovement = false;
            PauseController.instance.IsPaused = true;

            if (needsFadeOut)
            {
                await WaitForFade(_fadeOutHash);
                needsFadeOut = false;
            }

            _mapController.DestroyMap();

            var level = await WaitForLevel();
            if (level != null)
            {
                _mapController.OnMapGenerated(level);
                currentStartPos = level.Rooms.Where(rm => rm.Id == level.StartRoomId).Select(rm => rm.ToWorldCenter()).FirstOrDefault();
                currentFinishPos = level.Rooms.Where(rm => rm.Id == level.FinishRoomId).Select(rm => rm.ToWorldCenter()).FirstOrDefault();
            }
            else
            {
                Debug.LogError("failed to generate map");
                _mapController.OnMapGenerationFailed();
            }

            await Awaitable.NextFrameAsync();
            await Awaitable.EndOfFrameAsync();
            await WaitForFade(_fadeInHash);
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
            _playerMovement.EnableMovement = true;
            updateText.text = "";
        }
    }

    private void OnMapComplete()
    {
        updateText.text = "Warping...";
        _fxSource.Stop();
        _fxSource.PlayOneShot(warpSound);
        NextLevel();
    }
}
