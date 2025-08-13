param($ProjectPath)

# - - - - - - - - - - - - - - - - - - - -
# 根据 SetupModel.json 生成 SetupEntries.g.cs、Setup.g.cs
# 根据 SetupListener.cs 生成 SetupListenerRegisterer.cs
# - - - - - - - - - - - - - - - - - - - -

# 获取 SetupModel.json 文件内容

$setupModelFilePath = Join-Path -Path $ProjectPath -ChildPath 'ProgramSetup\SetupModel.json'
$setupJson = Get-Content -Path $setupModelFilePath -Raw | ConvertFrom-Json

# - - - - - - - - - - - - - - - - - - - -
# 生成 SetupEntries.g.cs & Setup.g.cs
# - - - - - - - - - - - - - - - - - - - -

# 处理整个 json 对象，构造 SetupEntries 中存配置项的嵌套静态类、Setup 中的嵌套静态类的属性、记录所有 SetupEntry 旧名称->新名称的字典

$entryModelCode = [System.Text.StringBuilder]::new()
$propertyModelCode = [System.Text.StringBuilder]::new()
$allSetupEntries = [System.Collections.Generic.Dictionary[string, string]]::new()
$entryDictionaryCode = [System.Text.StringBuilder]::new()

function Format-Value($value)
{
    if ($null -eq $value) {
        throw "SetupModel.json 中某个默认值为 null，不受支持。"
    }

    if ($value -is [string]) {
        return [PSCustomObject]@{
            FormattedValue = "`"$value`""
            Type          = 'string'
            GetMethod     = 'GetString'
            SetMethod     = 'SetString'
        }
    }

    if ($value -is [bool]) {
        return [PSCustomObject]@{
            FormattedValue = $value.ToString().ToLowerInvariant()
            Type          = 'bool'
            GetMethod     = 'GetBool'
            SetMethod     = 'SetBool'
        }
    }

    # 仅支持 C# 侧的 Int32：在 PowerShell 7+/非 Windows 下，JSON 数字常为 [long]/[double]/[decimal]
    $i = $null
    if ($value -is [int]) {
        $i = [int]$value
    }
    elseif ($value -is [long]) {
        if ($value -lt [int]::MinValue -or $value -gt [int]::MaxValue) {
            throw "SetupModel.json 中存在超出 Int32 范围的整数值：$value"
        }
        $i = [int]$value
    }
    elseif ($value -is [double] -or $value -is [decimal]) {
        $d = [double]$value
        if ($d -ne [math]::Truncate($d)) {
            throw "SetupModel.json 中存在非整数数值：$value"
        }
        if ($d -lt [double][int]::MinValue -or $d -gt [double][int]::MaxValue) {
            throw "SetupModel.json 中存在超出 Int32 范围的整数值：$value"
        }
        $i = [int][math]::Truncate($d)
    }
    else {
        throw "不支持的默认值类型：$($value.GetType().FullName)"
    }

    return [PSCustomObject]@{
        FormattedValue = $i.ToString()
        Type          = 'int'
        GetMethod     = 'GetInt32'
        SetMethod     = 'SetInt32'
    }
}

function Format-EntrySource($raw) {
    if ($raw -eq 'local') { return 'SetupEntrySource.PathLocal' }
    if ($raw -eq 'global') { return 'SetupEntrySource.SystemGlobal' }
    if ($raw -eq 'instance') { return 'SetupEntrySource.GameInstance' }
}

function Process-Namespace($namespaceStore, $currentJson) {
    $indent = '    ' * ($namespaceStore.Count + 1)
    foreach ($childPSProp in $currentJson.PSObject.Properties) {
        if ($childPSProp.Name -like 'ns:*') { # 子命名空间
            $childNamespaceName = $childPSProp.Name -replace '^ns:'
            # 输出子命名空间的 EntryModel 类定义
            [void] $entryModelCode.Append($indent).AppendLine("public static class $childNamespaceName")
            [void] $entryModelCode.Append($indent).AppendLine('{')
            # 输出子命名空间的 PropertyModel 类定义
            [void] $propertyModelCode.Append($indent).AppendLine("public static class $childNamespaceName")
            [void] $propertyModelCode.Append($indent).AppendLine('{')
            # 递归处理子命名空间
            Process-Namespace ($namespaceStore + $childNamespaceName) $childPSProp.Value
            # 输出子命名空间的 EntryModel 类定义
            [void] $entryModelCode.Append($indent).AppendLine('}')
            # 输出子命名空间的 PropertyModel 类定义
            [void] $propertyModelCode.Append($indent).AppendLine('}')
        } else { # 子条目
            # 获取子条目的信息
            $isEntryDynamic = $childPSProp.Name -like 'dyn:*'
            $entryName = $childPSProp.Name -replace '^dyn:'
            $childEntry = $childPSProp.Value
            $entrySource = $childEntry.source
            $formattedEntrySource = Format-EntrySource $entrySource
            $entryKey = $childEntry.key
            $entryValue = Format-Value $childEntry.value
            $namespaceStr = $namespaceStore -join '.'
            $formattedEntrypted = $childEntry.encrypted.ToString().ToLower()
            # 输出 EntryModel 代码
            if ($isEntryDynamic) {
                [void] $entryModelCode.Append($indent).AppendLine(
                        "public static SetupEntry $entryName => global::PCL.Core.ProgramSetup.SetupEntries.ForKeyName(`"$entryKey`")!;")
            } else {
                [void] $entryModelCode.Append($indent).AppendLine(
                        "public static readonly SetupEntry $entryName = new SetupEntry($formattedEntrySource, `"$entryKey`", $($entryValue.FormattedValue), $formattedEntrypted);")
            }
            # 输出 PropertyModel 代码
            if ($entrySource -ne 'instance') {
                [void] $propertyModelCode.Append($indent).AppendLine("public static $($entryValue.Type) $entryName")
                [void] $propertyModelCode.Append($indent).AppendLine('{')
                [void] $propertyModelCode.Append($indent).AppendLine("    get => SetupService.$($entryValue.GetMethod)(SetupEntries.$namespaceStr.$entryName);")
                [void] $propertyModelCode.Append($indent).AppendLine("    set => SetupService.$($entryValue.SetMethod)(SetupEntries.$namespaceStr.$entryName, value);")
                [void] $propertyModelCode.Append($indent).AppendLine('}')
            } else {
                [void] $propertyModelCode.Append($indent).AppendLine("public static PCL.Core.Utils.ParameterizedProperty<string, $($entryValue.Type)> $entryName = new()")
                [void] $propertyModelCode.Append($indent).AppendLine('{')
                [void] $propertyModelCode.Append($indent).AppendLine("    GetValue = gamePath => SetupService.$($entryValue.GetMethod)(SetupEntries.$namespaceStr.$entryName, gamePath),")
                [void] $propertyModelCode.Append($indent).AppendLine("    SetValue = (gamePath, value) => SetupService.$($entryValue.SetMethod)(SetupEntries.$namespaceStr.$entryName, value, gamePath)")
                [void] $propertyModelCode.Append($indent).AppendLine('};')
            }
            # 输出 EntryDictionary 字典初始化代码
            if (-not $isEntryDynamic) {
                [void] $entryDictionaryCode.AppendLine("        [`"$entryKey`"] = $namespaceStr.$entryName,")
            }
        }
    }
}

