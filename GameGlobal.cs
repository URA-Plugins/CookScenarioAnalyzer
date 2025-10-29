using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CookScenarioAnalyzer.i18n.Game;

namespace CookScenarioAnalyzer
{
    public static partial class GameGlobal
    {
        #region LARC
        public static readonly int[] LArcTrainBonusEvery5Percent = [0, 5, 8, 10, 13, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 30, 31, 31, 32, 32, 33, 33, 34, 34, 35, 35, 36, 36, 37, 37, 38, 38, 39, 39, 40];
        public static readonly int[] LArcLessonMappingInv = [2, 5, 1, 4, 6, 3, 7, 8, 9, 10];
        public static readonly FrozenDictionary<int, string> LArcSSEffectNameFullColored = new Dictionary<int, string>
        {
            { 1, "技能hint" },
            { 3, "[#00ff00]体力[/]" },
            { 4, "[#00ffff]体力与上限[/]" },//最好的，用亮色
            { 5, "[#00ff00]心情体力[/]" },
            { 6, "充电" },
            { 7, "适性pt" },
            { 8, "[#00ff00]爱娇[/]" },
            { 9, "上手" },
            { 11, "属性" },
            { 12, "[#0000ff]技能点[/]" } //最烂的，用个深色
        }.ToFrozenDictionary();
        public static readonly FrozenDictionary<int, string> LArcSSEffectNameColoredShort = new Dictionary<int, string>
        {
            { 1, "技" },
            { 3, "[#00ff00]体[/]" },
            { 4, "[#00ffff]体[/]" },
            { 5, "[#00ff00]心[/]" },
            { 6, "充" },
            { 7, "适" },
            { 8, "[#ffff00]娇[/]" },
            { 9, "练" },
            { 11, "属" },
            { 12, "pt" },
        }.ToFrozenDictionary();
        public static readonly FrozenDictionary<int, string> LArcSSEffectNameColored = new Dictionary<int, string>
        {
            { 1, "技能" } ,
            { 3, "[#00ff00]体力[/]" },
            { 4, "[#00ffff]体力[/]" },
            { 5, "[#00ff00]心情[/]" },
            { 6, "[#ff00ff]充电[/]" },
            { 7, "适pt" },
            { 8, "[#ffff00]爱娇[/]" },
            { 9, "上手" },
            { 11, "属性" },
            { 12, "技pt" }
        }.ToFrozenDictionary();
        #endregion

        #region GM
        public static readonly FrozenDictionary<int, string> GrandMastersSpiritNamesColored = new Dictionary<int, string>
        {
            { 1, $"[red]{I18N_SpeedSimple}[/]" },
            { 2, $"[red]{I18N_StaminaSimple}[/]" },
            { 3, $"[red]{I18N_PowerSimple}[/]" },
            { 4, $"[red]{I18N_NutsSimple}[/]" },
            { 5, $"[red]{I18N_WizSimple}[/]" },
            { 6, $"[red]星[/]" },
            { 9, $"[blue]{I18N_SpeedSimple}[/]" },
            { 10, $"[blue]{I18N_StaminaSimple}[/]" },
            { 11, $"[blue]{I18N_PowerSimple}[/]" },
            { 12, $"[blue]{I18N_NutsSimple}[/]" },
            { 13, $"[blue]{I18N_WizSimple}[/]" },
            { 14, $"[blue]星[/]" },
            { 17, $"[yellow]{I18N_SpeedSimple}[/]" },
            { 18, $"[yellow]{I18N_StaminaSimple}[/]" },
            { 19, $"[yellow]{I18N_PowerSimple}[/]" },
            { 20, $"[yellow]{I18N_NutsSimple}[/]" },
            { 21, $"[yellow]{I18N_WizSimple}[/]" },
            { 22, $"[yellow]星[/]" }
        }.ToFrozenDictionary();
        #endregion

        #region Cook
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
        public static FrozenDictionary<int, string> CookEffectName = new Dictionary<int, string>
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
        #endregion
    }
}
