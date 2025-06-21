param($ProjectPath)

# 函数：动态获取 LifecycleState 枚举的所有有效值
function Get-ValidLifecycleStates {
    param($ProjectPath)
    
    try {
        # 查找 LifecycleState.cs 文件
        $lifecycleStateFile = Get-ChildItem -Path $ProjectPath -Filter "LifecycleState.cs" -Recurse | Select-Object -First 1
        
        if (-not $lifecycleStateFile) {
            Write-Warning "LifecycleState.cs not found"
            return @()
        }
        
        Write-Host "Found LifecycleState.cs: $($lifecycleStateFile.FullName)"
        
        # 读取文件内容
        $content = Get-Content $lifecycleStateFile.FullName -Raw
        
        # 提取枚举定义块
        $enumPattern = '(?s)public enum LifecycleState\s*\{(.*?)\}'
        $enumMatch = [regex]::Match($content, $enumPattern)
        
        if (-not $enumMatch.Success) {
            Write-Warning "Cannot find enum define in the file"
            return @()
        }
        
        $enumContent = $enumMatch.Groups[1].Value
        $validStates = @()
        
        # 按行分割并处理每一行
        $lines = $enumContent -split "`n"
        foreach ($line in $lines) {
            $trimmedLine = $line.Trim()
            
            # 跳过空行、注释行和花括号
            if ($trimmedLine -eq '' -or 
                $trimmedLine.StartsWith('///') -or 
                $trimmedLine.StartsWith('//') -or 
                $trimmedLine.StartsWith('/*') -or 
                $trimmedLine.StartsWith('*') -or 
                $trimmedLine -eq '{' -or 
                $trimmedLine -eq '}') {
                continue
            }
            
            # 匹配枚举成员（可能包含逗号）
            if ($trimmedLine -match '^(\w+)\s*,?\s*$') {
                $enumValue = $matches[1]
                if ($enumValue -and $enumValue -ne 'LifecycleState') {
                    $validStates += $enumValue
                }
            }
        }
        
        if ($validStates.Count -gt 0) {
            Write-Host "Found state(s): $($validStates -join ', ')"
        } else {
            Write-Warning "No state found"
        }
        
        return $validStates
    }
    catch {
        Write-Warning "Error reading LifecycleState.cs: $($_.Exception.Message)"
        return @()
    }
}

# 获取有效的生命周期状态
$validStates = Get-ValidLifecycleStates -ProjectPath $ProjectPath

# 如果没有获取到有效状态，退出脚本
if ($validStates.Count -eq 0) {
    Write-Error "Error reading lifecycle states"
    exit 1
}

$sourceFiles = Get-ChildItem -Path $ProjectPath -Filter "*.cs" -Recurse
$lifecycleServices = @{}

foreach ($file in $sourceFiles) {
    $content = Get-Content $file.FullName -Raw

    # 匹配 LifecycleService 注解的各种写法
    $patterns = @(
        '\[LifecycleService\(\s*LifecycleState\.(\w+)\s*\)\][\s\S]*?class\s+(\w+)',
        '\[LifecycleService\(\s*LifecycleState\.(\w+)\s*,\s*Priority\s*=\s*([^,\)]+)\)\][\s\S]*?class\s+(\w+)'
    )

    foreach ($pattern in $patterns) {
        $matches = [regex]::Matches($content, $pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)

        foreach ($match in $matches) {
            $state = $match.Groups[1].Value
            
            # 验证状态是否有效
            if ($validStates -notcontains $state) {
                continue
            }
            
            $priority = 0
            $className = ""

            if ($match.Groups.Count -eq 3) {
                # 没有 Priority 的情况
                $className = $match.Groups[2].Value
            } elseif ($match.Groups.Count -eq 4) {
                # 有 Priority 的情况
                $priorityStr = $match.Groups[2].Value.Trim()
                $className = $match.Groups[3].Value
                
                # 处理各种 Priority 值的写法
                if ($priorityStr -match '^\d+$') {
                    # 纯数字
                    $priority = [int]$priorityStr
                } elseif ($priorityStr -eq 'int.MaxValue') {
                    $priority = [int]::MaxValue
                } elseif ($priorityStr -eq 'int.MinValue') {
                    $priority = [int]::MinValue
                } elseif ($priorityStr -match '^-?\d+$') {
                    # 负数
                    $priority = [int]$priorityStr
                } else {
                    # 其他表达式，尝试评估或使用默认值
                    try {
                        # 尝试简单的数学表达式评估
                        $priority = Invoke-Expression $priorityStr
                    } catch {
                        $priority = 0
                    }
                }
            }

            # 提取命名空间
            $namespacePattern = 'namespace\s+([\w\.]+)\s*[;{]'
            $namespaceMatch = [regex]::Match($content, $namespacePattern)
            $namespace = if ($namespaceMatch.Success) { 
                $namespaceMatch.Groups[1].Value 
            } else { 
                "PCL.Core" # 默认命名空间，作为后备
            }

            if (-not $lifecycleServices.ContainsKey($state)) {
                $lifecycleServices[$state] = @()
            }

            $lifecycleServices[$state] += @{
                ClassName = $className
                Priority = $priority
                FullName = "$namespace.$className"
            }
        }
    }
}

