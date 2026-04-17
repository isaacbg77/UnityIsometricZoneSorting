using System;

namespace YoWorld.Core.Sorting
{
    /// <summary>
    /// Encodes which side of each sorting line a zone falls on.
    /// Each element corresponds to a sorting line by index: true means the zone
    /// is on that line's front side (closer to camera), false means the back side.
    /// Two zones with the same signature are the same zone.
    /// </summary>
    public sealed class ZoneSignature : IEquatable<ZoneSignature>
    {
        private readonly bool[] _sides;

        public int LineCount => _sides.Length;

        public ZoneSignature(bool[] sides)
        {
            _sides = sides;
        }

        public bool IsOnFrontSide(int lineIndex)
        {
            return _sides[lineIndex];
        }

        /// <summary>
        /// Returns the index of the single differing line, or -1 if signatures
        /// differ by zero or more than one line.
        /// </summary>
        public int FindSingleDifferingLine(ZoneSignature other)
        {
            var differingIndex = -1;
            var differCount = 0;

            for (var index = 0; index < _sides.Length && index < other._sides.Length; index++)
            {
                if (_sides[index] != other._sides[index])
                {
                    differingIndex = index;
                    differCount++;
                    if (differCount > 1) return -1;
                }
            }

            return differCount == 1 ? differingIndex : -1;
        }

        /// <summary>
        /// Returns how many lines match between this and another signature.
        /// </summary>
        public int CountMatches(ZoneSignature other)
        {
            var count = 0;
            var length = Math.Min(_sides.Length, other._sides.Length);
            for (var index = 0; index < length; index++)
            {
                if (_sides[index] == other._sides[index]) count++;
            }
            return count;
        }

        #region IEquatable

        public bool Equals(ZoneSignature? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (_sides.Length != other._sides.Length) return false;

            for (var index = 0; index < _sides.Length; index++)
            {
                if (_sides[index] != other._sides[index]) return false;
            }

            return true;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ZoneSignature);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var side in _sides)
            {
                hash.Add(side);
            }
            return hash.ToHashCode();
        }

        public static bool operator ==(ZoneSignature? left, ZoneSignature? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(ZoneSignature? left, ZoneSignature? right)
        {
            return !(left == right);
        }

        #endregion // IEquatable
    }
}