Process-Namespace @() $setupJson

# 输出 SetupEntries.g.cs & Setup.g.cs

$setupEntriesFilePath = Join-Path -Path $ProjectPath -ChildPath 'ProgramSetup\SetupEntries.g.cs'

@"
// ** 使用 IDE 删除该文件可能导致项目配置文件中的引用被删除，请谨慎操作 **
// <auto-generated />
// 此文件由 MSBuild 自动生成，请勿手动修改，而是修改 SetupModel.json 文件

#nullable enable

namespace PCL.Core.ProgramSetup;

public static class SetupEntries
{
$entryModelCode
    /// <summary>
    /// 根据 KeyName 获取一个 SetupEntry，如 VersionAdvanceJvm => SetupEntries.Instance.JvmArgs
    /// </summary>
    /// <param name="keyName">要获取的键名</param>
    /// <returns>找到的 SetupEntry，如果未找到则返回 <see langword="null"/></returns>
    public static SetupEntry? ForKeyName(string keyName)
    {
        if (keyName is null)
            return null;
        return EntryDictionary.TryGetValue(keyName, out var value) ? value : null;
    }

    /// <summary>
    /// 注册一个配置项，用于动态配置项的初始化
    /// </summary>
    /// <param name="keyName">键名</param>
    /// <param name="source">配置源</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="isEntrypted">是否加密</param>
    /// <exception cref="global::System.ArgumentException">已存在该键名</exception>
    public static void RegisterEntry(string keyName, SetupEntrySource source, object defaultValue, bool isEntrypted = false)
    {
        if (!EntryDictionary.TryAdd(keyName, new SetupEntry(source, keyName, defaultValue, isEntrypted)))
            throw new global::System.ArgumentException($"键名 {keyName} 已存在于配置项字典中");
    }

