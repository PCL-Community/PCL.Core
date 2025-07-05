using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using PCL.Core.Helper;
using PCL.Core.LifecycleManagement;

namespace PCL.Core.Service;

/// <summary>
/// 用于终止 Pipe RPC 执行过程并返回错误信息的异常<br/>
/// 当抛出该异常时 RPC 服务端将会返回内容为 <c>Reason</c> 的 <c>ERR</c> 响应
/// </summary>
public class RpcException(string reason) : Exception
{
    public string Reason => reason;
}

public enum RpcResponseStatus
{
    SUCCESS,
    FAILURE,
    ERR
}

public enum RpcResponseType
{
    EMPTY,
    TEXT,
    JSON,
    BASE64
}

/// <summary>
/// Pipe RPC 响应
/// </summary>
public class RpcResponse
{
    public RpcResponseStatus Status { get; }

    public RpcResponseType Type { get; }

    public string? Name { get; }

    public string? Content { get; }

    public RpcResponse(RpcResponseStatus status, RpcResponseType type = RpcResponseType.EMPTY, string? content = null, string? name = null)
    {
        if (content != null && type == RpcResponseType.EMPTY)
            throw new ArgumentException("Empty response with non-null content");
        Status = status;
        Type = type;
        Content = content;
        Name = name;
    }

    // STATUS type [name]
    // [content]
    public void Response(StreamWriter writer)
    {
        var nameArea = Name == null ? "" : $" {Name}";
        writer.WriteLine($"{Status} {Type.ToString().ToLowerInvariant()}{nameArea}");
        if (Content != null)
            writer.WriteLine(Content);
    }

    public static readonly RpcResponse EmptySuccess = new RpcResponse(RpcResponseStatus.SUCCESS);

    public static readonly RpcResponse EmptyFailure = new RpcResponse(RpcResponseStatus.FAILURE);

    public static RpcResponse Err(string content, string? name = null)
    {
        return new RpcResponse(RpcResponseStatus.ERR, RpcResponseType.TEXT, content, name);
    }

    public static RpcResponse Success(RpcResponseType type, string content, string? name = null)
    {
        return new RpcResponse(RpcResponseStatus.SUCCESS, type, content, name);
    }

    public static RpcResponse Failure(RpcResponseType type, string content, string? name = null)
    {
        return new RpcResponse(RpcResponseStatus.FAILURE, type, content, name);
    }
}

public class RpcPropertyOperationFailedException : Exception;

/// <summary>
/// RPC 属性<br/>
/// 大多数时候只需要使用构造方法，其他结构保留供内部使用
/// </summary>
public class RpcProperty
{
    public delegate void GetValueDelegate(out string? outValue);
    public event GetValueDelegate GetValue;

    public delegate void SetValueDelegate(string? value, ref bool success);
    public event SetValueDelegate? SetValue;

    public readonly string Name;
    public readonly bool Settable = true;

    public string? Value
    {
        get
        {
            GetValue.Invoke(out var value);
            return value;
        }
        set
        {
            var success = true;
            SetValue?.Invoke(value, ref success);
            if (!success)
                throw new RpcPropertyOperationFailedException();
        }
    }

    /// <param name="name">属性名称</param>
    /// <param name="onGetValue">默认的 <c>GetValue</c> 回调</param>
    /// <param name="onSetValue">默认的 <c>SetValue</c> 回调</param>
    /// <param name="settable">指定该属性是否可更改，若该值为 <c>false</c> 的同时 <paramref name="onSetValue"/> 为 <c>null</c>，则该属性成为只读属性</param>
    public RpcProperty(string name, Func<string?> onGetValue, Action<string?>? onSetValue = null, bool settable = false)
    {
        Name = name;
        GetValue += (out string? outValue) =>
        {
            outValue = onGetValue();
        };
        if (onSetValue != null)
        {
            SetValue += (string? value, ref bool _) =>
            {
                onSetValue(value);
            };
        }
        else if (!settable)
        {
            Settable = false;
            SetValue += (string? _, ref bool success) =>
            {
                success = false;
            };
        }
    }
}

