# winDrawer 配置格式与兼容要求

## 1. 用途

本文件定义 winDrawer 本地设置的 TOML 数据契约。配置文件是用户数据，不应被 UI 或平台实现细节随意改变。正式发布后，字段升级和不兼容修改必须先定义迁移方案；本项目尚未发布，当前 schema 可直接调整，但不实现旧用户配置的运行时迁移。

> 本文件描述当前配置格式：主题颜色使用 `startup.window_color = "#RRGGBB"`，透明度使用独立整数 `startup.window_opacity`（0–100）。程序尚未发布，不提供旧 `window_color_hsla` 的自动迁移；开发用用户配置在阶段 6.1 中直接改为新格式。其它旧字段/表示不属于当前 schema。

设置页的 **Default** 按钮会在用户确认后立即把内嵌默认模板中受支持的 `[startup]` 项写入活动配置，保留 `drawers` 和未知键，并删除已废弃的 `startup.icon_size`。这一步会同步 Windows 当前用户登录启动状态，之后的 Save/Cancel 只控制 Default 之后的改动。

## 2. 基础格式

配置使用 UTF-8 TOML。核心结构包含一个 `[startup]` 表与零个或多个标准 `[[drawers]]` 表。抽屉路径使用标准 TOML 双引号字符串；Windows 路径中的反斜杠按 TOML 规则写成 `\\`，解析后得到普通 Windows 路径。抽屉尺寸使用与 `name`、`path` 同级的 `size = [width, height]` 数组。应用只按当前 schema 读取 TOML 表、表数组、数组、字符串、布尔值和整数，不尝试把旧子表、字符串标量或其它类型转换成当前字段。

```toml
[startup]
start_with_windows = false
window_locked = false
always_on_top = false
toggle_hotkey_enabled = true
window_color = "#000533"
window_opacity = 95
window_height = 200
max_drawer_items = 200
window_position = [100, 100]

[[drawers]]
name = "Documents"
path = "D:\\Users\\Example\\Documents"
size = [800, 600]
overlay_color = "#FF8800"
sort_mode = "oldest"

[[drawers]]
name = "Downloads"
path = "%USERPROFILE%\\Downloads"
```

## 3. 字段定义

