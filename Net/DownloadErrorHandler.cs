using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using PCL.Core.Logging;

namespace PCL.Core.Net;

/// <summary>
/// 下载错误类型
/// </summary>
public enum DownloadErrorType
{
    /// <summary>
    /// 网络连接错误
    /// </summary>
    NetworkConnection,
    
    /// <summary>
    /// HTTP错误
    /// </summary>
    HttpError,
    
    /// <summary>
    /// 文件IO错误
    /// </summary>
    FileIo,
    
    /// <summary>
    /// 服务器错误
    /// </summary>
    ServerError,
    
    /// <summary>
    /// 认证错误
    /// </summary>
    Authentication,
    
    /// <summary>
    /// 超时错误
    /// </summary>
    Timeout,
    
    /// <summary>
    /// 取消操作
    /// </summary>
    Cancelled,
    
    /// <summary>
    /// 配置错误
    /// </summary>
    Configuration,
    
    /// <summary>
    /// 未知错误
    /// </summary>
    Unknown
}

/// <summary>
/// 错误严重程度
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// 信息级别
    /// </summary>
    Info,
    
    /// <summary>
    /// 警告级别
    /// </summary>
    Warning,
    
    /// <summary>
    /// 错误级别
    /// </summary>
    Error,
    
    /// <summary>
    /// 严重错误级别
    /// </summary>
    Critical
}

/// <summary>
/// 下载错误信息
/// </summary>
public class DownloadError
{
    /// <summary>
    /// 错误ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 发生时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 错误类型
    /// </summary>
    public DownloadErrorType Type { get; set; }
    
    /// <summary>
    /// 错误严重程度
    /// </summary>
    public ErrorSeverity Severity { get; set; }
    
    /// <summary>
    /// 错误代码
    /// </summary>
    public string ErrorCode { get; set; } = "";
    
    /// <summary>
    /// 错误消息
    /// </summary>
    public string Message { get; set; } = "";
    
    /// <summary>
    /// 详细描述
    /// </summary>
    public string Details { get; set; } = "";
    
    /// <summary>
    /// 原始异常
    /// </summary>
    public Exception? OriginalException { get; set; }
    
    /// <summary>
    /// 上下文信息
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();
    
    /// <summary>
    /// 建议的解决方案
    /// </summary>
    public List<string> SuggestedSolutions { get; set; } = new();
    
    /// <summary>
    /// 是否可重试
    /// </summary>
    public bool IsRetryable { get; set; } = true;
    
    /// <summary>
    /// 重试延迟（毫秒）
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;
    
    /// <summary>
    /// 已重试次数
    /// </summary>
    public int RetryCount { get; set; } = 0;
    
    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    public override string ToString()
    {
        return $"[{Type}] {ErrorCode}: {Message}";
    }
}

/// <summary>
/// 错误恢复策略
/// </summary>
public enum ErrorRecoveryStrategy
{
    /// <summary>
    /// 立即重试
    /// </summary>
    ImmediateRetry,
    
    /// <summary>
    /// 延迟重试
    /// </summary>
    DelayedRetry,
    
    /// <summary>
    /// 指数退避重试
    /// </summary>
    ExponentialBackoff,
    
    /// <summary>
    /// 降级处理（如切换到单线程）
    /// </summary>
    Fallback,
    
    /// <summary>
    /// 终止操作
    /// </summary>
    Abort,
    
    /// <summary>
    /// 跳过当前任务
    /// </summary>
    Skip
}

/// <summary>
/// 错误恢复结果
/// </summary>
public class ErrorRecoveryResult
{
    /// <summary>
    /// 采用的策略
    /// </summary>
    public ErrorRecoveryStrategy Strategy { get; set; }
    
    /// <summary>
    /// 是否成功恢复
    /// </summary>
    public bool IsRecovered { get; set; }
    
    /// <summary>
    /// 恢复消息
    /// </summary>
    public string Message { get; set; } = "";
    
    /// <summary>
    /// 额外参数
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// 下载错误处理器
/// 提供错误分析、诊断和自动恢复功能
/// </summary>
public class DownloadErrorHandler
{
    private const string LogModule = "DownloadErrorHandler";
    
    private readonly Dictionary<DownloadErrorType, Func<DownloadError, ErrorRecoveryResult>> _recoveryHandlers = new();
    private readonly List<DownloadError> _errorHistory = new();
    private readonly object _lockObject = new();
    
