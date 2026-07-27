using Gallop;
using UmamusumeResponseAnalyzer.TerminalGui;
using static CookScenarioAnalyzer.i18n.Game;

namespace CookScenarioAnalyzer;

public static class Handlers
{
    public static WorkspaceContent ParseCookCommandInfo(
        SingleModeCookCheckEventResponse response,
        ref int currentTurn)
    {
        var importantRows = new List<string>();
        var trainingRows = new List<string>();
        var extraRows = new List<string>();
        var turn = new TurnInfoCook(response.data);
        var eventCookDataset = response.data.cook_data_set;

        if (currentTurn != turn.Turn - 1
            && currentTurn != turn.Turn
            && turn.Turn != 1)
        {
            importantRows.Add(string.Format(I18N_WrongTurnAlert, currentTurn, turn.Turn));
        }

        if (response.data.chara_info.playing_state != 1)
            importantRows.Add(I18N_RepeatTurn);
        else
            currentTurn = turn.Turn;

        var trainItems = new Dictionary<int, SingleModeCommandInfo>
        {
            [101] = response.data.home_info.command_info_array[0],
            [105] = response.data.home_info.command_info_array[1],
            [102] = response.data.home_info.command_info_array[2],
            [103] = response.data.home_info.command_info_array[3],
            [106] = response.data.home_info.command_info_array[4]
        };
        var trainStats = new TrainStats[5];
        var failureRates = new int[5];
        var currentFiveValue = new[]
        {
            response.data.chara_info.speed,
            response.data.chara_info.stamina,
            response.data.chara_info.power,
            response.data.chara_info.guts,
            response.data.chara_info.wiz
        };
        var totalValue = currentFiveValue.Select(ScoreUtils.ReviseOver1200).Sum();

        for (var i = 0; i < 5; i++)
        {
            var trainId = GameGlobal.TrainIds[i];
            failureRates[i] = trainItems[trainId].failure_rate;
            var trainParams = new Dictionary<int, int>
            {
                [1] = 0,
                [2] = 0,
                [3] = 0,
                [4] = 0,
                [5] = 0,
                [30] = 0,
                [10] = 0
            };
            foreach (var item in turn.GetCommonResponse().home_info.command_info_array)
            {
                if (!GameGlobal.ToTrainId.TryGetValue(item.command_id, out var value) || value != trainId)
                    continue;

                foreach (var trainParam in item.params_inc_dec_info_array)
                    trainParams[trainParam.target_type] += trainParam.value;
            }

            var stats = new TrainStats
            {
                VitalGain = trainParams[10],
                FiveValueGain = [trainParams[1], trainParams[2], trainParams[3], trainParams[4], trainParams[5]],
                PtGain = trainParams[30]
            };
            if (turn.Vital + stats.VitalGain > turn.MaxVital)
                stats.VitalGain = turn.MaxVital - turn.Vital;
            if (stats.VitalGain < -turn.Vital)
                stats.VitalGain = -turn.Vital;

            var cookValueGainUpper = eventCookDataset.command_info_array
                .FirstOrDefault(x => x.command_id == trainId || x.command_id == GameGlobal.XiahesuIds[trainId])
                ?.params_inc_dec_info_array;
            if (cookValueGainUpper is not null)
            {
                foreach (var item in cookValueGainUpper)
                {
                    if (item.target_type == 30)
                        stats.PtGain += item.value;
                    else if (item.target_type <= 5)
                        stats.FiveValueGain[item.target_type - 1] += item.value;
                }
            }

            for (var j = 0; j < 5; j++)
            {
                stats.FiveValueGain[j] =
                    ScoreUtils.ReviseOver1200(turn.Stats[j] + stats.FiveValueGain[j])
                    - ScoreUtils.ReviseOver1200(turn.Stats[j]);
            }

            trainStats[i] = stats;
        }

        var bestScore = trainStats.Max(x => x.FiveValueGain.Sum());
        foreach (var command in turn.CommandInfoArray)
        {
            var index = command.TrainIndex - 1;
            var failureRate = failureRates[index];
            var trainName = command.TrainIndex switch
            {
                1 => I18N_Speed,
                2 => I18N_Stamina,
                3 => I18N_Power,
                4 => I18N_Nuts,
                5 => I18N_Wiz,
                _ => throw new InvalidOperationException($"未知训练索引: {command.TrainIndex}")
            };
            trainingRows.Add($"[{trainName}{(failureRate > 0 ? $" ({failureRate}%)" : string.Empty)}]");

            var currentStat = turn.StatsRevised[index];
            var statUpToMax = turn.MaxStatsRevised[index] - currentStat;
            trainingRows.Add($"  {I18N_CurrentRemainStat} {currentStat}:{statUpToMax}");

            var afterVital = trainStats[index].VitalGain + turn.Vital;
            trainingRows.Add($"  {I18N_Vital}: {afterVital}/{turn.MaxVital}");

            var commandMaterial = turn.CommandMaterials.FirstOrDefault(x => x.command_id == command.CommandId);
            var totalMaterialsBeforeClick = turn.Harvests.Sum(x => x.harvest_num);
            var totalMaterials = commandMaterial?.material_harvest_info_array.Sum(x => x.harvest_num)
                - totalMaterialsBeforeClick ?? 0;
            trainingRows.Add($"  Lv{command.TrainLevel} | 材+{totalMaterials}");

            var stats = trainStats[index];
            var score = stats.FiveValueGain.Sum();
            trainingRows.Add($"  {(score == bestScore ? "★ " : string.Empty)}{I18N_StatSimple}: {score} | Pt: {stats.PtGain}");

            foreach (var trainingPartner in command.TrainingPartners)
                trainingRows.Add($"  {(trainingPartner.Shining ? "★ " : string.Empty)}{trainingPartner.Name}");

            var matText = GameGlobal.CookMaterialName[index];
            var matCurrent = turn.Materials[index].num;
            var matMax = turn.Facilities[index].facility_level * 200;
            var matHarvestBeforeClick = turn.Harvests[index].harvest_num;
            var matHarvest = commandMaterial?.material_harvest_info_array[index].harvest_num
                ?? matHarvestBeforeClick;
            var materialState = matCurrent + matHarvest switch
            {
                var value when value >= matMax => "（已满）",
                var value when value > matMax * 0.85 => "（接近上限）",
                _ => string.Empty
            };
            trainingRows.Add($"  {matText}: {matCurrent}/{matMax}{materialState}");
            trainingRows.Add($"  {I18N_Harvest}: +{matHarvestBeforeClick}/+{matHarvest - matHarvestBeforeClick}");
            trainingRows.Add(string.Empty);
        }

        if (turn.Facilities.Any(x =>
                x.facility_id != 200
                && GameGlobal.CookGardenLevelUpCost[x.facility_level] <= eventCookDataset.cook_info.care_point))
        {
            importantRows.Add("田地可升级");
        }
        if (response.data.chara_info.chara_effect_id_array.Contains(32))
            importantRows.Add("【休息的心得】生效中");

        extraRows.Add($"农田Pt: {eventCookDataset.cook_info.care_point} (+{eventCookDataset.care_point_gain_num})");
        extraRows.Add($"料理Pt: {eventCookDataset.cook_info.cooking_friends_power}");
        extraRows.Add(string.Empty);
        foreach (var item in turn.CommandMaterials)
        {
            var actionName = item.command_type switch
            {
                3 => "出行",
                4 => "比赛",
                7 => "休息",
                8 => "治疗",
                _ => null
            };
            if (actionName is null)
                continue;

            var matHarvest = item.material_harvest_info_array[item.material_id / 100 - 1].harvest_num;
            var matHarvestBeforeClick = turn.Harvests[item.material_id / 100 - 1].harvest_num;
            var matText = GameGlobal.CookMaterialName[item.material_id / 100 - 1];
            extraRows.Add($"{(item.boost_type is 2 or 4 ? "★ " : string.Empty)}{actionName}: {matText} +{matHarvest - matHarvestBeforeClick}");
        }

        if (eventCookDataset.dish_info is null)
        {
            if (eventCookDataset.cook_info.cooking_success_point >= eventCookDataset.cook_info.cooking_success_base_point
                || eventCookDataset.cooking_success_rate == 100)
            {
                extraRows.Add("必定大成功");
            }
            else
            {
                extraRows.Add($"料理成功率: {eventCookDataset.cooking_success_rate}%");
                extraRows.Add($"进度 {eventCookDataset.cook_info.cooking_success_point}/{eventCookDataset.cook_info.cooking_success_base_point}");
            }
        }
        else
        {
            var dish = eventCookDataset.dish_info;
            var dishName = GameGlobal.CookDishName[dish.dish_id]
                + (dish.cooking_result_state == 2 ? " HQ" : string.Empty);
            extraRows.Add($"当前料理: {dishName}");
            extraRows.Add("效果:");
            foreach (var item in dish.dish_effect_info_array)
                extraRows.Add($"{GameGlobal.CookEffectName[item.effect_type]}+{item.effect_value_1}");

            if (dish.dish_id <= 2)
                extraRows.Add("羁绊+2");

            if (eventCookDataset.success_effect_id_array is not null)
            {
                foreach (var effectId in eventCookDataset.success_effect_id_array)
                {
                    if (effectId <= 15)
                    {
                        extraRows.Add(GameGlobal.CookSuccessEffect[(effectId - 1) % 5]);
                    }
                    else
                    {
                        extraRows.Add("心情+1, 分身+2");
                        break;
                    }
                }
            }
        }

        if (turn.Turn >= 69)
        {
            extraRows.Add(string.Empty);
            var isCooked = eventCookDataset.dish_info is not null;
            var turnRemainUra = Math.Min(6, 78 - turn.Turn);
            var g1PlateNeeded = Math.Min(6, turnRemainUra + (isCooked ? 0 : 1));
            var turnRemainNormal = 72 - turn.Turn + 1;
            if (turn.Turn == 72 && eventCookDataset.care_history_info_array.Length == 0)
                --turnRemainNormal;

            extraRows.Add($"剩 {g1PlateNeeded} 个GI料理");
            extraRows.Add("食材溢出量：");
            var normalRate = turnRemainNormal > 0 ? 160.0 / eventCookDataset.care_point_gain_num : 0.0;
            for (var i = 0; i < 5; ++i)
            {
                var baseHarvest = GameGlobal.CookGardenBaseHarvest[turn.Facilities[i].facility_level - 1];
                var normalHarvest = (int)Math.Floor(turn.Harvests[i].harvest_num * normalRate);
                var uraBaseHarvest = (int)Math.Floor(baseHarvest * 0.75);
                var materialRemain =
                    turn.Materials[i].num
                    + normalHarvest
                    + uraBaseHarvest * turnRemainUra
                    - g1PlateNeeded * 80;
                extraRows.Add($"{GameGlobal.CookMaterialName[i]}: {materialRemain:+#;-#;0}");
            }
        }

        var motivation = response.data.chara_info.motivation switch
        {
            5 => I18N_MotivationBest,
            4 => I18N_MotivationGood,
            3 => I18N_MotivationNormal,
            2 => I18N_MotivationBad,
            1 => I18N_MotivationWorst,
            _ => throw new InvalidOperationException($"未知干劲值: {response.data.chara_info.motivation}")
        };
        if (response.data.home_info.command_info_array.Count(x => x.is_enable == 1) <= 1)
            importantRows.Add("非训练回合");

        var lines = new List<string>
        {
            $"{turn.Year}{I18N_Year} {turn.Month}{I18N_Month}{turn.HalfMonth} | 总属性: {totalValue} | {I18N_Vital}: {turn.Vital}/{turn.MaxVital} | {motivation}"
        };
        if (importantRows.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("== 重要信息 ==");
            lines.AddRange(importantRows);
        }
        lines.Add(string.Empty);
        lines.Add("== 训练信息 ==");
        lines.AddRange(trainingRows);
        lines.Add("== Extras ==");
        lines.AddRange(extraRows);
        return WorkspaceContent.Text(string.Join(Environment.NewLine, lines));
    }
}