| 字段 | 类型 | 默认值 | 有效范围/语义 |
|---|---|---:|---|
| `startup.start_with_windows` | Boolean | `false` | 是否在 Windows 登录时启动。若 UI 提供该选项，必须真正生效；是否纳入首版见产品需求。 |
| `startup.window_locked` | Boolean | `false` | 为 `true` 时禁止通过拖动区域移动主窗口，并隐藏标题区的移动光标提示；解锁后立即恢复拖动与提示。 |
| `startup.always_on_top` | Boolean | `false` | 为 `true` 时把主窗口和当前抽屉内容窗口设为置顶（`Form.TopMost`）。设置面板本身不置顶；该选项不影响托盘入口、显示/隐藏、任务栏归属和窗口锁定。缺失或类型无效时按 `false` 处理。 |
| `startup.toggle_hotkey_enabled` | Boolean | `true` | 为 `true` 时在启动时注册全局快捷键 Win+Shift+D（语义与托盘 `Show/Hide` 相同）。缺失或类型无效时按 `true` 处理。注册失败（例如组合键已被其它程序占用）不改变该值：应用只记录日志，并在下次启动或主窗口句柄重建时重试；在设置面板显式打开该项时失败会给出一次可见提示。 |
| `startup.window_height` | Integer | `200` | 主窗口客户区高度，单位为 96 DPI 逻辑像素（DIP），支持范围 100–1000。屏幕空间不足时运行时高度会临时受工作区限制，但保存的期望值不变。缺失或类型无效时使用默认高度 200。 |
| `startup.max_drawer_items` | Integer | `200` | 单个抽屉最多显示的条目数，支持范围 1–10000，超出范围时夹取。抽屉条目数超过该值时不再显示文件条目，只显示提示用户用资源管理器打开的状态；底栏文件夹路径仍可点击。缺失或类型无效时使用默认值 200。该字段不提供设置页面，可直接编辑 TOML；修改后在下一次打开或刷新抽屉时生效。 |
| `startup.window_color` | `#RRGGBB` 字符串 | `#000533` | 主主题基础颜色；只接受 6 位 RGB 十六进制颜色。缺失或格式错误时使用默认颜色。 |
| `startup.window_opacity` | Integer | `95` | 主窗口透明度百分比，支持 0–100；超范围值夹取，缺失或类型无效时使用默认值。 |
| `startup.window_position` | 恰好 2 个整数的数组 | `[100, 100]` | 主窗口桌面位置 `[x, y]`。数组长度必须恰好为 2 且元素为整数；长度或类型错误时整组回退默认值。坐标单位与 DPI 转换以技术决策为准。 |
| `drawers[].name` | String | 路径末级名称 | UI 显示名称。空白名称可在读取时从路径末级名称推导。 |
| `drawers[].path` | String | 必填 | Windows 文件/目录路径，使用标准 TOML 双引号字符串；空路径条目忽略并记录诊断。支持环境变量展开。 |
| `drawers[].size` | 2 个整数的数组 | 未设置 | 抽屉窗口尺寸 `[width, height]`；数组必须恰有两个整数，分别夹取到 200–4000。 |
| `drawers[].overlay_color` | `#RRGGBB` 字符串 | 未设置 | 可选的抽屉背景蒙版颜色；应用固定以 70% 不透明度覆盖主题背景。缺少或格式无效时不叠加专属颜色。 |
| `drawers[].sort_mode` | String | `"newest"` | 抽屉条目的显示顺序：`"newest"`（最后修改时间降序）、`"oldest"`（升序）、`"name"`（名称 `OrdinalIgnoreCase` 升序）。三种模式都保持目录与文件混排；名称相同时再按完整路径 `Ordinal` 升序。缺失、大小写不同或取值未知时按 `"newest"` 处理，不报错。应用只在用户于抽屉右键菜单里明确切换时写入该键，新增抽屉不主动写。 |

`drawers[].size` 可整体省略；省略或格式无效时使用 UI 的标准初始尺寸，不要求自动写回配置。尺寸单位必须与窗口布局使用的单位一致，并在实现中由统一的窗口坐标策略解释。

抽屉专属蒙版使用 6 位十六进制 RGB 字符串，例如 `overlay_color = "#FF8800"` 表示橙色；应用固定以 70% 不透明度混合到抽屉背景，不提供 Alpha 设置。省略字段或在设置中选择“不叠加”时移除该字段。颜色只接受 `#RRGGBB`。改名、更改绑定路径、颜色更新、排序和删除均只修改对应抽屉配置；删除抽屉不会删除或移动磁盘上的目录或文件。`[[drawers]]` 表的顺序即主窗口显示顺序。

路径使用标准 TOML basic string，例如 `path = "D:\\Users\\Example\\Documents"`。配置文件中的 `\\` 在 TOML 解析后表示一个反斜杠，因此运行时路径仍是 Windows 资源管理器所用的 `D:\Users\Example\Documents`。应用通过 TOML 库序列化路径，不自定义字符串引号风格。

## 4. 读取与校验规则

