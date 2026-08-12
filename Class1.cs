using System.Text.Json;
using System.Text.Json.Serialization;
using Gallop;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using UmamusumeResponseAnalyzer.Plugin;
using UmamusumeResponseAnalyzer.TerminalGui;

namespace CookScenarioAnalyzer;

public sealed class CookScenarioAnalyzer : IPlugin
{
    const string InternalName = "CookScenarioAnalyzer";
    const string TrainingPanelKey = "training";
    const int DefaultHistoryLimit = 100;
    const int MaximumHistoryLimit = 1000;

    static readonly JsonSerializerOptions SettingsJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
    };

    readonly object historyGate = new();
    readonly List<HistoryEntry> history = [];

    IApplication? application;
    Workspace? workspace;
    WorkspaceContent? panelContent;
    WorkspaceContent? liveSnapshot;
    HistoryPanelView? historyView;
    int historyLimit = DefaultHistoryLimit;
    int selectedIndex = -1;
    int currentTurn;
    long refreshGeneration;
    bool hasPublishedTrainingPanel;
    bool hasUnread;
    volatile bool disposed;

    static string SettingsDirectory => Path.Combine("PluginData", InternalName);

    static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public void Initialize(IPluginContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Analyzers.Register<SingleModeCookCheckEventResponse>(
            AnalyzerKind.Response,
            [EndpointPattern.Exact("/umamusume/single_mode_cook/check_event")],
            invocation => Analyze(invocation.Payload),
            priority: 1);
        var settings = LoadSettings();
        lock (historyGate)
        {
            history.Clear();
            liveSnapshot = null;
            selectedIndex = -1;
            hasUnread = false;
            historyLimit = settings.HistoryLimit;
            application = context.Application;
            disposed = false;
        }

        hasPublishedTrainingPanel = false;
        currentTurn = 0;
    }

    public void Dispose()
    {
        HistoryPanelView? view;
        Workspace? publishedWorkspace;
        lock (historyGate)
        {
            disposed = true;
            refreshGeneration++;
            history.Clear();
            liveSnapshot = null;
            selectedIndex = -1;
            hasUnread = false;
            view = historyView;
            historyView = null;
            panelContent = null;
            application = null;
            publishedWorkspace = hasPublishedTrainingPanel ? workspace : null;
            workspace = null;
            hasPublishedTrainingPanel = false;
        }
        view?.DetachKeyboard();

        currentTurn = 0;
        publishedWorkspace?.RemovePanel(TrainingPanelKey);
    }

    public ValueTask Analyze(SingleModeCookCheckEventResponse response)
    {
        var data = response.data;
        if (data.home_info.command_info_array is null || data.chara_info.state is 2 or 3)
            return ValueTask.CompletedTask;
        if ((data.unchecked_event_array is { Length: > 0 }) || data.race_start_info is not null)
            return ValueTask.CompletedTask;

        var refresh = false;
        var notifyUnread = false;
        Workspace target;
        lock (historyGate)
        {
            if (disposed)
                return ValueTask.CompletedTask;

            var key = new HistoryKey(data.chara_info.single_mode_chara_id, data.chara_info.turn);
            var content = Handlers.ParseCookCommandInfo(response, ref currentTurn);
            target = workspace ??= Workspace.Create(InternalName);
            if (!hasPublishedTrainingPanel)
            {
                panelContent = new(CreateHistoryView);
                target.SetPanel(
                    TrainingPanelKey,
                    "训练分析",
                    panelContent,
                    fullBleed: true,
                    switchToWorkspace: true);
                hasPublishedTrainingPanel = true;
            }
            else
            {
                target.SetPanel(
                    TrainingPanelKey,
                    "训练分析",
                    panelContent!,
                    fullBleed: true,
                    switchToWorkspace: false);
            }

            (refresh, notifyUnread) = PublishLocked(key, content);
        }

        if (refresh)
            RefreshHistoryView();
        if (notifyUnread)
            NotifyIfActive(target, "有新的训练分析记录。按 → 查看最新。");
        return ValueTask.CompletedTask;
    }

    public async Task ConfigPromptAsync(
        IApplication application,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(application);
        cancellationToken.ThrowIfCancellationRequested();
        if (application.TopRunnable is null &&
            Environment.CurrentManagedThreadId != application.MainThreadId)
        {
            throw new InvalidOperationException(
                "CookScenarioAnalyzer 无法从非 UI thread 启动配置：Terminal.Gui 当前没有正在运行的 session。");
        }

        var draft = LoadSettings();
        HistorySettings saved;
        if (Environment.CurrentManagedThreadId == application.MainThreadId)
        {
            saved = RunConfigDialog(application, draft, cancellationToken);
        }
        else
        {
            var completion = new TaskCompletionSource<HistorySettings>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            application.Invoke(() =>
            {
                try
                {
                    completion.SetResult(RunConfigDialog(application, draft, cancellationToken));
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                }
            });
            saved = await completion.Task;
        }

        cancellationToken.ThrowIfCancellationRequested();
        ValidateHistoryLimit(saved.HistoryLimit);
        Directory.CreateDirectory(SettingsDirectory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(saved, SettingsJson));
        ApplyHistoryLimit(saved.HistoryLimit);
    }

    static HistorySettings RunConfigDialog(
        IApplication application,
        HistorySettings draft,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var dialog = new Dialog
        {
            Title = "CookScenarioAnalyzer 配置",
            Width = 58,
            Height = 12,
        };
        var historyLimit = new NumericUpDown<int>
        {
            X = 1,
            Y = 2,
            Width = 18,
            Value = draft.HistoryLimit,
            Increment = 1,
        };
        var validation = new Label
        {
            X = 1,
            Y = 5,
            Width = Dim.Fill(1),
            Height = 2,
            Text = string.Empty,
        };
        dialog.Add(
            new Label { X = 1, Y = 1, Text = "History 保存上限（0 表示关闭）" },
            historyLimit,
            new Label { X = 21, Y = 2, Text = "范围：0–1000" },
            validation);

        var accepted = false;
        var save = new Button { Text = "保存", IsDefault = true };
        save.Accepting += (_, e) =>
        {
            if (historyLimit.Value is < 0 or > MaximumHistoryLimit)
            {
                validation.Text = "History 上限必须是 0 到 1000。";
                e.Handled = true;
                return;
            }

            accepted = true;
            application.RequestStop(dialog);
            e.Handled = true;
        };
        var cancel = new Button { Text = "取消" };
        cancel.Accepting += (_, e) =>
        {
            application.RequestStop(dialog);
            e.Handled = true;
        };
        dialog.AddButton(cancel);
        dialog.AddButton(save);
        historyLimit.SetFocus();

        using (cancellationToken.Register(
                   () => application.Invoke(() => application.RequestStop(dialog))))
            application.Run(dialog);
        cancellationToken.ThrowIfCancellationRequested();
        if (!accepted)
        {
            throw new OperationCanceledException(
                "CookScenarioAnalyzer 配置已取消。",
                cancellationToken);
        }

        return new(historyLimit.Value);
    }

    static HistorySettings LoadSettings()
    {
        if (!File.Exists(SettingsPath))
            return new(DefaultHistoryLimit);

        HistorySettings settings;
        try
        {
            settings = JsonSerializer.Deserialize<HistorySettings>(
                    File.ReadAllText(SettingsPath),
                    SettingsJson)
                ?? throw new JsonException("配置内容不能是 null。");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"CookScenarioAnalyzer 配置文件无效: {SettingsPath}。{ex.Message}",
                ex);
        }

        ValidateHistoryLimit(settings.HistoryLimit);
        return settings;
    }

    static void ValidateHistoryLimit(int value)
    {
        if (value is < 0 or > MaximumHistoryLimit)
        {
            throw new InvalidDataException(
                $"CookScenarioAnalyzer historyLimit 必须在 0 到 {MaximumHistoryLimit} 之间，当前值: {value}。配置文件: {SettingsPath}");
        }
    }

    (bool Refresh, bool NotifyUnread) PublishLocked(HistoryKey key, WorkspaceContent content)
    {
        var refresh = false;
        var notifyUnread = false;
        liveSnapshot = content;
        if (historyLimit == 0)
        {
            history.Clear();
            selectedIndex = -1;
            hasUnread = false;
            refresh = true;
        }
        else
        {
            var existingIndex = history.FindIndex(entry => entry.Key == key);
            if (existingIndex >= 0)
            {
                history[existingIndex] = new(key, content);
                refresh = existingIndex == selectedIndex;
            }
            else
            {
                var wasFollowingLatest = selectedIndex < 0 || selectedIndex == history.Count - 1;
                history.Add(new(key, content));
                if (wasFollowingLatest)
                {
                    selectedIndex = history.Count - 1;
                    hasUnread = false;
                    refresh = true;
                }
                else if (!hasUnread)
                {
                    hasUnread = true;
                    notifyUnread = true;
                }

                if (TrimHistoryLocked())
                {
                    refresh = true;
                    notifyUnread = false;
                }
            }
        }

        return (refresh, notifyUnread);
    }

    void ApplyHistoryLimit(int value)
    {
        ValidateHistoryLimit(value);
        lock (historyGate)
        {
            historyLimit = value;
            if (value == 0)
            {
                history.Clear();
                selectedIndex = -1;
                hasUnread = false;
            }
            else
            {
                TrimHistoryLocked();
            }
        }
        RefreshHistoryView();
    }

    bool TrimHistoryLocked()
    {
        var overflow = history.Count - historyLimit;
        if (overflow <= 0)
            return false;

        if (selectedIndex < overflow)
        {
            history.RemoveRange(0, overflow);
            selectedIndex = history.Count - 1;
            hasUnread = false;
            return true;
        }

        history.RemoveRange(0, overflow);
        selectedIndex -= overflow;
        return false;
    }

    bool Navigate(KeyCode keyCode)
    {
        WorkspaceContent? content;
        int position;
        int count;
        lock (historyGate)
        {
            if (disposed || historyLimit == 0 || history.Count == 0)
                return false;

            var previousIndex = selectedIndex;
            selectedIndex = keyCode switch
            {
                KeyCode.CursorUp => Math.Max(0, selectedIndex - 1),
                KeyCode.CursorDown => Math.Min(history.Count - 1, selectedIndex + 1),
                KeyCode.CursorLeft => 0,
                KeyCode.CursorRight => history.Count - 1,
                _ => selectedIndex,
            };
            if (selectedIndex == history.Count - 1)
                hasUnread = false;

            content = history[selectedIndex].Content;
            position = selectedIndex + 1;
            count = history.Count;
            if (selectedIndex == previousIndex)
                content = null;
        }

        if (content is not null)
            RefreshHistoryView(content);
        Workspace? target;
        lock (historyGate)
            target = workspace;
        if (target is not null)
            NotifyIfActive(target, $"训练分析历史 {position}/{count}");
        return true;
    }

    void NotifyIfActive(Workspace target, string text)
    {
        lock (historyGate)
        {
            if (!disposed && ReferenceEquals(workspace, target))
                target.Notify(text, UiSeverity.Info);
        }
    }

    WorkspaceContent? SelectedContentLocked()
        => historyLimit > 0 && selectedIndex >= 0 && selectedIndex < history.Count
            ? history[selectedIndex].Content
            : liveSnapshot;

    View CreateHistoryView()
    {
        HistoryPanelView view;
        WorkspaceContent? content;
        lock (historyGate)
        {
            if (disposed || application is not { } app)
            {
                return new View
                {
                    Width = Dim.Fill(),
                    Height = Dim.Auto(),
                };
            }
            view = new(app, this);
            historyView = view;
            content = SelectedContentLocked();
        }
        view.Show(content ?? WorkspaceContent.Text(string.Empty));
        return view;
    }

    void RefreshHistoryView()
    {
        WorkspaceContent? content;
        long generation;
        lock (historyGate)
        {
            if (disposed)
                return;
            content = SelectedContentLocked();
            generation = ++refreshGeneration;
        }
        if (content is not null)
            RefreshHistoryView(content, generation);
    }

    void RefreshHistoryView(WorkspaceContent content)
    {
        long generation;
        lock (historyGate)
        {
            if (disposed)
                return;
            generation = ++refreshGeneration;
        }
        RefreshHistoryView(content, generation);
    }

    void RefreshHistoryView(WorkspaceContent content, long generation)
    {
        HistoryPanelView? view;
        IApplication? app;
        lock (historyGate)
        {
            view = historyView;
            app = application;
        }
        if (view is null || app is null)
            return;

        void Refresh()
        {
            lock (historyGate)
            {
                if (disposed || generation != refreshGeneration ||
                    !ReferenceEquals(view, historyView))
                {
                    return;
                }
                view.Show(content);
                RefreshPanelLayoutLocked();
            }
        }

        if (Environment.CurrentManagedThreadId == app.MainThreadId)
        {
            Refresh();
            return;
        }
        app.Invoke(Refresh);
    }

    void RefreshPanelLayoutLocked()
    {
        if (!hasPublishedTrainingPanel || workspace is null || panelContent is null)
            return;
        workspace.SetPanel(
            TrainingPanelKey,
            "训练分析",
            panelContent,
            fullBleed: true,
            switchToWorkspace: false);
    }

    void ReleaseHistoryView(HistoryPanelView view)
    {
        lock (historyGate)
        {
            if (ReferenceEquals(historyView, view))
                historyView = null;
        }
    }

    readonly record struct HistoryKey(int SingleModeCharaId, int Turn);

    sealed record HistoryEntry(HistoryKey Key, WorkspaceContent Content);

    sealed record HistorySettings([property: JsonRequired] int HistoryLimit);

    sealed class HistoryPanelView : View
    {
        readonly IApplication application;
        readonly CookScenarioAnalyzer owner;
        bool keyboardAttached = true;
        bool viewDisposed;

        internal HistoryPanelView(IApplication application, CookScenarioAnalyzer owner)
        {
            this.application = application;
            this.owner = owner;
            Width = Dim.Fill();
            Height = Dim.Auto();
            CanFocus = true;
            TabStop = TabBehavior.TabGroup;
            application.Keyboard.KeyDown += ApplicationKeyDown;
        }

        internal void Show(WorkspaceContent content)
        {
            if (viewDisposed)
                return;

            var next = content.CreateView();
            next.X = 0;
            next.Y = 0;
            next.Width = Dim.Fill();
            next.Height = Dim.Auto();
            next.CanFocus = false;

            var previous = SubViews.FirstOrDefault();
            if (previous is not null)
            {
                Remove(previous);
                previous.Dispose();
            }
            Add(next);
            SetNeedsLayout();
            SetNeedsDraw();
        }

        internal void DetachKeyboard()
        {
            if (!keyboardAttached)
                return;
            keyboardAttached = false;
            application.Keyboard.KeyDown -= ApplicationKeyDown;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !viewDisposed)
            {
                viewDisposed = true;
                DetachKeyboard();
                owner.ReleaseHistoryView(this);
            }
            base.Dispose(disposing);
        }

        void ApplicationKeyDown(object? sender, Key key)
        {
            if (key.Handled || key.IsCtrl || key.IsAlt || key.IsShift ||
                !ReferenceEquals(Workspace.Current, owner.workspace) ||
                !ContainsFocus())
            {
                return;
            }

            if (key.KeyCode is not (
                    KeyCode.CursorUp or
                    KeyCode.CursorDown or
                    KeyCode.CursorLeft or
                    KeyCode.CursorRight))
            {
                return;
            }

            if (owner.Navigate(key.KeyCode))
                key.Handled = true;
        }

        bool ContainsFocus()
        {
            for (var view = application.TopRunnableView?.MostFocused;
                 view is not null;
                 view = view.SuperView)
            {
                if (ReferenceEquals(view, this))
                    return true;
            }
            return false;
        }
    }
}