    /// <summary>
    /// 最大错误历史记录数
    /// </summary>
    public int MaxErrorHistoryCount { get; set; } = 100;
    
    /// <summary>
    /// 启用自动恢复
    /// </summary>
    public bool EnableAutoRecovery { get; set; } = true;
    
    /// <summary>
    /// 错误历史记录
    /// </summary>
    public IReadOnlyList<DownloadError> ErrorHistory
    {
        get
        {
            lock (_lockObject)
            {
                return _errorHistory.ToArray();
            }
        }
    }
    
    /// <summary>
    /// 错误发生事件
    /// </summary>
    public event Action<DownloadError>? ErrorOccurred;
    
    /// <summary>
    /// 错误恢复事件
    /// </summary>
    public event Action<DownloadError, ErrorRecoveryResult>? ErrorRecovered;
    
    public DownloadErrorHandler()
    {
        InitializeDefaultRecoveryHandlers();
        LogWrapper.Info(LogModule, "下载错误处理器已初始化");
    }
    
    /// <summary>
    /// 处理异常并转换为下载错误
    /// </summary>
    /// <param name="exception">异常实例</param>
    /// <param name="context">上下文信息</param>
    /// <returns>下载错误实例</returns>
    public DownloadError HandleException(Exception exception, Dictionary<string, object>? context = null)
    {
        var error = AnalyzeException(exception);
        
        // 添加上下文信息
        if (context != null)
        {
            foreach (var kvp in context)
            {
                error.Context[kvp.Key] = kvp.Value;
            }
        }
        
        // 记录错误
        RecordError(error);
        
        // 触发事件
        ErrorOccurred?.Invoke(error);
        
        LogWrapper.Warn(LogModule, $"处理下载错误: {error}");
        
        return error;
    }
    
    /// <summary>
    /// 尝试恢复错误
    /// </summary>
    /// <param name="error">错误实例</param>
    /// <returns>恢复结果</returns>
    public async Task<ErrorRecoveryResult> TryRecoverAsync(DownloadError error)
    {
        if (!EnableAutoRecovery || !error.IsRetryable)
        {
            return new ErrorRecoveryResult
            {
                Strategy = ErrorRecoveryStrategy.Abort,
                IsRecovered = false,
                Message = "自动恢复已禁用或错误不可重试"
            };
        }
        
        ErrorRecoveryResult result;
        
        if (_recoveryHandlers.TryGetValue(error.Type, out var handler))
        {
            result = handler(error);
        }
        else
        {
            result = HandleGenericError(error);
        }
        
        // 应用恢复策略
        if (result.IsRecovered && result.Strategy == ErrorRecoveryStrategy.DelayedRetry)
        {
            var delay = result.Parameters.ContainsKey("DelayMs") 
                ? (int)result.Parameters["DelayMs"] 
                : error.RetryDelayMs;
            await Task.Delay(delay);
        }
        
        // 更新错误状态
        error.RetryCount++;
        
        // 触发恢复事件
        ErrorRecovered?.Invoke(error, result);
        
        LogWrapper.Info(LogModule, $"错误恢复尝试: {error.Id}, 策略: {result.Strategy}, 成功: {result.IsRecovered}");
        
        return result;
    }
    
