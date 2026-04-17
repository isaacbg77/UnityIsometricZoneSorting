using IsometricZoneSorting;
using UnityEngine;

/// <summary>
/// Demo helper: finds the scene's <see cref="ZoneSortingService"/> and calls
/// <see cref="ZoneSortingService.RebuildZones"/> on Awake so the sample scene
/// works out of the box. In a real project you'd typically call RebuildZones
/// from whatever loads the scene (a room manager, level loader, etc.) when the
/// set of sorting lines changes.
/// </summary>
public class RebuildZonesOnAwake : MonoBehaviour
{
    [SerializeField] private ZoneSortingService _service;

    private void Awake()
    {
        if (_service == null) _service = FindFirstObjectByType<ZoneSortingService>();
        if (_service == null)
        {
            Debug.LogError($"[{nameof(RebuildZonesOnAwake)}]: no {nameof(ZoneSortingService)} found in scene");
            return;
        }
        _service.RebuildZones();
    }
}
