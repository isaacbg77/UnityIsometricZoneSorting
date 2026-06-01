using UnityEngine;
using UnityEngine.Rendering;

namespace IsometricZoneSorting
{
    public class StaticZoneSortable : MonoBehaviour, IStaticZoneSortable
    {
        [SerializeField, Min(0), Tooltip("Offset added to the zone's first sorting layer. " +
                                         "0 (default) puts this sortable on the first layer in its zone; " +
                                         "raise it to stack above other movers within the same zone. " +
                                         "Must be less than stride-1 to stay inside the zone; stride-1 lands on the zone's front boundary.")]
        private int _sortOrderBias;

        private IZoneSortingService? _zoneSortingService;

        public SortingGroup? SortingGroup { get; private set; }

        public Vector2 SortPosition => transform.position;
        public int SortOrderBias => _sortOrderBias;

        private void Awake()
        {
            _zoneSortingService = SceneUtils.FindInterfaceOfType<IZoneSortingService>();
            if (_zoneSortingService == null) Debug.LogError($"[{nameof(DynamicZoneSortable)}]: {nameof(IZoneSortingService)} is null", this);
            
            SortingGroup = GetComponent<SortingGroup>();
        }

        private void OnEnable()
        {
            if (_zoneSortingService == null || SortingGroup == null) return;
            _zoneSortingService.Register(this);
        }

        private void OnDisable()
        {
            _zoneSortingService?.Unregister(this);
        }
    }
}