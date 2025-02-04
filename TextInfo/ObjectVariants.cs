using System.Collections.Generic;

namespace OverEasy.TextInfo
{
    public class ObjectVariants
    {
        public static List<string> fruits = new()
        {
            "(Apple)",
            "(Banana)",
            "(Cherry)",
            "(Melon)",
            "(Pineapple)",
            "(Strawberry)",
            "(Watermelon)",
        };

        public static List<string> zakoCrows = new()
        {
            "Dora (Cat)",
            "Gelly (Snake)",
            "Gray (Pufferfish)",
            "Anter (Ant)",
            "Batter (Bat)"
        };

        public static Dictionary<string, string> enemyFileMap = new()
        {
            {"ar_ene_am02.arc", "102" },
            {"ar_ene_ants_queen.arc", "106" },
            {"ar_ene_armadillo.arc", "10A"},
            {"ar_ene_bat.arc", "108"},
            {"ar_ene_bee.arc", "104"},
            {"ar_ene_blue_boss.arc", "200"},
            {"ar_ene_frog.arc", "109" },
            {"ar_ene_green_boss.arc", "900"},
            {"ar_ene_green_demo.arc", "600"},
            {"ar_ene_last_boss.arc", "700"},
            {"ar_ene_last_ex_boss.arc", "800"},
            {"ar_ene_lizard.arc", "105"},
            {"ar_ene_orange_boss.arc", "500"},
            {"ar_ene_orange_boss_map.arc", "500_Map"},
            {"ar_ene_penguin.arc", "107"},
            {"ar_ene_purple_boss.arc", "400"},
            {"ar_ene_red_boss.arc", "300"},
            {"ar_ene_snake.arc", "103"},
            {"ar_ene_yellow_boss.arc", "600"},
            {"ar_ene_yellow_boss_green.arc", "600_B"},
            {"ar_ene_zako_ant.arc", "101_3"},
            {"ar_ene_zako_bat.arc", "101_4"},
            {"ar_ene_zako_cat.arc", "101_0"},
            {"ar_ene_zako_fugu.arc", "101_2"},
            {"ar_ene_zako_snake.arc", "101_1"},
        };
    }
}
