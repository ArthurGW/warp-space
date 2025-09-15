using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Animations;
using Enemy;
using Layout;
using MapObjects;
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
[DisallowMultipleComponent]
public class ProgressionManager : MonoBehaviour
{
    private static ProgressionManager _instance;
    private GameMapGenerator _mapGenerator;
    private GameMapController _mapController;

    private ConcurrentQueue<MapResult> _results;
    
    private uint _levelSeed;
    private uint _levelsPerGeneration;
    private float _breaches = 1f;
    private float _portals;
    
    // This is the player's "score"
    private uint _numWarps;
    
    private static int _generationRunning = 0;
    
    private FadeComplete _fadeComplete;
    private int _fadeInHash;
    private int _fadeOutHash;
    private string _initialUpdateText;
    private AudioSource _musicSource;
    private AudioSource _fxSource;
    
    private bool _warpInfoShown;
    private bool _portalInfoShown;
    
    private PlayerController _playerController;
    
#if !UNITY_EDITOR
    private InputAction _quit;
    private bool _waitingForQuit;
#endif
    
    [Header("Level Generation")]
    public uint seed;
    public bool resetSeedOnPlay = true;

    public uint levelsFirstGeneration = 3u;
    public uint maxLevelsPerGeneration = 5u;
    
    public uint numSolverThreads = 2u;
    
    [Header("Level Characteristics")]
    public uint minRoomCount = 3u;
    public uint maxRoomCount = 12u;

    public float breachIncreaseRate = 0.5f;
    public uint maxBreaches = 5u;

    public float portalIncreaseRate = 0.5f;
    public uint maxPortals = 10u;
    
    [Header("Enemy Characteristics")]
    public float enemySpeed = 8f;
    public float enemySpeedMultiplier = 1.05f;

    public float enemyAccel = 10f;
    public float enemyAccelMultiplier = 1.05f;

    public float enemyAngularSpeed = 210f;
    public float enemyAngularSpeedMultiplier = 1.05f;

    public float enemyMinSpawnTime = 5f;
    public float enemyMinSpawnTimeMultiplier = 0.95f;
    public float enemyMaxSpawnTime = 8f;
    public float enemyMaxSpawnTimeMultiplier = 0.95f;
    
    [Header("Controls & Display")]
    [SerializeField] private Animator fadeAnimator;
    public bool needsFadeOut;

    [SerializeField] private Button restartButton;
    [SerializeField] private TextMeshProUGUI numWarpsText;
    [SerializeField] private TextMeshProUGUI updateText;
    [SerializeField] private TextMeshProUGUI waitForQuitText;

    [SerializeField] private AudioClip alarmSound;
    [SerializeField] private AudioClip warpSound;
    [SerializeField] private AudioClip[] music;
    
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

#if !UNITY_EDITOR
        _quit = InputSystem.actions.FindAction("Player/Quit");
        waitForQuitText.enabled = false;
#endif
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

            if (!_musicSource.isPlaying)
            {
                _musicSource.clip = music[Random.Range(0, music.Length)];
                _musicSource.Play();
            }
            _fxSource.PlayOneShot(alarmSound);
            
            _initialUpdateText = updateText.text;
            
            await NextLevel();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

#if !UNITY_EDITOR
    private void Update()
    {
        switch (_quit.triggered)
        {
            case true when _waitingForQuit:
                Application.Quit();
                break;
            case true:
                _waitingForQuit = true;
                StartCoroutine(nameof(WaitForQuit));
                break;
        }
    }

    private IEnumerator WaitForQuit()
    {
        _waitingForQuit = true;
        waitForQuitText.enabled = true;
        yield return new WaitForSecondsRealtime(5f);
        _waitingForQuit = false;
        waitForQuitText.enabled = false;
    }
#endif

    private async Awaitable WaitForFade(int trigger)
    {
        fadeAnimator.SetTrigger(trigger);
        await _fadeComplete.onComplete;
    }
    
