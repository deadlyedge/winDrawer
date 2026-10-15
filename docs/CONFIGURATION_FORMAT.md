# winDrawer 配置格式与兼容要求

## 1. 用途

本文件定义 winDrawer 本地设置的 TOML 数据契约。配置文件是用户数据，不应被 UI 或平台实现细节随意改变。字段升级和不兼容修改都必须有迁移方案；当前版本暂不自动迁移旧位置的配置。

> 本文字段表描述当前已实现版本的配置格式。后续计划将以 `startup.window_color = "#RRGGBB"` 和独立的 `startup.window_opacity` 替代 `window_color_hsla`；实施细节及一次性迁移要求见 [`NEXT_DEVELOPMENT_PLAN.md`](NEXT_DEVELOPMENT_PLAN.md)。在迁移实现完成前，不要把计划中的目标字段误认为当前程序已经支持的字段。

## 2. 基础格式

配置使用 UTF-8 TOML。核心结构包含一个 `[startup]` 表与零个或多个 `[[drawers]]` 表。抽屉路径使用标准 TOML 双引号字符串；Windows 路径中的反斜杠按 TOML 规则写成 `\\`，解析后得到普通 Windows 路径。抽屉尺寸使用与 `name`、`path` 同级的 `[width, height]` 数组。

```toml
[startup]
start_with_windows = false
window_locked = false
window_color = "#RRGGBB"
window_opacity = 90
window_height = 400
window_position = [100, 100]

[[drawers]]
name = "Documents"
path = "D:\\Users\\Example\\Documents"
size = [800, 600]
overlay_color = "#FF8800"

[[drawers]]
name = "Downloads"
path = "%USERPROFILE%\\Downloads"
```

## 3. 字段定义

| 字段 | 类型 | 默认值 | 有效范围/语义 |
|---|---|---:|---|
| `startup.start_with_windows` | Boolean | `false` | 是否在 Windows 登录时启动。若 UI 提供该选项，必须真正生效；是否纳入首版见产品需求。 |
| `startup.window_locked` | Boolean | `false` | 为 `true` 时禁止通过拖动区域移动主窗口。 |
| `startup.icon_size` | Integer | `96` | 图标正方形边长，支持范围 32–512。超范围值夹取到范围内。 |
| `startup.window_height` | Integer | `400` | 主窗口客户区高度，单位为 96 DPI 逻辑像素（DIP），支持范围 200–1000。屏幕空间不足时运行时高度会临时受工作区限制，但保存的期望值不变。缺失或类型无效时使用默认高度 400。 |
| `startup.window_color_hsla` | 4 个整数的数组 | `[194, 100, 41, 60]` | 顺序为 Hue 0–360、Saturation 0–100、Lightness 0–100、Alpha 0–100。缺失或非法分量使用对应默认值/报告诊断，不得越界进入 UI。 |
| `startup.window_position` | 2 个整数的数组 | `[100, 100]` | 主窗口桌面位置 `[x, y]`。必须有且仅需读取前两个整数；长度不足、类型错误时回退默认值并记录诊断。坐标单位与 DPI 转换以技术决策为准。 |
| `drawers[].name` | String | 路径末级名称 | UI 显示名称。空白名称可在读取时从路径末级名称推导。 |
| `drawers[].path` | String | 必填 | Windows 文件/目录路径，使用标准 TOML 双引号字符串；空路径条目忽略并记录诊断。支持环境变量展开。 |
| `drawers[].size` | 2 个整数的数组 | 未设置 | 抽屉窗口尺寸 `[width, height]`；数组必须恰有两个整数，分别夹取到 200–4000。 |
| `drawers[].overlay_color` | `#RRGGBB` 字符串 | 未设置 | 可选的抽屉背景蒙版颜色；应用固定以 70% 不透明度覆盖主题背景。缺少或格式无效时不叠加专属颜色。 |

`drawers[].size` 可整体省略；省略或格式无效时使用 UI 的标准初始尺寸，不要求自动写回配置。尺寸单位必须与窗口布局使用的单位一致，并在实现中由统一的窗口坐标策略解释。

抽屉专属蒙版使用 6 位十六进制 RGB 字符串，例如 `overlay_color = "#FF8800"` 表示橙色；应用固定以 70% 不透明度混合到抽屉背景，不提供 Alpha 设置。省略字段或在设置中选择“不叠加”时移除该字段。旧版 8 位 `#AARRGGBB` 值仍可读取，读取时保留 RGB 并忽略原 Alpha。改名、更改绑定路径、颜色更新、排序和删除均只修改对应抽屉配置；删除抽屉不会删除或移动磁盘上的目录或文件。`[[drawers]]` 表的顺序即主窗口显示顺序。

路径使用标准 TOML basic string，例如 `path = "D:\\Users\\Example\\Documents"`。配置文件中的 `\\` 在 TOML 解析后表示一个反斜杠，因此运行时路径仍是 Windows 资源管理器所用的 `D:\Users\Example\Documents`。应用通过 TOML 库序列化路径，不自定义字符串引号风格。

## 4. 读取与校验规则

