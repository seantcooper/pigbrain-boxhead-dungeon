// AUTO-GENERATED

namespace pigbrain.generated
{
    public enum GameLayer
    {
        Default = 0,
        TransparentFX = 1,
        Ignore_Raycast = 2,
        Reserved = 3,
        Water = 4,
        UI = 5,
        UIWorld = 6,
        Team1 = 8,
        Team2 = 9,
        Team3 = 10,
        Team4 = 11,
        Terrain = 16,
        Wall = 17,
        Furniture = 18,
        Ceiling = 19,
        Void = 20,
        FX = 24,
        Camera = 25,
        WorldPP = 26,
        UIPP = 27,
    }
}


// AUTO-GENERATED

namespace pigbrain.generated
{
    [System.Flags]
    public enum GameLayerFlags
    {
        None = 0,
        Default = 1 << 0,
        TransparentFX = 1 << 1,
        Ignore_Raycast = 1 << 2,
        Reserved = 1 << 3,
        Water = 1 << 4,
        UI = 1 << 5,
        UIWorld = 1 << 6,
        Team1 = 1 << 8,
        Team2 = 1 << 9,
        Team3 = 1 << 10,
        Team4 = 1 << 11,
        Terrain = 1 << 16,
        Wall = 1 << 17,
        Furniture = 1 << 18,
        Ceiling = 1 << 19,
        Void = 1 << 20,
        FX = 1 << 24,
        Camera = 1 << 25,
        WorldPP = 1 << 26,
        UIPP = 1 << 27,
        All = ~0
    }
}
