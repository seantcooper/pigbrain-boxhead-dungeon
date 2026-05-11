#pragma warning disable UDR0001
namespace pigbrain.core.Geom
{
    public static class Hashing
    {
        public static int GetHashCode(this int[] arr)
        {
            unchecked
            {
                int hash = (int)2166136261; // FNV offset basis
                for (int i = 0; i < arr.Length; i++)
                {
                    hash ^= arr[i];
                    hash *= 16777619;        // FNV prime
                    hash = (hash << 13) | (hash >> 19); // cheap avalanche
                }
                return hash;
            }
        }
    }
}
