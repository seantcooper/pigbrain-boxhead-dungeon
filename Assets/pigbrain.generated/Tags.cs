// AUTO-GENERATED

namespace pigbrain.generated
{
    public enum Tag
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
        RecorderCamera = 13,
    }
}


// AUTO-GENERATED

namespace pigbrain.generated
{
    [System.Flags]
    public enum TagMask
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
        RecorderCamera = 1 << 13,
        All = ~0
    }
}


// AUTO-GENERATED

namespace pigbrain.generated
{
    public static class TagFlags
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
            "RecorderCamera",
        };

        public static readonly System.Collections.Generic.Dictionary<string, Tag> Lookup =
            new System.Collections.Generic.Dictionary<string, Tag>()
            {
                { "Untagged", Tag.Untagged },
                { "Death", Tag.Death },
                { "Chicken", Tag.Chicken },
                { "Food", Tag.Food },
                { "Rooster", Tag.Rooster },
                { "Wolf", Tag.Wolf },
                { "Chick", Tag.Chick },
                { "FoodSpawn", Tag.FoodSpawn },
                { "RoosterMain", Tag.RoosterMain },
                { "Egg", Tag.Egg },
                { "Target", Tag.Target },
                { "FX", Tag.FX },
                { "Effect", Tag.Effect },
                { "Background", Tag.Background },
                { "RecorderCamera", Tag.RecorderCamera },
            };


        public static readonly System.Collections.Generic.Dictionary<Tag, string> ReverseLookup =
            new System.Collections.Generic.Dictionary<Tag, string>()
            {
                { Tag.Untagged, "Untagged" },
                { Tag.Death, "Death" },
                { Tag.Chicken, "Chicken" },
                { Tag.Food, "Food" },
                { Tag.Rooster, "Rooster" },
                { Tag.Wolf, "Wolf" },
                { Tag.Chick, "Chick" },
                { Tag.FoodSpawn, "FoodSpawn" },
                { Tag.RoosterMain, "RoosterMain" },
                { Tag.Egg, "Egg" },
                { Tag.Target, "Target" },
                { Tag.FX, "FX" },
                { Tag.Effect, "Effect" },
                { Tag.Background, "Background" },
                { Tag.RecorderCamera, "RecorderCamera" },
            };

    }
}