    /// <summary>
    /// 分析异常并创建下载错误
    /// </summary>
    private DownloadError AnalyzeException(Exception exception)
    {
        var error = new DownloadError
        {
            OriginalException = exception,
            Message = exception.Message,
            Details = exception.ToString()
        };
        
        // 分析异常类型
        switch (exception)
        {
            case HttpRequestException httpEx:
                error.Type = DownloadErrorType.HttpError;
                error.Severity = ErrorSeverity.Error;
                error.ErrorCode = "HTTP_REQUEST_FAILED";
                AnalyzeHttpException(error, httpEx);
                break;
                
            case TaskCanceledException cancelEx when cancelEx.InnerException is TimeoutException:
                error.Type = DownloadErrorType.Timeout;
                error.Severity = ErrorSeverity.Warning;
                error.ErrorCode = "TIMEOUT";
                error.SuggestedSolutions.AddRange(new[]
                {
                    "增加超时时间",
                    "检查网络连接",
                    "减少并发线程数"
                });
                break;
                
            case OperationCanceledException:
                error.Type = DownloadErrorType.Cancelled;
                error.Severity = ErrorSeverity.Info;
                error.ErrorCode = "OPERATION_CANCELLED";
                error.IsRetryable = false;
                break;
                
            case UnauthorizedAccessException:
            case System.Security.Authentication.AuthenticationException:
                error.Type = DownloadErrorType.Authentication;
                error.Severity = ErrorSeverity.Error;
                error.ErrorCode = "AUTHENTICATION_FAILED";
                error.SuggestedSolutions.AddRange(new[]
                {
                    "检查访问凭据",
                    "验证用户权限",
                    "检查认证配置"
                });
                break;
                
            case System.IO.IOException ioEx:
                error.Type = DownloadErrorType.FileIo;
                error.Severity = ErrorSeverity.Error;
                error.ErrorCode = "FILE_IO_ERROR";
                AnalyzeFileIOException(error, ioEx);
                break;
                
            case System.Net.NetworkInformation.NetworkInformationException:
            case System.Net.Sockets.SocketException:
                error.Type = DownloadErrorType.NetworkConnection;
                error.Severity = ErrorSeverity.Error;
                error.ErrorCode = "NETWORK_CONNECTION_FAILED";
                error.SuggestedSolutions.AddRange(new[]
                {
                    "检查网络连接",
                    "检查防火墙设置",
                    "尝试使用代理"
                });
                break;
                
            case ArgumentException argEx:
                error.Type = DownloadErrorType.Configuration;
                error.Severity = ErrorSeverity.Error;
                error.ErrorCode = "INVALID_CONFIGURATION";
                error.Message = $"配置错误: {argEx.Message}";
                error.IsRetryable = false;
                break;
                
            default:
                error.Type = DownloadErrorType.Unknown;
                error.Severity = ErrorSeverity.Error;
                error.ErrorCode = "UNKNOWN_ERROR";
                error.SuggestedSolutions.Add("查看详细错误信息以获取更多帮助");
                break;
        }
        
        return error;
    }
    
    /// <summary>
    /// 分析HTTP异常
    /// </summary>
    private void AnalyzeHttpException(DownloadError error, HttpRequestException httpEx)
    {
        if (httpEx.Message.Contains("404"))
        {
            error.ErrorCode = "HTTP_404_NOT_FOUND";
            error.Message = "文件未找到";
            error.IsRetryable = false;
            error.SuggestedSolutions.Add("检查下载链接是否正确");
        }
        else if (httpEx.Message.Contains("403"))
        {
            error.ErrorCode = "HTTP_403_FORBIDDEN";
            error.Message = "访问被禁止";
            error.IsRetryable = false;
            error.SuggestedSolutions.AddRange(new[]
            {
                "检查访问权限",
                "检查User-Agent设置",
                "检查访问频率限制"
            });
        }
        else if (httpEx.Message.Contains("500"))
        {
            error.Type = DownloadErrorType.ServerError;
            error.ErrorCode = "HTTP_500_SERVER_ERROR";
            error.Message = "服务器内部错误";
            error.SuggestedSolutions.AddRange(new[]
            {
                "稍后重试",
                "联系服务器管理员",
                "尝试其他下载源"
            });
        }
        else if (httpEx.Message.Contains("502") || httpEx.Message.Contains("503"))
        {
            error.Type = DownloadErrorType.ServerError;
            error.ErrorCode = "HTTP_SERVER_UNAVAILABLE";
            error.Message = "服务器不可用";
            error.RetryDelayMs = 5000; // 5秒后重试
        }
    }
    