/// <summary>
/// RPC 函数<br/>
/// 接收参数并返回响应内容
/// </summary>
/// <param name="argument">参数</param>
/// <returns>响应内容</returns>
public delegate RpcResponse RpcFunction(string? argument, string? content, bool indent);

/// <summary>
/// RPC 服务项
/// </summary>
[LifecycleService(LifecycleState.Loaded)]
public sealed class RpcService : ILifecycleService
{
    public string Identifier => "rpc";
    public string Name => "远程执行服务";
    public bool SupportAsyncStart => true;
    
    private readonly LifecycleContext Context;
    private RpcService() { Context = Lifecycle.GetContext(this); }

    private NamedPipeServerStream? _pipe;
    
    public void Start()
    {
        _pipe = NativeInterop.StartPipeServer("Echo", EchoPipeName, EchoPipeCallback);
    }

    public void Stop()
    {
        _pipe?.Dispose();
    }
    
    private static readonly string EchoPipeName = $"PCLCE_RPC@{NativeInterop.CurrentProcess.Id}";
    private static readonly string[] RequestTypeArray = ["GET", "SET", "REQ"];
    private static readonly HashSet<string> RequestType = [..RequestTypeArray];

    #region Property
    
    private static readonly Dictionary<string, RpcProperty> PropertyMap = new();

    /// <summary>
    /// 添加一个新的 RPC 属性，若有多个使用 foreach 即可
    /// </summary>
    /// <param name="prop">要添加的属性</param>
    /// <returns>是否成功添加（若已存在相同名称的属性则无法添加）</returns>
    public static bool AddProperty(RpcProperty prop)
    {
        var key = prop.Name;
        if (PropertyMap.ContainsKey(key))
            return false;
        PropertyMap[key] = prop;
        return true;
    }

    /// <summary>
    /// 通过指定的名称删除已存在的 RPC 属性
    /// </summary>
    /// <param name="name">属性名称</param>
    /// <returns>是否成功删除（若不存在该名称则无法删除）</returns>
    public static bool RemoveProperty(string name)
    {
        return PropertyMap.Remove(name);
    }

    /// <summary>
    /// 删除已存在的 RPC 属性，实质上仍然是通过属性的名称删除，但会检查是否是同一个对象
    /// </summary>
    /// <param name="prop">要删除的属性</param>
    /// <returns></returns>
    public static bool RemoveProperty(RpcProperty prop)
    {
        var key = prop.Name;
        var result = PropertyMap.TryGetValue(key, out var value);
        if (!result || value != prop) return false;
        PropertyMap.Remove(key);
        return true;
    }

    #endregion

    #region Function

    private static readonly Dictionary<string, RpcFunction> FunctionMap = new() {
        ["ping"] = (_, _, _) => RpcResponse.EmptySuccess
    };

    /// <summary>
    /// 添加一个新的 RPC 函数，若有多个使用 foreach 即可
    /// </summary>
    /// <param name="name">函数名称</param>
    /// <param name="func">函数过程</param>
    /// <returns>是否成功添加（若已存在相同名称的函数则无法添加）</returns>
    public static bool AddFunction(string name, RpcFunction func)
    {
        if (FunctionMap.ContainsKey(name))
            return false;
        FunctionMap[name] = func;
        return true;
    }

    /// <summary>
    /// 通过指定的名称删除已存在的 RPC 函数
    /// </summary>
    /// <param name="name">函数名称</param>
    /// <returns>是否成功删除（若不存在该名称则无法删除）</returns>
    public static bool RemoveFunction(string name)
    {
        return FunctionMap.Remove(name);
    }
    
    #endregion

