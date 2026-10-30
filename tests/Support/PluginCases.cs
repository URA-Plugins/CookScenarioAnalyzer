using UmamusumeResponseAnalyzer.PluginTesting;
using System.IO.Compression;
using System.Text;
using Gallop;
using Gallop.Endpoints;
using Newtonsoft.Json;
using UmamusumeResponseAnalyzer;
using UmamusumeResponseAnalyzer.Entities;
using UmamusumeResponseAnalyzer.TerminalGui;
using HostSkillData = UmamusumeResponseAnalyzer.Entities.SkillData;

static partial class PluginCases
{
    static readonly int[] BaseTrainingIds = [101, 105, 102, 103, 106];

    public static PluginCase Case { get; } =
        new(
            "CookScenarioAnalyzer",
            static () => new CookScenarioAnalyzer.CookScenarioAnalyzer(),
            new(
                static () => CreateCookTrainingResponse(),
                "CookScenarioAnalyzer",
                "training",
                "训练分析",
                FullBleed: true,
                VisibleText: "== 训练信息 ==",
                CreateUpdatedResponse: static () => CreateCookTrainingResponse(73),
                InitialUpdateText: "80/100",
                UpdatedVisibleText: "63/100",
                History: new(
                    static (vital, charaId, turn) => CreateCookTrainingResponse(vital, charaId, turn))));

    static SingleModeCookCheckEventResponse CreateCookTrainingResponse(
        int vital = 80,
        int singleModeCharaId = 1001,
        int turn = 1)
    {
        var response = (SingleModeCookCheckEventResponse)RandomDtoGenerator.Create(
            typeof(SingleModeCookCheckEventResponse),
            "CookScenarioAnalyzer:valid", RandomDtoProfile.Analyzer);
        response.data.chara_info = CreateChara(
            8,
            vital: vital,
            singleModeCharaId: singleModeCharaId,
            turn: turn);
        response.data.home_info = CreateHomeInfo();
        response.data.unchecked_event_array = [];
        response.data.select_index_info_array = [];
        response.data.race_start_info = null!;
        response.data.cook_data_set = new()
        {
            cook_info = new()
            {
                care_point = 50,
                cooking_friends_power = 100,
                cooking_success_point = 10,
                cooking_success_base_point = 100
            },
            command_info_array =
            [
                .. BaseTrainingIds.Select((commandId, index) => new SingleModeCookCommandInfo
                {
                    command_type = 1,
                    command_id = commandId,
                    params_inc_dec_info_array = CreateParams(index + 1)
                })
            ],
            material_info_array =
            [
                .. Enumerable.Range(1, 5).Select(index => new SingleModeCookMaterialInfo
                {
                    material_id = index * 100,
                    num = 100
                })
            ],
            material_harvest_info_array =
            [
                .. Enumerable.Range(1, 5).Select(index => new SingleModeCookMaterialHarvestInfo
                {
                    material_id = index * 100,
                    harvest_num = 20
                })
            ],
            care_history_info_array = [],
            dish_info = null!,
            facility_info_array =
            [
                .. Enumerable.Range(1, 5).Select(index => new SingleModeCookFacilityInfo
                {
                    facility_id = index * 100,
                    facility_level = 1
                })
            ],
            cooking_success_rate = 50,
            cook_result_info_array = [],
            success_effect_id_array = [],
            care_point_gain_num = 10,
            command_material_care_info_array = []
        };
        return response;
    }

    static SingleModeChara CreateChara(
        int scenarioId,
        int[]? trainIds = null,
        int vital = 80,
        int singleModeCharaId = 1001,
        int turn = 1) => new()
    {
        single_mode_chara_id = singleModeCharaId,
        card_id = 100100,
        state = 1,
        playing_state = 1,
        scenario_id = scenarioId,
        turn = turn,
        motivation = 5,
        vital = vital,
        max_vital = 100,
        speed = 500,
        stamina = 500,
        power = 500,
        guts = 500,
        wiz = 500,
        max_speed = 1200,
        max_stamina = 1200,
        max_power = 1200,
        max_guts = 1200,
        max_wiz = 1200,
        skill_point = 1200,
        skill_array = [],
        skill_tips_array = [],
        support_card_array = [],
        evaluation_info_array = [],
        training_level_info_array =
        [
            .. (trainIds ?? BaseTrainingIds).Select(commandId => new TrainingLevelInfo
            {
                command_id = commandId,
                level = 1
            })
        ],
        chara_effect_id_array = [],
        proper_distance_short = 1,
        proper_distance_mile = 1,
        proper_distance_middle = 1,
        proper_distance_long = 1,
        proper_running_style_nige = 1,
        proper_running_style_senko = 1,
        proper_running_style_sashi = 1,
        proper_running_style_oikomi = 1,
        proper_ground_turf = 1,
        proper_ground_dirt = 1
    };

    static SingleModeHomeInfo CreateHomeInfo(int[]? trainIds = null) => new()
    {
        command_info_array =
        [
            .. (trainIds ?? BaseTrainingIds).Select((commandId, index) => new SingleModeCommandInfo
            {
                command_type = 1,
                command_id = commandId,
                is_enable = 1,
                failure_rate = index * 5,
                training_partner_array = [],
                tips_event_partner_array = [],
                sub_command_partner_array = [],
                params_inc_dec_info_array = CreateParams(index + 1)
            })
        ]
    };

    static SingleModeParamsIncDecInfo[] CreateParams(int value) =>
    [
        new() { target_type = 1, value = value },
        new() { target_type = 2, value = value },
        new() { target_type = 3, value = value },
        new() { target_type = 4, value = value },
        new() { target_type = 5, value = value },
        new() { target_type = 30, value = value },
        new() { target_type = 10, value = -10 }
    ];}
