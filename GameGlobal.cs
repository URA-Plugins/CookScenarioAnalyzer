using System;
using System.Collections.Frozen;
using static CookScenarioAnalyzer.i18n.Game;

namespace CookScenarioAnalyzer
{
    public static partial class GameGlobal
    {
        public static readonly string[] CookMaterialName = [I18N_Carrot, I18N_Garlic, I18N_Potato, I18N_Chili, I18N_Berry];
        public static readonly string[] CookSuccessEffect = ["体力+10", "心情+1", "羁绊+3", "分身+1", "体力上限+4"];
        public static readonly int[] CookGardenLevelUpCost = [0, 100, 180, 220, 250, 9999];
        public static readonly int[] CookGardenBaseHarvest = [20, 20, 30, 40, 40];
        public static readonly FrozenDictionary<int, string> CookDishName = new Dictionary<int, string>
        {
            { 1, "三明治" },
            { 2, "咖喱" },
            { 3, "速度II" },
            { 4, "耐力II" },
            { 5, "力量II" },
            { 6, "根性II" },
            { 7, "智力II" },
            { 8, "速度II+1" },
            { 9, "耐力II+1" },
            { 10, "力量II+1" },
            { 11, "根性II+1" },
            { 12, "智力II+1" },
            { 13, "速度II+2" },
            { 14, "耐力II+2" },
            { 15, "力量II+2" },
            { 16, "根性II+2" },
            { 17, "智力II+2" },
            { 18, "速度III" },
            { 19, "耐力III" },
            { 20, "力量III" },
            { 21, "根性III" },
            { 22, "智力III" },
            { 23, "速度III+1" },
            { 24, "耐力III+1" },
            { 25, "力量III+1" },
            { 26, "根性III+1" },
            { 27, "智力III+1" },
            { 28, "速度III+2" },
            { 29, "耐力III+2" },
            { 30, "力量III+2" },
            { 31, "根性III+2" },
            { 32, "智力III+2" },
            { 33, "GI拼盘" },
            { 34, "GI拼盘+1" },
            { 35, "超满足GI拼盘+1" }
        }.ToFrozenDictionary();
        public static readonly FrozenDictionary<int, string> CookEffectName = new Dictionary<int, string>
        {
            { 2, "训练" },
            { 21, "赛后" },
            { 201, "体力" },
            { 202, "心情" },
            { 203, "羁绊" },
            { 204, "分身" },
            { 205, "上限" },
            { 206, "得意" },
            { 207, "PT" },
            { 208, "粉丝" }
        }.ToFrozenDictionary();
    }
}
