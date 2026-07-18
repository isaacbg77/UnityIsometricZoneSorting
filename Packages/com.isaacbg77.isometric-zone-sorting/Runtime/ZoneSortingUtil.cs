using UnityEngine;
using UnityEngine.Tilemaps;

namespace IsometricZoneSorting
{
	internal static class ZoneSortingUtil
	{
		public static Vector2 CalculateTilemapSortPosition(Tilemap? tilemap)
		{
			if (tilemap == null) return default;
			Vector3 totalPosition = Vector3.zero;
			int tileCount = 0;

			// Get the painted boundaries of the tilemap in cell coordinates.
			tilemap.CompressBounds();
			BoundsInt bounds = tilemap.cellBounds;

			// Iterate through every cell within the painted bounds, recording painted tile positions.
			foreach (Vector3Int cellPosition in bounds.allPositionsWithin)
			{
				if (!tilemap.HasTile(cellPosition)) continue;

				// Record the painted tile's position, adjusted by its sprite's pivot offset.
				Vector3 cellCenter = tilemap.GetCellCenterWorld(cellPosition);
				Matrix4x4 tileTransform = tilemap.GetTransformMatrix(cellPosition);
				Vector3 tileOffset = tileTransform.GetPosition();
				totalPosition += cellCenter + tileOffset;
				tileCount++;
			}

			// Calculate the average position of recorded tiles, or the tilemap's position if empty.
			return tileCount > 0 ? totalPosition / tileCount : tilemap.transform.position;
		}

		public static void SetTilemapSortPivot(Tilemap? tilemap, Vector3 pivotWorldSpace)
		{
			if (tilemap == null) return;

			// Sorting and culling both derive from the painted cell bounds, which only grow as tiles
			// are painted and never shrink on erase; compress so stale regions don't skew them.
			tilemap.CompressBounds();

			// The renderer's transparency sort point is its bounds center (the cell bounds around the
			// transform), not the transform position — offset the target so the sort point, rather
			// than the transform, lands on the pivot.
			Vector3 boundsCenterOffset = tilemap.transform.TransformVector(tilemap.localBounds.center);
			Vector3 targetPosition = pivotWorldSpace - boundsCenterOffset;

			// InverseTransformVector, not InverseTransformPoint: the offset is a direction, and
			// transforming it as a point would subtract the tilemap's position a second time.
			Vector3 worldOffset = targetPosition - tilemap.transform.position;
			Vector3 localOffset = tilemap.transform.InverseTransformVector(worldOffset);

			// The orientation matrix is only honored when the orientation is Custom. Capture the
			// preset-derived matrix first, since the getter returns the preset until the switch.
			Matrix4x4 orientationMatrix = tilemap.orientationMatrix;
			tilemap.orientation = Tilemap.Orientation.Custom;

			// Move the transform so the sort point sits on the pivot, counter-shifting rendered tiles
			// so they stay put.
			tilemap.transform.position = targetPosition;
			tilemap.orientationMatrix = Matrix4x4.Translate(-localOffset) * orientationMatrix;

			// Culling ignores the orientation matrix, so the counter-shifted tiles would vanish once
			// the transform leaves the camera frustum. Pad the chunk culling bounds (local units,
			// like the offset) to cover the shift, plus each sprite's own overhang since switching
			// to Manual discards the sprite padding that Auto detection provided.
			var tilemapRenderer = tilemap.GetComponent<TilemapRenderer>();
			if (tilemapRenderer != null)
			{
				Vector3 offsetPadding = new Vector3(Mathf.Abs(localOffset.x), Mathf.Abs(localOffset.y), Mathf.Abs(localOffset.z));
				tilemapRenderer.detectChunkCullingBounds = TilemapRenderer.DetectChunkCullingBounds.Manual;
				tilemapRenderer.chunkCullingBounds = offsetPadding + CalculateMaxTilePadding(tilemap);
			}
		}

		/// <summary>
		/// Largest extents of any painted tile's sprite plus its per-tile transform offset — the
		/// overhang beyond cell bounds that Auto chunk culling bounds detection would have covered.
		/// </summary>
		private static Vector3 CalculateMaxTilePadding(Tilemap tilemap)
		{
			Vector3 padding = Vector3.zero;
			foreach (Vector3Int cellPosition in tilemap.cellBounds.allPositionsWithin)
			{
				Sprite? sprite = tilemap.GetSprite(cellPosition);
				if (sprite == null) continue;

				Vector3 tileOffset = tilemap.GetTransformMatrix(cellPosition).GetPosition();
				Vector3 absoluteTileOffset = new Vector3(Mathf.Abs(tileOffset.x), Mathf.Abs(tileOffset.y), Mathf.Abs(tileOffset.z));
				padding = Vector3.Max(padding, sprite.bounds.extents + absoluteTileOffset);
			}
			return padding;
		}
	}
}