    private static async Awaitable WaitForUnscaledTime(float time)
    {
        await Awaitable.NextFrameAsync();
        while (Time.unscaledTime < time)
            await Awaitable.NextFrameAsync();
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

    private static async Awaitable FlyBetween(
        Transform cameraTransform,
        Vector3 startPos,
        Quaternion startRot,
        Vector3 endPos,
        Quaternion endRot,
        float speedMultiplier)
    {
        var phase = Mathf.PI;
        while (phase <= Mathf.PI * 2f)
        {
            await Awaitable.NextFrameAsync();
            var amount = (Mathf.Cos(phase) + 1f) / 2f;
            cameraTransform.position = Vector3.Lerp(startPos, endPos, amount);
            cameraTransform.rotation = Quaternion.Slerp(startRot, endRot, amount);
            phase += Time.unscaledDeltaTime * speedMultiplier;
        }
        await WaitForUnscaledTime(Time.unscaledTime + 1);
    }

    private static (Vector3 pos, Quaternion rot) GetPortalView(PortalEnd portal, float offset)
    {
        var spawnPoint = portal.SpawnPoint;
        var spawnOffsetVector = new Vector3(
            spawnPoint.x - portal.transform.position.x,
            portal.transform.position.y,
            spawnPoint.y - portal.transform.position.z
        );
        
        // View position offset by offset, at 45 degrees
        var offsetVector =  (spawnOffsetVector.normalized + Vector3.up).normalized;
        var viewPos = portal.transform.position + offsetVector * offset;
        return (viewPos, Quaternion.LookRotation(-offsetVector));
    }
    
    private async Awaitable DoFlythrough(List<(Vector3 pos, Quaternion rot)> destinations)
    {
        var cameraTransform = _playerController.CameraFollow.CameraTransform;
        var startingPosition = cameraTransform.position;
        var startingRotation = cameraTransform.rotation;
        _playerController.CameraFollow.enabled = false;

        destinations = destinations
            .Prepend((startingPosition, startingRotation))
            .Append((startingPosition, startingRotation))
            .ToList();

        try
        {
            for (var i = 0; i < destinations.Count - 1; ++i)
            {
                await FlyBetween(
                    cameraTransform,
                    destinations[i].pos,
                    destinations[i].rot,
                    destinations[i + 1].pos,
                    destinations[i + 1].rot,
                    i == destinations.Count - 2 ? 2f : 1f
                );
            }
        }
        finally
        {
            cameraTransform.position = startingPosition;
            cameraTransform.rotation = startingRotation;
            _playerController.CameraFollow.enabled = true;
        }
    }
    
    private async Awaitable DoWarpFlythrough()
    {
        try
        {
            var warp = FindAnyObjectByType<WarpController>();
            if (!warp) return;
        
            var offsetVector =  (Vector3.left + Vector3.up).normalized;
            var viewPos = warp.transform.position + offsetVector * _playerController.CameraFollow.offset;
            var viewRot = Quaternion.LookRotation(-offsetVector);
        
            await DoFlythrough(new List<(Vector3, Quaternion)>{(viewPos, viewRot)});
        }
        finally
        {
            _warpInfoShown = true;
        }
    }

    private async Awaitable DoPortalFlythrough()
    {
        var offset = _playerController.CameraFollow.offset;

        try
        {
            var firstPortal = FindAnyObjectByType<PortalEnd>();
            if (!firstPortal) return;
        
            var secondPortal = firstPortal.Destination;
            if (!secondPortal) return;

            var firstView = GetPortalView(firstPortal, offset);
            var secondView = GetPortalView(secondPortal, offset);

            await DoFlythrough(new List<(Vector3, Quaternion)> { firstView, secondView });
        }
        finally
        {
            _portalInfoShown = true;
        }
        
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

            // Ensure faded-out for some time, to allow time for audio and a breather for the player
            var minimumTime = Time.unscaledTime + 4f;

            _mapController.DestroyMap();

            var level = await WaitForLevel();
            if (level != null)
            {
                _mapController.OnMapGenerated(level);
                _mapController.SetEnemyCharacteristics(
                    enemySpeed, enemyAccel, enemyAngularSpeed, enemyMinSpawnTime, enemyMaxSpawnTime
                );
                
                if (level.Portals.Any(pair => pair.Value.Count > 0) && !_portalInfoShown)
                {
                    updateText.text += "\n\nPortals are appearing! Use them to travel around the ship!";
                    await WaitForUnscaledTime(Time.unscaledTime + 4f);
                }
            }
            else
            {
                Debug.LogError("failed to generate map");
                _mapController.OnMapGenerationFailed();
            }

            await WaitForUnscaledTime(minimumTime);
            await WaitForFade(_fadeInHash);
            fadeAnimator.gameObject.SetActive(false);
            needsFadeOut = true;

            if (!_warpInfoShown)
                await DoWarpFlythrough();
            else if (level != null && level.Portals.Any(pair => pair.Value.Count > 0) && !_portalInfoShown)
                await DoPortalFlythrough();
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
        
        // Make the enemies harder
        enemySpeed *= enemySpeedMultiplier;
        enemyAccel *= enemyAccelMultiplier;
        enemyAngularSpeed *= enemyAngularSpeedMultiplier;
        enemyAngularSpeed = Mathf.Min(enemyAngularSpeed, 360f);
        enemyMinSpawnTime *= enemyMinSpawnTimeMultiplier;
        enemyMaxSpawnTime *= enemyMaxSpawnTimeMultiplier;
        enemyMaxSpawnTime = Mathf.Max(enemyMaxSpawnTime, enemyMinSpawnTime + 0.5f);
        
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