    /// <summary>
    /// 分析文件IO异常
    /// </summary>
    private void AnalyzeFileIOException(DownloadError error, System.IO.IOException ioEx)
    {
        if (ioEx.Message.Contains("space") || ioEx.Message.Contains("disk"))
        {
            error.ErrorCode = "INSUFFICIENT_DISK_SPACE";
            error.Message = "磁盘空间不足";
            error.IsRetryable = false;
            error.Severity = ErrorSeverity.Critical;
            error.SuggestedSolutions.AddRange(new[]
            {
                "清理磁盘空间",
                "选择其他存储位置",
                "删除不必要的文件"
            });
        }
        else if (ioEx.Message.Contains("access") || ioEx.Message.Contains("permission"))
        {
            error.ErrorCode = "FILE_ACCESS_DENIED";
            error.Message = "文件访问被拒绝";
            error.IsRetryable = false;
            error.SuggestedSolutions.AddRange(new[]
            {
                "检查文件权限",
                "以管理员身份运行",
                "选择其他存储位置"
            });
        }
        else if (ioEx.Message.Contains("use") || ioEx.Message.Contains("locked"))
        {
            error.ErrorCode = "FILE_IN_USE";
            error.Message = "文件正在被使用";
            error.RetryDelayMs = 2000; // 2秒后重试
            error.SuggestedSolutions.AddRange(new[]
            {
                "关闭正在使用该文件的程序",
                "稍后重试",
                "选择不同的文件名"
            });
        }
    }
    
    /// <summary>
    /// 记录错误到历史记录
    /// </summary>
    private void RecordError(DownloadError error)
    {
        lock (_lockObject)
        {
            _errorHistory.Add(error);
            
            // 限制历史记录数量
            while (_errorHistory.Count > MaxErrorHistoryCount)
            {
                _errorHistory.RemoveAt(0);
            }
        }
    }
    
    /// <summary>
    /// 初始化默认恢复处理器
    /// </summary>
    private void InitializeDefaultRecoveryHandlers()
    {
        _recoveryHandlers[DownloadErrorType.NetworkConnection] = HandleNetworkError;
        _recoveryHandlers[DownloadErrorType.HttpError] = HandleHttpError;
        _recoveryHandlers[DownloadErrorType.ServerError] = HandleServerError;
        _recoveryHandlers[DownloadErrorType.Timeout] = HandleTimeoutError;
        _recoveryHandlers[DownloadErrorType.FileIo] = HandleFileIOError;
    }
    
    /// <summary>
    /// 处理网络错误
    /// </summary>
    private ErrorRecoveryResult HandleNetworkError(DownloadError error)
    {
        if (error.RetryCount < error.MaxRetries)
        {
            return new ErrorRecoveryResult
            {
                Strategy = ErrorRecoveryStrategy.ExponentialBackoff,
                IsRecovered = true,
                Message = "网络错误，使用指数退避重试",
                Parameters = new Dictionary<string, object>
                {
                    ["DelayMs"] = Math.Min(error.RetryDelayMs * (int)Math.Pow(2, error.RetryCount), 30000)
                }
            };
        }
        
        return new ErrorRecoveryResult
        {
            Strategy = ErrorRecoveryStrategy.Abort,
            IsRecovered = false,
            Message = "网络错误重试次数已达上限"
        };
    }
    
    /// <summary>
    /// 处理HTTP错误
    /// </summary>
    private ErrorRecoveryResult HandleHttpError(DownloadError error)
    {
        // 4xx错误通常不需要重试
        if (error.ErrorCode.Contains("404") || error.ErrorCode.Contains("403"))
        {
            return new ErrorRecoveryResult
            {
                Strategy = ErrorRecoveryStrategy.Abort,
                IsRecovered = false,
                Message = "HTTP客户端错误，不需要重试"
            };
        }
        
        // 其他HTTP错误可以重试
        if (error.RetryCount < error.MaxRetries)
        {
            return new ErrorRecoveryResult
            {
                Strategy = ErrorRecoveryStrategy.DelayedRetry,
                IsRecovered = true,
                Message = "HTTP错误，延迟重试",
                Parameters = new Dictionary<string, object>
                {
                    ["DelayMs"] = error.RetryDelayMs
                }
            };
        }
        
        return new ErrorRecoveryResult
        {
            Strategy = ErrorRecoveryStrategy.Abort,
            IsRecovered = false,
            Message = "HTTP错误重试次数已达上限"
        };
    }
    
    /// <summary>
    /// 处理服务器错误
    /// </summary>
    private ErrorRecoveryResult HandleServerError(DownloadError error)
    {
        if (error.RetryCount < error.MaxRetries)
        {
            return new ErrorRecoveryResult
            {
                Strategy = ErrorRecoveryStrategy.ExponentialBackoff,
                IsRecovered = true,
                Message = "服务器错误，使用指数退避重试",
                Parameters = new Dictionary<string, object>
                {
                    ["DelayMs"] = Math.Min(5000 * (int)Math.Pow(2, error.RetryCount), 60000)
                }
            };
        }
        
        return new ErrorRecoveryResult
        {
            Strategy = ErrorRecoveryStrategy.Abort,
            IsRecovered = false,
            Message = "服务器错误重试次数已达上限"
        };
    }
    
