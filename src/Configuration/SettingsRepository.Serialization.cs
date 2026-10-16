using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Serialization;
using Tomlyn.Syntax;

namespace winDrawer.Configuration;

internal sealed partial class SettingsRepository
{
    private string SerializeSettings(TomlTable root)
    {
        var metadataStore = new TomlMetadataStore();
        var options = new TomlSerializerOptions { MetadataStore = metadataStore };
        var orderedRoot = new TomlTable();
        if (root.TryGetValue("drawers", out var drawers))
        {
            switch (drawers)
            {
                case TomlTableArray tableArray:
                    orderedRoot["drawers"] = CreateDrawerTableArray(tableArray, metadataStore);
                    break;
                default:
                    orderedRoot["drawers"] = drawers;
                    break;
            }
        }

        if (root.TryGetValue("startup", out var startup))
        {
            if (startup is TomlTable startupTable)
            {
                var standardStartupTable = new TomlTable();
                foreach (var key in startupTable.Keys)
                {
                    if (string.Equals(key, ObsoleteIconSizeSettingName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    standardStartupTable[key] = startupTable[key];
                }

                AddSectionFormatting(standardStartupTable, metadataStore,
                    "启动与主窗口设置",
                    "配置登录启动、窗口位置、主题颜色和主窗口高度。");
                orderedRoot["startup"] = standardStartupTable;
            }
            else
            {
                orderedRoot["startup"] = startup;
            }
        }

        foreach (var key in root.Keys)
        {
            if (key is not "drawers" and not "startup")
            {
                orderedRoot[key] = root[key];
            }
        }

        return TomlSerializer.Serialize(orderedRoot, options);
    }

    private TomlTableArray CreateDrawerTableArray(TomlTableArray drawers, TomlMetadataStore metadataStore)
    {
        var result = new TomlTableArray();
        foreach (var drawer in drawers)
        {
            result.Add(CreateDrawerTable(drawer, metadataStore));
        }

        return result;
    }

    private static TomlTable CreateDrawerTable(TomlTable source, TomlMetadataStore metadataStore)
    {
        var drawer = new TomlTable();
        foreach (var key in source.Keys)
        {
            drawer[key] = source[key];
        }

        // AddSectionFormatting(drawer, metadataStore,
        //     "抽屉目录",
        //     "name 为显示名称，path 为目录路径，size 为窗口尺寸；每个抽屉单独配置。");
        return drawer;
    }

    private void AddSectionFormatting(
        TomlTable table,
        TomlMetadataStore metadataStore,
        string title,
        string description)
    {
        var keys = table.Keys.ToList();
        if (keys.Count == 0)
        {
            return;
        }

        var firstKey = keys[0];
        var lastKey = keys[^1];
        var metadata = new TomlPropertiesMetadata();
        metadata.SetProperty(firstKey, new TomlPropertyMetadata
        {
            LeadingTrivia = new List<TomlSyntaxTriviaMetadata>
            {
                new(TokenKind.Comment, $"# {title}"),
                new(TokenKind.NewLine, "\n"),
                new(TokenKind.Comment, $"# {description}"),
                new(TokenKind.NewLine, "\n")
            }
        });
        if (firstKey != lastKey)
        {
            metadata.SetProperty(lastKey, new TomlPropertyMetadata
            {
                TrailingTriviaAfterEndOfLine = new List<TomlSyntaxTriviaMetadata>
                {
                    new(TokenKind.NewLine, "\n"),
                    // new(TokenKind.NewLine, "\n")
                }
            });
        }

        metadataStore.SetProperties(table, metadata);
    }
}
