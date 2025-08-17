using MapObjects;
using UnityEngine;

/// <summary>
/// Singleton to hold global paused state
/// </summary>
public class PauseController : MonoBehaviour
{
    public static PauseController instance;

    private bool _isPaused = true;

    private float _oldTimeScale;

    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            if ((!_isPaused || _oldTimeScale == 0f) && value)
            {
                // Pausing
                _oldTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else if (_isPaused && !value)
            {
                // Unpausing
                Time.timeScale = _oldTimeScale;
                _oldTimeScale = 0f;
            }
                
            _isPaused = value;
        }
    }

    private void Awake()
    {
        if (instance != null)
        {
            ObjectUtils.DestroyGameObject(gameObject);
            return;
        }

        instance = this;
    }
}