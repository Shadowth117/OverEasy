using Godot.Collections;

namespace OverEasy.TextInfo
{
    public class LightingMapping
    {
        /// <summary>
        /// First value is day, second is night. These are hardcoded to the world name for regular stages while battle stages are just hardcoded to 2 particular ones.
        /// Boss stages are a guess for now
        /// </summary>
        public static Dictionary<string, int[]> LightIdMapping = new Dictionary<string, int[]>()
        {
            { "green", new int[]{ 1, 11 } },
            { "blue", new int[]{ 2, 12 } },
            { "red", new int[]{ 3, 13 } },
            { "purple", new int[]{ 4, 14 } },
            { "orange", new int[]{ 5, 15 } },
            { "yellow", new int[]{ 35, 16 } },
            { "last", new int[]{ 7, 17 } },
            { "yellow_underground", new int[]{ 6, 6 } },
            { "battle_green", new int[]{ 121, 121 } },
            { "battle_green2", new int[]{ 121, 121 } },
            { "battle_blue", new int[]{ 121, 121 } },
            { "battle_last", new int[]{ 121, 121 } },
            { "battle_red", new int[]{ 0, 0 } },
            { "battle_purple", new int[]{ 0, 0 } },
            { "battle_orange", new int[]{ 0, 0 } },
            { "battle_yellow", new int[]{ 0, 0 } },
            { "blueboss", new int[]{ 2, 12 } },
            { "redboss", new int[]{ 3, 13 } },
            { "purpleboss", new int[]{ 4, 14 } },
            { "orangeboss", new int[]{ 5, 15 } },
            { "yellowboss", new int[]{ 35, 16 } },
            { "lastboss", new int[]{ 7, 17 } },
            { "lastboss2", new int[]{ 7, 17 } },
            { "greenboss", new int[]{ 1, 11 } },
            { "title", new int[]{ 1, 11 } },
        };
    }
}
