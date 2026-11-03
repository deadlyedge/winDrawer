# winDrawer 发布流程

> 本文描述当前版本的编号规则、自动 GitHub Release 流程和本地验证方式。构建与运行要求见 [`../README.md`](../README.md)。

## 1. 版本规则

版本格式为 `yy.M.patch`，例如 `26.11.1`：

- `yy`：年份后两位；
- `M`：月份，不补前导零（例如 9 月为 `26.9.1`，避免 NuGet/MSBuild 拒绝带前导零的版本段）；
- `patch`：当月发布序号，从 1 开始，跨月重新计数。

`patch` 由仓库中已有的 `v<yy>.<M>.<patch>` Git 标签推导，因此同一月连续发布得到 `26.11.1`、`26.11.2`……

版本的唯一来源是 [`src/winDrawer.csproj`](../src/winDrawer.csproj) 的 `<Version>`。设置面板左下角通过 `AppVersion` 显示程序集版本，发布工作流会用 `-p:Version` / `-p:AssemblyVersion` / `-p:FileVersion` 覆盖它，使 exe、程序集和页面显示保持一致。

## 2. 自动发布

工作流：[`.github/workflows/release.yml`](../.github/workflows/release.yml)

触发方式：

1. **推送到 `main`**：自动计算当月下一个版本并发布；
2. **手动运行**（Actions → Release → Run workflow）：可留空版本号让脚本计算，也可填入精确版本（例如 `26.11.2`）用于补发或修正。

提交信息包含 `[skip release]` 时，推送触发的运行会跳过发布（仍会执行版本判定步骤，随后直接结束）。

每次发布会：

1. `actions/checkout`（`fetch-depth: 0`，保证能读取全部标签）；
2. 安装 .NET 10 SDK；
3. 运行 [`scripts/Get-NextWinDrawerVersion.ps1`](../scripts/Get-NextWinDrawerVersion.ps1) 计算版本；
4. 发布两个单文件 exe：
   - `standalone`：自包含 .NET 运行时，可直接运行；
   - `framework-dependent`：不含 .NET 运行时，需要系统安装 .NET 10 Desktop Runtime；
5. 分别重命名为 `winDrawer-v<version>-win-x64-standalone.exe` 和 `winDrawer-v<version>-win-x64-framework-dependent.exe`，并为两者生成 `SHA256SUMS.txt`；
6. 上传构建产物并创建 GitHub Release（`gh release create --generate-notes --latest`）。同名 Release 已存在时改为覆盖上传资源。

工作流使用仓库自带的 `GITHUB_TOKEN`，`permissions.contents: write` 已足够；不需要额外的个人访问令牌。仓库设置中的 Actions 必须允许运行工作流。

## 3. 本地验证

不发布也能验证版本计算：

```powershell
./scripts/Get-NextWinDrawerVersion.ps1
./scripts/Get-NextWinDrawerVersion.ps1 -Version 26.11.7
```

本地构建并确认程序集版本：

```powershell
dotnet build .\src\winDrawer.csproj -c Release
(Get-Item .\src\bin\Release\net10.0-windows\win-x64\winDrawer.dll).VersionInfo |
  Select-Object FileVersion, ProductVersion
```

本地模拟发布（输出到临时目录，不触碰被占用的 `bin`）。先发布依赖运行时的版本：

```powershell
dotnet publish .\src\winDrawer.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  -p:IncludeAllContentForSelfExtract=true `
  -p:Version=26.11.1 `
  -p:VersionPrefix=26.11.1 `
  -p:AssemblyVersion=26.11.1.0 `
  -p:FileVersion=26.11.1.0 `
  -o "$env:TEMP\winDrawer-release\framework-dependent"
```

再发布自包含版本：

```powershell
dotnet publish .\src\winDrawer.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:Version=26.11.1 `
  -p:VersionPrefix=26.11.1 `
  -p:AssemblyVersion=26.11.1.0 `
  -p:FileVersion=26.11.1.0 `
  -o "$env:TEMP\winDrawer-release\standalone"
```

两个目录都应只生成一个 `winDrawer.exe`；`standalone` 约 52 MB，`framework-dependent` 约 1–2 MB。启动任意一个，打开设置面板，确认左下角显示 `v26.11.1`。

## 4. 数字证书说明

项目没有代码签名证书，发布的两个 exe 均未做数字签名：

- Windows SmartScreen 可能提示“未知发布者”，需要用户点击“更多信息 → 仍要运行”；
- 杀毒软件可能对未签名的新文件做额外扫描；
- 下载后请核对 Release 页面附带的 `SHA256SUMS.txt`：

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath .\winDrawer-v26.11.1-win-x64-standalone.exe
Get-FileHash -Algorithm SHA256 -LiteralPath .\winDrawer-v26.11.1-win-x64-framework-dependent.exe
```

README 的 **Download** 与 **Versioning and releases** 章节已包含同样的提醒；获得证书后应同步更新这两处文档。

## 5. 回滚与补发

- **补发同一版本**：从 Actions 手动运行 Release 并填写该版本号（例如 `26.11.1`）。资源会覆盖上传到同名 Release；如果该 Release 还有拆分前遗留的 `winDrawer-v<version>-win-x64.exe`，工作流会先删除它。
- **发布新补丁**：推送修复到 `main`，或在手动运行时留空版本号，得到 `26.11.2`。
- **注意**：“Re-run all jobs”会重新执行版本判定；此时该版本的标签已存在，因此会计算并发布下一个 patch，而不是覆盖原 Release。需要覆盖资源时请用上面的手动运行方式。
- **撤销错误发布**：在 GitHub Release 页面删除 Release 与标签；下一次运行会重新使用该 patch 号。删除前确认没有用户依赖该版本。
