// AUTO-GENERATED

namespace pigbrain.generated
{
    public enum NavArea
    {
        Walkable = 0,
        Not_Walkable = 1,
        Jump = 2,
        Not_Flat = 3,
        Player_Only = 4,
        Danger = 5,
        Enemy_Only = 6,
        x2 = 31,
    }

    public static class NavAreaCosts
    {
        public const float Walkable = 1f;
        public const float Not_Walkable = 1f;
        public const float Jump = 2f;
        public const float Not_Flat = 3f;
        public const float Player_Only = 1f;
        public const float Danger = 1f;
        public const float Enemy_Only = 1f;
        public const float x2 = 2f;
    }
}


// AUTO-GENERATED

namespace pigbrain.generated
{
    [System.Flags]
    public enum NavAreaFlags
    {
        None = 0,
        Walkable = 1 << 0,
        Not_Walkable = 1 << 1,
        Jump = 1 << 2,
        Not_Flat = 1 << 3,
        Player_Only = 1 << 4,
        Danger = 1 << 5,
        Enemy_Only = 1 << 6,
        x2 = 1 << 31,
        All = ~0
    }
}