1. 解析 TOML 后先验证类型，再转换为领域设置；UI 不直接消费原始 TOML 值。保存时序列化完整配置模型，确保 `[startup]`、`[[drawers]]` 表及其顺序均写入。
2. 缺少 `[startup]` 时按默认值运行；缺字段使用上表默认值。
3. 数值字段接受整数。是否兼容浮点/数字字符串不是格式要求；若为了兼容而接受，应添加测试，避免含糊转换。
4. `startup.window_height` 缺失或类型无效时使用默认值 200；超出 100–1000 时夹取。`startup.max_drawer_items` 缺失或类型无效时使用默认值 200，超出 1–10000 时夹取。`window_color` 必须为 `#RRGGBB`；`window_opacity` 必须为整数并夹取到 0–100；`window_position` 长度或元素类型错误时整组回退默认值，不能产生索引越界。
5. `drawers` 只接受 TOML 标准 `[[drawers]]` 表数组；无效/缺少 `path` 的条目跳过并记录诊断；有效条目的空名称可从路径推导。`size` 只接受恰有两个整数的数组；缺少或无效的 `overlay_color` 按未设置处理。颜色仅接受 `#RRGGBB`，不读取旧的 `#AARRGGBB`。`sort_mode` 只接受字符串 `"newest"`/`"oldest"`/`"name"`（忽略大小写），其它取值或类型按 `"newest"` 处理。
6. 对 `path` 先展开支持的 Windows 环境变量，再去除首尾空白；重复目录判断在 Windows 上忽略大小写。更改现有抽屉路径时，允许其原路径当前不存在；只有更换到另一条路径时，才要求目标目录存在。除非明确产品需求，不要对路径执行会改变其指向的规范化（例如擅自解析符号链接）。
7. 目录当前不存在时仍可保留配置条目，以便其之后重新可用；UI 显示缺失状态。
8. 未知字段不得导致配置读取失败；优先保留未知数据。若保存完整模型会丢失未知字段或注释，应使用支持保留的编辑方式，或在写入前明确拒绝破坏性保存并备份原文件。
9. TOML 语法损坏时，不得静默覆盖原始文件。启动阶段向用户显示可理解的配置错误，并写出足以定位问题的日志；原文件保持不变，用户可在应用外备份并修复后重试。
10. 读取/保存失败不得让应用崩溃或丢弃内存中未保存状态；明确告知用户保存失败。

## 5. 写入约束

- 保存前验证整个配置对象，而非逐个 UI 控件独立写盘。
- 对多个快速更新做合并/防抖；应用退出时等待写入完成。
- 推荐先写入同目录临时文件、完成 flush 后原子替换目标文件；保留旧文件直到新文件确认写入成功。
- 仅由 settings repository 负责 TOML 序列化、备份和原子写入。
- 设置文件默认位于 `%USERPROFILE%\.config\winDrawer\Drawer-settings.toml`；日志文件为同目录下的 `Drawer.log`。配置目录由应用在首次创建设置或写入日志时按需创建；日志路径或写入失败不会阻止应用运行。
- 设置页的 **Open Settings TOML** 按钮使用 Windows Notepad 打开当前活动的 TOML 文件。
- 抽屉的拖入/拖出、底栏文件夹入口和列表右键排序菜单都是运行时交互，不会新增或改写任何配置键；文件拖放只作用于磁盘文件，不写入设置。

## 6. 迁移与演进

- 当前版本应使用本文件第 2 节给出的字段格式。
- 以下历史形式不属于当前 schema，程序不读取、转换或迁移它们：`startup.window_color_hsla`、8 位 `#AARRGGBB` 颜色、旧 `[drawers.size]` 子表、非标准的抽屉表示、将布尔/整数/颜色字段写成字符串、将整数写成浮点数，或将坐标/尺寸数组写成字符串。路径和名称按当前 schema 使用字符串。`window_position` 和 `drawers[].size` 只接受当前格式定义的精确长度整数数组。
- `startup.icon_size` 已从当前设置模型移除；应用不再读取或写入该键。任何设置写盘（包括普通保存和 **Default**）都会从 `[startup]` 序列化结果中剔除 `icon_size`；其它未知键仍保留。
- 当前抽屉 schema 使用标准 TOML 字符串，并要求尺寸使用同级 `size = [width, height]` 数组；不兼容旧的 `[drawers.size]` 子表。升级前需将旧尺寸子表转换为本节新格式。
- 当前格式没有 schema version。首次引入 `schema_version` 或改用其他格式前，先定义默认版本、旧版本升级、未知未来版本处理和回滚策略。
- 程序尚未发布，不实现旧配置运行时迁移或 HSLA 兼容读取。阶段 6.1 中开发者当前用户目录配置由开发任务直接改写为新字段；应用首次启动时若活动文件不存在，则用随程序部署的根目录 `Drawer-settings.toml` 模板初始化。若活动文件已经存在，模板不会覆盖、合并或重置该文件；应用不会搜索、读取或迁移其它路径中的配置。
- 暂不迁移旧日志。新日志写入配置目录中的 `Drawer.log`；程序目录中已有的旧日志保持原位，不自动读取、复制、移动或删除。创建目录或日志写入失败不得阻止应用启动。
- 将来若实施不兼容 schema 变更，应先单独设计迁移：备份旧文件、读取旧 schema、转换到新模型、验证新文件、原子写入，然后才确认迁移成功。当前版本不执行旧路径配置迁移。
- 本项目尚未发布；不为 `window_color_hsla` 添加运行时迁移或兼容读取。若未来发布时需要照顾旧用户，应先单独决定迁移策略。
- `reference/Drawer-settings copy.toml` 是历史配置副本，含已废弃字段/表示和机器专属路径；它不是模板、不会随应用加载，也不能直接作为当前用户配置使用。需要整理旧配置时，先在应用外备份原文件，再手动将颜色改为 `#RRGGBB`、将旧尺寸改为各 `[[drawers]]` 表中的 `size = [width, height]`，并核对每个字段的数据类型；不要删除或覆盖原文件，直到新格式已验证。

