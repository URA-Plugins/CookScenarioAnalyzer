using Gallop;
using Gallop.Endpoints;
using UmamusumeResponseAnalyzer.TerminalGui;
using UmamusumeResponseAnalyzer.Plugin;

namespace CookScenarioAnalyzer;

public sealed class CookScenarioAnalyzer : IPlugin
{
    const string WorkspaceTitle = "CookScenarioAnalyzer";
    const string TrainingPanelKey = "training";

    Workspace? workspace;
    bool hasPublishedTrainingPanel;
    int currentTurn;

    public void Initialize(IPluginContext context)
    {
        hasPublishedTrainingPanel = false;
        currentTurn = 0;
    }

    public void Dispose()
    {
        currentTurn = 0;
        if (!hasPublishedTrainingPanel)
            return;

        workspace!.RemovePanel(TrainingPanelKey);
        hasPublishedTrainingPanel = false;
    }

    [ResponseAnalyzer<GameApi.SingleModeCook.CheckEvent>(1)]
    public ValueTask Analyze(SingleModeCookCheckEventResponse response)
    {
        var data = response.data;
        if (data.home_info.command_info_array is null || data.chara_info.state is 2 or 3)
            return ValueTask.CompletedTask;
        if ((data.unchecked_event_array is { Length: > 0 }) || data.race_start_info is not null)
            return ValueTask.CompletedTask;

        var content = Handlers.ParseCookCommandInfo(response, ref currentTurn);
        var workspace = this.workspace ??= Workspace.Create(WorkspaceTitle);
        workspace.SetPanel(
            TrainingPanelKey,
            "训练分析",
            content,
            fullBleed: true,
            switchToWorkspace: !hasPublishedTrainingPanel);
        hasPublishedTrainingPanel = true;
        return ValueTask.CompletedTask;
    }
}