# 按 Priority 降序排序每个状态的服务
$states = @($lifecycleServices.Keys)
foreach ($state in $states) {
    $lifecycleServices[$state] = $lifecycleServices[$state] | Sort-Object Priority -Descending
}

# 生成 C# 代码
$csharpCode = @"
// <auto-generated />
// 此文件由 MSBuild 自动生成，请勿手动修改

using System;

namespace PCL.Core.Lifecycle
{
    /// <summary>
    /// 包含所有使用 LifecycleService 注解的类型，按 StartState 分类并按 Priority 降序排序
    /// </summary>
    public static class LifecycleServiceTypes
    {
"@

foreach ($state in $lifecycleServices.Keys | Sort-Object) {
    $services = $lifecycleServices[$state]
    $csharpCode += "`n        /// <summary>`n"
    $csharpCode += "        /// $state 状态的生命周期服务类型`n"
    $csharpCode += "        /// </summary>`n"
    $csharpCode += "        public static readonly Type[] $state = new Type[]`n"
    $csharpCode += "        {`n"

    foreach ($service in $services) {
        $csharpCode += "            typeof($($service.FullName)), // Priority: $($service.Priority)`n"
    }

    $csharpCode += "        };`n"
}

$csharpCode += @"

        /// <summary>
        /// 获取指定生命周期状态的所有服务类型
        /// </summary>
        /// <param name="state">生命周期状态</param>
        /// <returns>该状态下的所有服务类型数组</returns>
        public static Type[] GetServiceTypes(LifecycleState state)
        {
            return state switch
            {
"@

foreach ($state in $lifecycleServices.Keys | Sort-Object) {
    $csharpCode += "`n                LifecycleState.$state => $state,"
}

$csharpCode += "`n"
$csharpCode += @"
                _ => new Type[0]
            };
        }

        /// <summary>
        /// 获取所有生命周期服务类型的状态映射
        /// </summary>
        /// <returns>状态到类型数组的字典</returns>
        public static System.Collections.Generic.Dictionary<LifecycleState, Type[]> GetAllServiceTypes()
        {
            return new System.Collections.Generic.Dictionary<LifecycleState, Type[]>
            {
"@

foreach ($state in $lifecycleServices.Keys | Sort-Object) {
    $csharpCode += "`n                { LifecycleState.$state, $state },"
}

$csharpCode += "`n"
$csharpCode += @"
            };
        }
    }
}
"@

# 将生成的代码写入文件
$outputPath = Join-Path $ProjectPath "Lifecycle\LifecycleServiceTypes.g.cs"
$csharpCode | Out-File -FilePath $outputPath -Encoding UTF8

Write-Host "Generated Lifecycle\LifecycleServiceTypes.g.cs with $($lifecycleServices.Keys.Count) different state(s)"