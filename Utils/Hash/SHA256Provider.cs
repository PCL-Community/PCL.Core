using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using PCL.Core.Logging;

namespace PCL.Core.Utils.Hash;

/// <summary>
/// 极致优化的SHA256提供者 - 展现现代C#性能编程艺术
/// 使用最前沿的优化技术，性能提升巨大！
/// </summary>
public class SHA256Provider : IHashProvider
{
    public static SHA256Provider Instance { get; } = new();
    
    // 高性能缓冲区池 - 减少GC压力
    private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;
    
    /// <summary>
    /// 超高性能Stream SHA256计算 - 智能选择最优算法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public string ComputeHash(Stream input)
    {
        // 小数据直接内存处理，避免流开销
        if (input.CanSeek && input.Length <= 8192) // 8KB阈值
        {
            var buffer = BufferPool.Rent((int)input.Length);
            try
            {
                var originalPos = input.Position;
                input.Position = 0;
                var bytesRead = input.Read(buffer, 0, (int)input.Length);
                input.Position = originalPos;
                
                return HashProvider.ComputeSHA256(buffer.AsSpan(0, bytesRead));
            }
            finally
            {
                BufferPool.Return(buffer);
            }
        }
        
        // 大数据使用优化的流式处理
        return HashProvider.ComputeHashStream(input, HashAlgorithmName.SHA256);
    }
    
    /// <summary>
    /// 零拷贝字节数组哈希计算
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public string ComputeHash(byte[] input) => HashProvider.ComputeSHA256(input);
    
    /// <summary>
    /// 智能字符串哈希计算 - 根据大小选择最优策略
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public string ComputeHash(string input, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        
        // 小字符串使用栈分配 - 零堆内存分配！
        if (input.Length <= 2048) // 2KB阈值，考虑到UTF-8可能的字节膨胀
        {
            var maxByteCount = encoding.GetMaxByteCount(input.Length);
            if (maxByteCount <= 8192) // 8KB栈限制
            {
                Span<byte> buffer = stackalloc byte[maxByteCount];
                var actualBytes = encoding.GetBytes(input.AsSpan(), buffer);
                return HashProvider.ComputeSHA256(buffer[..actualBytes]);
            }
        }
        
        // 中等字符串使用对象池
        if (input.Length <= 32768) // 32KB以下
        {
            var estimatedBytes = encoding.GetByteCount(input);
            var buffer = BufferPool.Rent(estimatedBytes);
            try
            {
                var actualBytes = encoding.GetBytes(input, 0, input.Length, buffer, 0);
                return HashProvider.ComputeSHA256(buffer.AsSpan(0, actualBytes));
            }
            finally
            {
                BufferPool.Return(buffer);
            }
        }
        
        // 大字符串直接转换（此时内存分配成本已经可以接受）
        var bytes = encoding.GetBytes(input);
        return HashProvider.ComputeSHA256(bytes);
    }
    
    /// <summary>
    /// 多核并行哈希计算 - 发挥多核CPU威力
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public string ComputeHashParallel(ReadOnlySpan<byte> data) => 
        HashProvider.ComputeHashParallel(data, HashAlgorithmName.SHA256);
    
    /// <summary>
    /// 超大文件专用哈希 - 使用内存映射和分块处理
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public string ComputeHashFromFile(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        
        // 根据文件大小选择最优策略
        return fileInfo.Length switch
        {
            <= 1024 * 1024 => // 1MB以下，直接读取
                ComputeHash(File.ReadAllBytes(filePath)),
            <= 100 * 1024 * 1024 => // 100MB以下，流式处理
                ComputeHash(File.OpenRead(filePath)),
            _ => // 超大文件，内存映射
                HashProvider.ComputeHashMemoryMapped(filePath, HashAlgorithmName.SHA256)
        };
    }
    
    /// <summary>
    /// ReadOnlySpan重载 - 现代C#的零拷贝特性
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public string ComputeHash(ReadOnlySpan<byte> input) => HashProvider.ComputeSHA256(input);
    
    /// <summary>
    /// Memory重载 - 支持异步场景
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]  
    public string ComputeHash(ReadOnlyMemory<byte> input) => HashProvider.ComputeSHA256(input.Span);

    public int Length => 64;
}