## 7. 配置测试清单

- [ ] 空文件、缺少 startup、缺少 drawers 均使用定义的默认值。
- [ ] 示例配置可读取并写回，抽屉顺序、名称、路径、尺寸和主题值不丢失。
- [ ] `window_color` 的合法/非法十六进制值和 `window_opacity` 的缺失/越界值符合规则。
- [ ] 普通保存、后台写盘及退出 flush 均不输出遗留的 `[startup].icon_size`，且其它未知 startup 键保持不变。
- [ ] Settings Default 取消确认时不改设置；确认后仅重置模板中支持的 startup 项、删除 `icon_size`、保留 drawers/未知键，并与 Windows 登录启动注册一致；失败时恢复原值并提示。
- [ ] 窗口坐标正常、数组过短、类型错误均不 panic。
- [ ] `startup.always_on_top` 缺失、为 `true`、类型错误时的解析结果，以及 Default 重置和 Save 后重启的取值符合规则。
- [ ] `startup.toggle_hotkey_enabled` 缺失、为 `true`/`false`、类型错误时分别按 `true`/`false`/`true` 解析；Save 后重启保持，Default 重置为模板值 `true`；注册失败不会写回其它值。
- [ ] 抽屉 name/path/size 缺失、尺寸数组长度/类型错误、尺寸越界与无效 path 符合规则。
- [ ] 环境变量展开后重复路径判断符合 Windows 大小写规则。
- [ ] 缺少、有效、格式错误的抽屉 `overlay_color` 都按约定读取；颜色修改、路径修改、删除和重排只修改目标条目且保留其它抽屉字段。
- [ ] `drawers[].sort_mode` 缺失、`"newest"`/`"oldest"`/`"name"`（含大小写变体）、未知取值与错误类型分别按 `"newest"` 默认与对应模式解析；用户切换后写回并保留同一抽屉的其它字段；**Default 重置不得改动任何抽屉的 `sort_mode`**；新增抽屉不写该键。
- [ ] 未知字段/注释策略有可重复测试。
- [ ] TOML 损坏、只读目录、磁盘写入失败时保留原文件并报告错误。
- [ ] 不执行旧路径或旧 HSLA 配置自动迁移；新默认配置按目标路径和根目录模板创建，不触碰其它位置文件。
- [ ] 设置文件新默认路径为 `%USERPROFILE%\.config\winDrawer\Drawer-settings.toml`；Notepad 打开按钮指向同一活动文件。

阶段 6.7 的临时 smoke harness 已验证：嵌入模板可解析并生成当前默认字段；仓储在仅有临时目标文件时可从模板初始化；损坏的既有 TOML 被拒绝且文件内容保持不变；当前 `#RRGGBB` 颜色、整数透明度/高度、两整数位置数组、标准 `[[drawers]]` 与 `size`/`overlay_color` 可读；旧 HSLA 不迁移、字符串标量回退当前默认值、旧 `[drawers.size]` 不导入。测试只操作随机命名的 `%TEMP%` 目录，不访问真实用户配置；它不替代本节其余未勾选的配置读写、故障注入和 UI 手工回归。