1. 解析 TOML 后先验证类型，再转换为领域设置；UI 不直接消费原始 TOML 值。保存时序列化完整配置模型，确保 `[startup]`、`[[drawers]]` 表及其顺序均写入。
2. 缺少 `[startup]` 时按默认值运行；缺字段使用上表默认值。
3. 数值字段接受整数。是否兼容浮点/数字字符串不是格式要求；若为了兼容而接受，应添加测试，避免含糊转换。
4. `startup.window_height` 缺失或类型无效时使用默认值 400；超出 200–1000 时夹取。HSLA 数组缺少某个分量时只对该分量使用默认值；`window_position` 长度不足时整组回退默认值，不能产生索引越界。
5. `drawers` 中无效/缺少 `path` 的条目跳过并记录诊断；有效条目的空名称可从路径推导。`size` 只接受恰有两个整数的数组，不读取旧的 `[drawers.size]` 子表格式；缺少或无效的 `overlay_color` 按未设置处理。
6. 对 `path` 先展开支持的 Windows 环境变量，再去除首尾空白；重复目录判断在 Windows 上忽略大小写。更改现有抽屉路径时，允许其原路径当前不存在；只有更换到另一条路径时，才要求目标目录存在。除非明确产品需求，不要对路径执行会改变其指向的规范化（例如擅自解析符号链接）。
7. 目录当前不存在时仍可保留配置条目，以便其之后重新可用；UI 显示缺失状态。
8. 未知字段不得导致配置读取失败；优先保留未知数据。若保存完整模型会丢失未知字段或注释，应使用支持保留的编辑方式，或在写入前明确拒绝破坏性保存并备份原文件。
9. TOML 语法损坏时，不得静默覆盖原始文件。向用户提供可恢复错误，保留原文件并写出足以定位问题的日志。
10. 读取/保存失败不得让应用崩溃或丢弃内存中未保存状态；明确告知用户保存失败。

## 5. 写入约束

- 保存前验证整个配置对象，而非逐个 UI 控件独立写盘。
- 对多个快速更新做合并/防抖；应用退出时等待写入完成。
- 推荐先写入同目录临时文件、完成 flush 后原子替换目标文件；保留旧文件直到新文件确认写入成功。
- 仅由 settings repository 负责 TOML 序列化、备份和原子写入。
- 设置文件默认位于 `%USERPROFILE%\.config\winDrawer\Drawer-settings.toml`；日志文件为同目录下的 `Drawer.log`。配置目录由应用在首次创建设置或写入日志时按需创建；日志路径或写入失败不会阻止应用运行。
- 设置页的 **Open Settings TOML** 按钮使用 Windows Notepad 打开当前活动的 TOML 文件。

## 6. 迁移与演进

- 初版应兼容本文件第 2 节给出的字段格式。
- 当前抽屉 schema 使用标准 TOML 字符串，并要求尺寸使用同级 `size = [width, height]` 数组；不兼容旧的 `[drawers.size]` 子表。升级前需将旧尺寸子表转换为本节新格式。
- 当前格式没有 schema version。首次引入 `schema_version` 或改用其他格式前，先定义默认版本、旧版本升级、未知未来版本处理和回滚策略。
- 暂不实现旧配置迁移。若 `%USERPROFILE%\.config\winDrawer\Drawer-settings.toml` 不存在，应用在该位置创建默认配置；程序目录中的旧 `Drawer-settings.toml` 不会被读取、复制、覆盖或删除，应由用户自行备份/处理。
- 暂不迁移旧日志。新日志写入配置目录中的 `Drawer.log`；程序目录中已有的旧日志保持原位，不自动读取、复制、移动或删除。创建目录或日志写入失败不得阻止应用启动。
- 将来若实施不兼容 schema 变更，应先单独设计迁移：备份旧文件、读取旧 schema、转换到新模型、验证新文件、原子写入，然后才确认迁移成功。当前版本不执行旧路径配置迁移。
- 设置字段被删除或语义改变时，保留旧字段的兼容读取/迁移一段明确周期，或在升级说明中明确说明。

## 7. 配置测试清单

- [ ] 空文件、缺少 startup、缺少 drawers 均使用定义的默认值。
- [ ] 示例配置可读取并写回，抽屉顺序、名称、路径、尺寸和主题值不丢失。
- [ ] HSLA 最小/最大/越界/缺失值符合规则。
- [ ] 窗口坐标正常、数组过短、类型错误均不 panic。
- [ ] 抽屉 name/path/size 缺失、尺寸数组长度/类型错误、尺寸越界与无效 path 符合规则。
- [ ] 环境变量展开后重复路径判断符合 Windows 大小写规则。
- [ ] 缺少、有效、格式错误的抽屉 `overlay_color` 都按约定读取；颜色修改、路径修改、删除和重排只修改目标条目且保留其它抽屉字段。
- [ ] 未知字段/注释策略有可重复测试。
- [ ] TOML 损坏、只读目录、磁盘写入失败时保留原文件并报告错误。
- [ ] 不执行旧位置配置自动迁移；新默认配置按目标路径创建，不触碰旧位置文件。
- [ ] 设置文件新默认路径为 `%USERPROFILE%\.config\winDrawer\Drawer-settings.toml`；Notepad 打开按钮指向同一活动文件。
