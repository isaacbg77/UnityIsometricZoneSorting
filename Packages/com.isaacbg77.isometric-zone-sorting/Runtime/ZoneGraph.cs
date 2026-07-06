using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IsometricZoneSorting
{
	/// <summary>
	/// Computes depth-ordered vertical strips from a set of pivots.
	/// Each pivot's Y position defines its sorting order relative to every other.
	/// The V-shaped vector axes extending from each pivot point control the ordering boundary (front vs back)
	/// for that specific pivot. Each V arm is capped by its length, and horizontal lines extend
	/// outward from each arm's tip to define per-side elevation boundaries.
	/// </summary>
	public class ZoneGraph
	{
		private readonly struct PivotNode
		{
			public readonly Vector2 Position;
			public readonly Vector2 RightAxis;
			public readonly Vector2 LeftAxis;
			public readonly Vector2 RightNormal;
			public readonly Vector2 LeftNormal;
			public readonly float RightVectorLength;
			public readonly float LeftVectorLength;
			public readonly float RightTipY;
			public readonly float LeftTipY;
			public readonly int BaseSortingOrder;

			public PivotNode(BoundaryZoneSortable zoneSortable, int baseSortingOrder)
			{
				Position = zoneSortable.SortPosition;
				ZoneSortingPivot.SortingAxes axes = zoneSortable.SortingAxes;
				RightAxis = axes.RightAxis;
				LeftAxis = axes.LeftAxis;
				RightVectorLength = axes.RightVectorLength;
				LeftVectorLength = axes.LeftVectorLength;

				// Front-facing normals (both point downward)
				RightNormal = new Vector2(RightAxis.y, -RightAxis.x);
				LeftNormal = new Vector2(-LeftAxis.y, LeftAxis.x);

				// Y elevation at each arm's tip
				RightTipY = Position.y + RightAxis.y * RightVectorLength;
				LeftTipY = Position.y + LeftAxis.y * LeftVectorLength;

				BaseSortingOrder = baseSortingOrder;
			}

			/// <summary>
			/// Determines if a point is on the front side (below/in-front-of) this pivot,
			/// using the V shape capped by arm lengths and horizontal lines from each tip.
			/// Each horizontal line only applies to its respective side (left or right of pivot).
			/// </summary>
			public bool IsOnFrontSide(Vector2 point)
			{
				Vector2 offset = point - Position;

				// Check if the point is inside the V region (between both arms, within their lengths).
				bool frontOfRight = Vector2.Dot(offset, RightNormal) > 0f;
				bool frontOfLeft = Vector2.Dot(offset, LeftNormal) > 0f;

				if (frontOfRight && frontOfLeft)
				{
					// Below the cone tip, i.e., in front of both arms.
					return true;
				}
				else if (!frontOfRight && !frontOfLeft)
				{
					// Fully inside the cone tip, i.e., behind both arms.
					return false;
				}

				// Outside the cone above the cone tip, either to the left or the right side of the pivot.
				// Check per-side.
				
				if (point.x > Position.x)
				{
					// Right side: point is in front if it's below the right arm's tip Y.
					return point.y < RightTipY;
				}
				if (point.x < Position.x)
				{
					// Left side: point is in front if it's below the left arm's tip Y.
					return point.y < LeftTipY;
				}
				return false;
			}
		}

		private readonly List<BoundaryZoneSortable> _sortedBoundaries;
		private readonly List<PivotNode> _sortedPivots;
		private readonly int _zoneOrderStride;

		public int PivotCount => _sortedPivots.Count;

		public int GetBaseSortingOrder(int pivotIndex)
		{
			pivotIndex = Mathf.Clamp(pivotIndex, 0, _sortedPivots.Count - 1);
			return _sortedPivots[pivotIndex].BaseSortingOrder;
		}

		public Vector2 GetPivotPosition(int pivotIndex)
		{
			pivotIndex = Mathf.Clamp(pivotIndex, 0, _sortedPivots.Count - 1);
			return _sortedPivots[pivotIndex].Position;
		}

		public BoundaryZoneSortable GetSortedBoundary(int pivotIndex)
		{
			pivotIndex = Mathf.Clamp(pivotIndex, 0, _sortedPivots.Count - 1);
			return _sortedBoundaries[pivotIndex];
		}

		public ZoneGraph(IReadOnlyList<BoundaryZoneSortable> boundarySortables, int zoneOrderStride = 1)
		{
			_zoneOrderStride = Mathf.Max(1, zoneOrderStride);

			// Sort pivots by Y descending (higher Y is further back).
			_sortedBoundaries = boundarySortables.OrderByDescending(sortable => sortable.SortPosition.y).ToList();
			
			_sortedPivots = new List<PivotNode>(_sortedBoundaries.Count);
			for (var i = 0; i < _sortedBoundaries.Count; i++)
			{
				// Depth 0 = backmost, Depth i = i-th strip.
				_sortedPivots.Add(new PivotNode(_sortedBoundaries[i], i * _zoneOrderStride));
			}
		}

		/// <summary>
		/// Returns the sorting order for the given world position.
		/// </summary>
		public int GetSortingOrderInLayer(Vector2 worldPosition, ref int cachedPivotIndex)
		{
			if (_sortedPivots.Count == 0) return 0;
			else if (cachedPivotIndex >= _sortedPivots.Count)
			{
				cachedPivotIndex = _sortedPivots.Count - 1;
			}

			int pivotIndex = FindPivotIndexNearCached(worldPosition, cachedPivotIndex);
			cachedPivotIndex = pivotIndex;
			
			if (pivotIndex == _sortedPivots.Count)
			{
				// The point is beyond the frontmost pivot and thus guaranteed to be below the pivot's V tip.
				// Advance by a zone order stride.
				return _sortedPivots[pivotIndex - 1].BaseSortingOrder + _zoneOrderStride;
			}

			// Y is at or just above the pivot's V tip.

			// Determine if we are in front of or behind the pivot at this index based on the V-axes.
			bool isInFront = _sortedPivots[pivotIndex].IsOnFrontSide(worldPosition);

			if (isInFront)
			{
				// The point is below one of the V axis vectors, or below the horizontal line corresponding to the
				// axis vector's tip.
				return _sortedPivots[pivotIndex].BaseSortingOrder + _zoneOrderStride;
			}
			else
			{
				// The point is inside the V shape, or above the horizontal line corresponding to the axis vector's tip.
				return _sortedPivots[pivotIndex].BaseSortingOrder;
			}
		}

		public int GetSortingOrderInLayer(Vector2 worldPosition)
		{
			int dummyIndex = -1;
			return GetSortingOrderInLayer(worldPosition, ref dummyIndex);
		}

		private int FindPivotIndexNearCached(Vector2 point, int cachedIndex)
		{
			if (cachedIndex < 0)
			{
				return FindYIndex(point);
			}

			// There is a cached index.

			// Check if the cached index still holds.
			bool isYAboveCachedIndexPosition = IsPointAboveIndex(point, cachedIndex);
			bool isYAtCachedIndex = isYAboveCachedIndexPosition && !IsPointAboveIndex(point, cachedIndex - 1);

			if (isYAtCachedIndex)
			{
				return cachedIndex;
			}
			if (isYAboveCachedIndexPosition)
			{
				return cachedIndex - 1 <= 0 ? 0 : FindYIndex(point.y, 0, cachedIndex - 1);
			}
			int indexCount = _sortedPivots.Count;
			return cachedIndex + 1 >= indexCount ? cachedIndex : FindYIndex(point.y, cachedIndex + 1, indexCount - 1);
		}

		private bool IsPointAboveIndex(Vector2 point, int index)
		{
			if (index >= _sortedPivots.Count) return true; // Beyond the front-most.
			if (index < 0) return false; // Beyond the back-most. 
			return point.y >= _sortedPivots[index].Position.y;
		}

		private int FindYIndex(Vector2 point) => FindYIndex(point.y, 0, _sortedPivots.Count - 1);

		private int FindYIndex(float y, int low, int high)
		{
			// Binary search for the index where y would be inserted in the Y-descending list.

			// We default the result index as out of bounds, beyond the frontmost sorted pivot.
			int result = _sortedPivots.Count;

			while (low <= high)
			{
				int mid = low + (high - low) / 2;
				if (_sortedPivots[mid].Position.y <= y)
				{
					result = mid;
					high = mid - 1;
				}
				else
				{
					low = mid + 1;
				}
			}

			return result;
		}
	}
}
