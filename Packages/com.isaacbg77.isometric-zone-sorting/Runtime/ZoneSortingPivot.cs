using UnityEngine;

namespace IsometricZoneSorting
{
	/// <summary>
	/// Defines an isometric plane origin. Replaces the dual-point line system with
	/// a single point and a configurable isometric angle.
	/// </summary>
	public class ZoneSortingPivot : MonoBehaviour
	{
		private const float DEFAULT_ISOMETRIC_ANGLE = 26.565f;

		public struct SortingAxes
		{
			public Vector2 RightAxis;
			public Vector2 LeftAxis;
			public float IsometricAngle;
			public float RightVectorLength;
			public float LeftVectorLength;

			public SortingAxes(Vector2 rightAxis, Vector2 leftAxis, float isometricAngle, float rightVectorLength, float leftVectorLength)
			{
				RightAxis = rightAxis;
				LeftAxis = leftAxis;
				IsometricAngle = isometricAngle;
				RightVectorLength = rightVectorLength;
				LeftVectorLength = leftVectorLength;
			}

			public static SortingAxes Default()
			{
				return new SortingAxes(new Vector2(1, 0), new Vector2(-1, 0), DEFAULT_ISOMETRIC_ANGLE, 1f, 1f);
			}
		}

		[SerializeField, Tooltip("Angle in degrees relative to the X-axis for the isometric grid.")]
		private float _isometricAngle = DEFAULT_ISOMETRIC_ANGLE;

		[SerializeField, Tooltip("Length of the right V vector arm."), Min(0.01f)]
		private float _rightVectorLength = 1f;

		[SerializeField, Tooltip("Length of the left V vector arm."), Min(0.01f)]
		private float _leftVectorLength = 1f;

		public Vector2 Position => transform.position;

		/// <summary>
		/// Gets the two directions forming the isometric 'V' or 'X' shape.
		/// </summary>
		public SortingAxes GetSortingAxes()
		{
			float rad = _isometricAngle * Mathf.Deg2Rad;
			float cos = Mathf.Cos(rad);
			float sin = Mathf.Sin(rad);

			// Vector A: (cos, sin) - Right-Up
			// Vector B: (-cos, sin) - Left-Up
			Vector2 dirA = new Vector2(cos, sin);
			Vector2 dirB = new Vector2(-cos, sin);

			return new SortingAxes(dirA, dirB, _isometricAngle, _rightVectorLength, _leftVectorLength);
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			var pos = (Vector3)Position;
			var sortingAxes = GetSortingAxes();

			float rightLength = _rightVectorLength;
			float leftLength = _leftVectorLength;

			// Front-facing normals (both point downward)
			Vector2 normA = new Vector2(sortingAxes.RightAxis.y, -sortingAxes.RightAxis.x);
			Vector2 normB = new Vector2(-sortingAxes.LeftAxis.y, sortingAxes.LeftAxis.x);

			// Draw length-capped V vectors.
			Gizmos.color = Color.magenta;
			Gizmos.DrawRay(pos, (Vector3)(sortingAxes.RightAxis * rightLength));
			Gizmos.DrawRay(pos, (Vector3)(sortingAxes.LeftAxis * leftLength));

			// Draw horizontal lines from each arm's tip
			Vector3 rightTip = pos + (Vector3)(sortingAxes.RightAxis * rightLength);
			Vector3 leftTip = pos + (Vector3)(sortingAxes.LeftAxis * leftLength);
			const float horizontalExtentLength = 0.5f;
			Gizmos.DrawLine(rightTip, rightTip + Vector3.right * horizontalExtentLength);
			Gizmos.DrawLine(leftTip, leftTip + Vector3.left * horizontalExtentLength);

			// Draw normals
			Gizmos.color = Color.cyan;
			const float normalLength = 0.1f;
			Gizmos.DrawRay(pos + (Vector3)(sortingAxes.RightAxis * rightLength * .5f), (Vector3)normA * normalLength);
			Gizmos.DrawRay(pos + (Vector3)(sortingAxes.LeftAxis * leftLength * .5f), (Vector3)normB * normalLength);

			// Pivot point
			Gizmos.color = Color.white;
			Gizmos.DrawSphere(pos, 0.1f);
		}
#endif
	}
}
