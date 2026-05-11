namespace pigbrain.core.Geom
{
    [System.Serializable]
    public struct MinMaxFloat
    {
        public float min, max;
        public MinMaxFloat(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
        public float mid => (max + min) / 2;
        public bool InRange(float i) => i >= min && i <= max;
        public float range => max - min;
    }

    public static class MinMaxFloatExtensions
    {
        public static float NextFloat(this Rnd rnd, MinMaxFloat r) =>
            rnd.NextFloat(r.min, r.max);
    }
}
