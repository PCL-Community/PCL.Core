using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCL.Core.Minecraft.Accounts.Models;
public abstract class ProfileBase
{
    /// <summary>
    /// 当前账户的访问令牌
    /// </summary>
    public required string AccessToken;
    /// <summary>
    /// 当前账户的刷新令牌
    /// </summary>
    public string? RefreshToken;
    
    /// <summary>
    /// 用户名称
    /// </summary>
    public string? UserName;
    /// <summary>
    /// 档案 UUID
    /// </summary>
    public string? Uuid;
    /// <summary>
    /// 过期时间
    /// </summary>
    public string? ExpiredIn;
    /// <summary>
    /// 此档案是否已过期
    /// </summary>
    public bool IsExpired;
    
    

    public abstract bool ValidateAsync();
}


public enum ProfileSource
{
    /// <summary>
    /// 正版账号
    /// </summary>
    Microsoft,
    /// <summary>
    /// 第三方账户（LittleSkin 皮肤站等）
    /// </summary>
    ThirdParty,
    /// <summary>
    /// 离线档案
    /// </summary>
    Offline,
    /// <summary>
    /// 其他类型档案（不可用于启动游戏，也不会展示在档案列表内）
    /// </summary>
    Other
}