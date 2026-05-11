// AUTO-GENERATED

namespace pigbrain.generated
{
    public enum GameTag
    {
        Untagged = -1,
        Death = 0,
        Chicken = 1,
        Food = 2,
        Rooster = 3,
        Wolf = 4,
        Chick = 5,
        FoodSpawn = 6,
        RoosterMain = 7,
        Egg = 8,
        Target = 9,
        FX = 10,
        Effect = 11,
        Background = 12,
    }
}


// AUTO-GENERATED

namespace pigbrain.generated
{
    [System.Flags]
    public enum GameTagMask
    {
        Untagged = 0,
        Death = 1 << 0,
        Chicken = 1 << 1,
        Food = 1 << 2,
        Rooster = 1 << 3,
        Wolf = 1 << 4,
        Chick = 1 << 5,
        FoodSpawn = 1 << 6,
        RoosterMain = 1 << 7,
        Egg = 1 << 8,
        Target = 1 << 9,
        FX = 1 << 10,
        Effect = 1 << 11,
        Background = 1 << 12,
        All = ~0
    }
}


// AUTO-GENERATED

namespace pigbrain.generated
{
    public static class GameTags
    {
        public static readonly string[] AllTags = new string[]
        {
            "Untagged",
            "Death",
            "Chicken",
            "Food",
            "Rooster",
            "Wolf",
            "Chick",
            "FoodSpawn",
            "RoosterMain",
            "Egg",
            "Target",
            "FX",
            "Effect",
            "Background",
        };

        public static readonly System.Collections.Generic.Dictionary<string, GameTag> Lookup =
            new System.Collections.Generic.Dictionary<string, GameTag>()
            {
                { "Untagged", GameTag.Untagged },
                { "Death", GameTag.Death },
                { "Chicken", GameTag.Chicken },
                { "Food", GameTag.Food },
                { "Rooster", GameTag.Rooster },
                { "Wolf", GameTag.Wolf },
                { "Chick", GameTag.Chick },
                { "FoodSpawn", GameTag.FoodSpawn },
                { "RoosterMain", GameTag.RoosterMain },
                { "Egg", GameTag.Egg },
                { "Target", GameTag.Target },
                { "FX", GameTag.FX },
                { "Effect", GameTag.Effect },
                { "Background", GameTag.Background },
            };


        public static readonly System.Collections.Generic.Dictionary<GameTag, string> ReverseLookup =
            new System.Collections.Generic.Dictionary<GameTag, string>()
            {
                { GameTag.Untagged, "Untagged" },
                { GameTag.Death, "Death" },
                { GameTag.Chicken, "Chicken" },
                { GameTag.Food, "Food" },
                { GameTag.Rooster, "Rooster" },
                { GameTag.Wolf, "Wolf" },
                { GameTag.Chick, "Chick" },
                { GameTag.FoodSpawn, "FoodSpawn" },
                { GameTag.RoosterMain, "RoosterMain" },
                { GameTag.Egg, "Egg" },
                { GameTag.Target, "Target" },
                { GameTag.FX, "FX" },
                { GameTag.Effect, "Effect" },
                { GameTag.Background, "Background" },
            };

    }
}
