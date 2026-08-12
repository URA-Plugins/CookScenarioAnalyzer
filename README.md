# CookScenarioAnalyzer

种田杯训练回合分析插件。

## History

插件在当前进程内保存训练分析记录，以 `single_mode_chara_id` 和 `turn` 作为唯一键；相同键的后续输出原位更新，不新增记录。History 不跨进程重启持久化。

在插件 workspace 的训练分析面板中使用方向键导航：`↑` 前往较旧记录，`↓` 前往较新记录，`←` 前往最旧记录，`→` 前往最新记录。History 启用时，正文仍可使用 `PageUp`、`PageDown`、`Home`、`End` 和鼠标滚轮滚动。

配置保存在 `PluginData/CookScenarioAnalyzer/settings.json`：

```json
{
  "historyLimit": 100
}
```

`historyLimit` 的有效范围是 `0` 到 `1000`，默认值为 `100`。设为 `0` 时不保留历史记录，面板仍显示最近一次成功分析结果。