    private bool EchoPipeCallback(StreamReader reader, StreamWriter writer, Process? client)
    {
        try
        {
            // GET/SET/REQ [target]
            // [content]
            var header = reader.ReadLine(); // 读入请求头
            Context.Info($"客户端请求: {header}");

            var args = header?.Split([' '], 2) ?? []; // 分离请求类型和参数
            if (args.Length < 2 || args[1].Length == 0) throw new RpcException("请求参数过少");
            var type = args[0].ToUpperInvariant();
            if (!RequestType.Contains(type)) throw new RpcException($"请求类型必须为 {string.Join("/", RequestTypeArray)} 其中之一");
            var target = args[1];

            // 读入请求内容（可能没有）
            var buffer = new StringBuilder();
            var tmp = reader.Read();
            while (tmp != NativeInterop.PipeEndingChar)
            {
                buffer.Append((char)tmp);
                tmp = reader.Read();
            }
            var content = buffer.Length == 0 ? null : buffer.ToString();

            switch (type)
            {
                case "GET": case "SET": {
                    target = target.ToLowerInvariant();
                    var result = PropertyMap.TryGetValue(target, out var prop);
                    if (!result) throw new RpcException($"不存在属性 {target}");
                    RpcResponse response;
                    if (type == "GET")
                    {
                        try
                        {
                            var value = prop!.Value;
                            response = new RpcResponse(RpcResponseStatus.SUCCESS, RpcResponseType.TEXT, value, target);
                            Context.Trace($"返回值: {value}");
                        }
                        catch (RpcPropertyOperationFailedException)
                        {
                            response = RpcResponse.EmptyFailure;
                            Context.Debug("设置失败: 只写属性或请求被拒绝");
                        }
                    }
                    else if (prop!.Settable)
                    {
                        try
                        {
                            prop.Value = content;
                            response = RpcResponse.EmptySuccess;
                            Context.Trace($"设置成功: {content}");
                        }
                        catch (RpcPropertyOperationFailedException)
                        {
                            response = RpcResponse.EmptyFailure;
                            Context.Debug("设置失败: 请求被拒绝");
                        }
                    }
                    else
                    {
                        response = RpcResponse.EmptyFailure;
                        Context.Debug("设置失败: 只读属性");
                    }
                    response.Response(writer);
                    break;
                }

                case "REQ": {
                    var targetArgs = target.Split([' '], 2); // 分离函数名和参数
                    var name = targetArgs[0].ToLowerInvariant();
                    var indent = false; // 检测缩进指示
                    if (name.EndsWith("$"))
                    {
                        indent = true;
                        name = name[..^1];
                    }
                    var result = FunctionMap.TryGetValue(name, out var func);
                    if (!result) throw new RpcException($"不存在函数 {name}");
                    string? argument = null;
                    if (targetArgs.Length > 1)
                        argument = targetArgs[1];
                    Context.Trace($"正在调用函数 {name} {argument}");
                    var response = func!(argument, content, indent);
                    response.Response(writer);
                    Context.Trace($"函数已退出，返回状态 {response.Status}");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            if (ex is RpcException rpcEx)
            {
                var reason = rpcEx.Reason;
                RpcResponse.Err(reason).Response(writer);
                Context.Info($"出错: {reason}");
            }
            else
            {
                RpcResponse.Err(ex.ToString(), "stacktrace").Response(writer);
                Context.Error("处理请求时发生异常", ex);
            }
        }
        return true;
    }
}

public static class Rpc
{
    [Obsolete("请使用 RpcService.AddProperty")]
    public static void AddProperty(RpcProperty prop) => RpcService.AddProperty(prop);
    
    [Obsolete("请使用 RpcService.RemoveProperty")]
    public static bool RemoveProperty(string name) => RpcService.RemoveProperty(name);
    
    [Obsolete("请使用 RpcService.RemoveProperty")]
    public static bool RemoveProperty(RpcProperty prop) => RpcService.RemoveProperty(prop);
    
    [Obsolete("请使用 RpcService.AddFunction")]
    public static bool AddFunction(string name, RpcFunction func) => RpcService.AddFunction(name, func);
    
    [Obsolete("请使用 RpcService.RemoveFunction")]
    public static bool RemoveFunction(string name) => RpcService.RemoveFunction(name);
    
    [Obsolete]
    public static void Start() { }
}