    /// <summary>
    /// 处理超时错误
    /// </summary>
    private ErrorRecoveryResult HandleTimeoutError(DownloadError error)
    {
        if (error.RetryCount < error.MaxRetries)
        {
            // 超时后切换到单线程模式
            return new ErrorRecoveryResult
            {
                Strategy = ErrorRecoveryStrategy.Fallback,
                IsRecovered = true,
                Message = "超时错误，切换到单线程模式",
                Parameters = new Dictionary<string, object>
                {
                    ["ThreadCount"] = 1,
                    ["TimeoutMs"] = (int)(error.Context.ContainsKey("TimeoutMs") ? 
                        (int)error.Context["TimeoutMs"] * 1.5 : 45000)
                }
            };
        }
        
        return new ErrorRecoveryResult
        {
            Strategy = ErrorRecoveryStrategy.Abort,
            IsRecovered = false,
            Message = "超时错误重试次数已达上限"
        };
    }
    
    /// <summary>
    /// 处理文件IO错误
    /// </summary>
    private ErrorRecoveryResult HandleFileIOError(DownloadError error)
    {
        // 磁盘空间不足或权限错误不需要重试
        if (error.ErrorCode.Contains("DISK_SPACE") || error.ErrorCode.Contains("ACCESS_DENIED"))
        {
            return new ErrorRecoveryResult
            {
                Strategy = ErrorRecoveryStrategy.Abort,
                IsRecovered = false,
                Message = "文件系统错误，无法自动恢复"
            };
        }
        
        // 文件被占用可以延迟重试
        if (error.ErrorCode.Contains("FILE_IN_USE") && error.RetryCount < error.MaxRetries)
        {
            return new ErrorRecoveryResult
            {
                Strategy = ErrorRecoveryStrategy.DelayedRetry,
                IsRecovered = true,
                Message = "文件被占用，延迟重试",
                Parameters = new Dictionary<string, object>
                {
                    ["DelayMs"] = 3000 // 3秒
                }
            };
        }
        
        return new ErrorRecoveryResult
        {
            Strategy = ErrorRecoveryStrategy.Abort,
            IsRecovered = false,
            Message = "文件IO错误无法恢复"
        };
    }
    
    /// <summary>
    /// 处理一般性错误
    /// </summary>
    private ErrorRecoveryResult HandleGenericError(DownloadError error)
    {
        if (error.RetryCount < error.MaxRetries)
        {
            return new ErrorRecoveryResult
            {
                Strategy = ErrorRecoveryStrategy.DelayedRetry,
                IsRecovered = true,
                Message = "一般错误，延迟重试",
                Parameters = new Dictionary<string, object>
                {
                    ["DelayMs"] = error.RetryDelayMs
                }
            };
        }
        
        return new ErrorRecoveryResult
        {
            Strategy = ErrorRecoveryStrategy.Abort,
            IsRecovered = false,
            Message = "错误重试次数已达上限"
        };
    }
    
    /// <summary>
    /// 获取错误统计信息
    /// </summary>
    /// <returns>错误统计</returns>
    public Dictionary<DownloadErrorType, int> GetErrorStatistics()
    {
        lock (_lockObject)
        {
            var stats = new Dictionary<DownloadErrorType, int>();
            
            foreach (DownloadErrorType errorType in Enum.GetValues<DownloadErrorType>())
            {
                stats[errorType] = 0;
            }
            
            foreach (var error in _errorHistory)
            {
                stats[error.Type]++;
            }
            
            return stats;
        }
    }
    
    /// <summary>
    /// 清理错误历史记录
    /// </summary>
    /// <param name="olderThan">清理指定时间之前的记录</param>
    public void ClearErrorHistory(DateTime? olderThan = null)
    {
        lock (_lockObject)
        {
            if (olderThan.HasValue)
            {
                _errorHistory.RemoveAll(e => e.Timestamp < olderThan.Value);
            }
            else
            {
                _errorHistory.Clear();
            }
        }
        
        LogWrapper.Info(LogModule, $"已清理错误历史记录");
    }
}
