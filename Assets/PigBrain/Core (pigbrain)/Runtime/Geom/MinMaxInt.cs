namespace pigbrain.core.Geom
{
    [System.Serializable]
    public struct MinMaxInt
    {
        public int min, max;
        public readonly int mid => (max + min) / 2;
        public MinMaxInt(int min, int max)
        {
            this.min = min;
            this.max = max;
        }
        public readonly bool InRange(int i) => i >= min && i <= max;
        public readonly int range => max - min;
    }

    public static class MinMaxIntExtensions
    {
        public static int NextInt(this Rnd rnd, MinMaxInt r) =>
            rnd.NextInt(r.min, r.max + 1);
    }

}