    /// <summary>
    /// 存储了已记录的 SetupEntry，字典键为其 KeyName，用于 <see cref="ForKeyName"/>
    /// </summary>
    public static global::System.Collections.Concurrent.ConcurrentDictionary<string, SetupEntry> EntryDictionary = new()
    {
$entryDictionaryCode
    };
}
"@ | Out-File -FilePath $setupEntriesFilePath

$setupFilePath = Join-Path -Path $ProjectPath -ChildPath 'ProgramSetup\Setup.g.cs'

@"
// ** 使用 IDE 删除该文件可能导致项目配置文件中的引用被删除，请谨慎操作 **
// <auto-generated />
// 此文件由 MSBuild 自动生成，请勿手动修改，而是修改 SetupModel.json 文件

namespace PCL.Core.ProgramSetup;

public static class Setup
{
$propertyModelCode
}
"@ | Out-File -FilePath $setupFilePath

# - - - - - - - - - - - - - - - - - - - -
# 生成 SetupListenerRegisterer.g.cs
# - - - - - - - - - - - - - - - - - - - -

# 用正则匹配注解的参数值、方法名、方法第一个参数类型

$setupListenerFilePath = Join-Path -Path $ProjectPath -ChildPath 'ProgramSetup\SetupListener.cs'
$setupListenerFileContent = Get-Content -Path $setupListenerFilePath -Raw

$setupListenerPattern = '\[\s*ListenSetupChanged\s*\(\s*"([^"]*)"\s*\)\s*\]\s*\w*\s+static\s+void\s+(\w+)\s*\(\s*(\w+)'
$setupListenerMaches = [regex]::Matches($setupListenerFileContent, $setupListenerPattern)

$setupListeners = [System.Collections.Generic.Dictionary[string, System.Collections.Generic.List[System.Tuple[string, string]]]]::new()

[regex]::Matches($setupListenerFileContent, $setupListenerPattern) | ForEach-Object {
    $setupKey = $_.Groups[1].Value
    $methodName = $_.Groups[2].Value
    $paramType = $_.Groups[3].Value
    $tuple = [System.Tuple]::Create($methodName, $paramType)
    if ($setupListeners.ContainsKey($setupKey)) {
        $setupListeners[$setupKey].Add($tuple)
    } else {
        $list = [System.Collections.Generic.List[System.Tuple[string, string]]]::new()
        $list.Add($tuple)
        $setupListeners[$setupKey] = $list
    }
}

# 生成代码，功能是为字典添加值

$setupListenerRegistererCode = [System.Text.StringBuilder]::new()

$setupListeners.GetEnumerator() | ForEach-Object {
    $setupKey = $_.Key
    [void] $setupListenerRegistererCode.Append('        ').Append("dict[`"$setupKey`"] = [")
    $_.Value.GetEnumerator() | ForEach-Object {
        $methodName = $_.Item1
        $paramType = $_.Item2
        if ($paramType -eq 'object') {
            [void] $setupListenerRegistererCode.Append("SetupListener.$methodName, ")
        } else{
            [void] $setupListenerRegistererCode.Append("(o, n, p) => SetupListener.$methodName(($paramType)o, ($paramType)n, p), ")
        }
    }
    [void] $setupListenerRegistererCode.AppendLine('];')
}

# 输出 SetupListenerRegisterer.g.cs

$setupListenerRegistererFilePath = Join-Path -Path $ProjectPath -ChildPath 'ProgramSetup\SetupListenerRegisterer.g.cs'

@"
// ** 使用 IDE 删除该文件可能导致项目配置文件中的引用被删除，请谨慎操作 **
// <auto-generated />
// 此文件由 MSBuild 自动生成，请勿手动修改，而是修改 SetupListener.cs 文件

namespace PCL.Core.ProgramSetup;

public static class SetupListenerRegisterer
{
    public static void RegisterClassListeners(System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<SetupListener.ValueChangedHandler>> dict)
    {
$setupListenerRegistererCode
    }
}
"@ | Out-File -FilePath $setupListenerRegistererFilePath
