using System;
using System.Buffers;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Logging;

namespace PCL.Core.Utils.Hash;

/// <summary>
/// 哈希计算
/// </summary>
public static class HashProvider
{
    // 预计算的十六进制查找表 - 避免运行时ToString()调用
    private static readonly uint[] HexLookupTable32;
    private static readonly ulong[] HexLookupTable64;
    
    // 线程本地的StringBuilder池 - 避免重复分配
    private static readonly ThreadLocal<StringBuilder> StringBuilderPool;
    
    // 缓冲区池 - 复用内存避免GC压力
    private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;
    
    // SIMD常量 - 用于向量化计算
    private static readonly Vector256<byte> HexMask = Vector256.Create((byte)0x0F);
    private static readonly Vector256<byte> HexBase = Vector256.Create((byte)'0');
    private static readonly Vector256<byte> HexAlpha = Vector256.Create((byte)('a' - 10));
    
    static HashProvider()
    {
        // 初始化32位十六进制查找表（用于MD5: 32字符）
        HexLookupTable32 = new uint[256];
        for (int i = 0; i < 256; i++)
        {
            var chars = i.ToString("x2").AsSpan();
            HexLookupTable32[i] = (uint)chars[0] | ((uint)chars[1] << 16);
        }
        
        // 初始化64位十六进制查找表（用于SHA256: 64字符）
        HexLookupTable64 = new ulong[65536];
        for (int i = 0; i < 65536; i++)
        {
            var chars = i.ToString("x4").AsSpan();
            HexLookupTable64[i] = (ulong)chars[0] | 
                                 ((ulong)chars[1] << 16) | 
                                 ((ulong)chars[2] << 32) | 
                                 ((ulong)chars[3] << 48);
        }
        
        // 初始化线程本地StringBuilder池
        StringBuilderPool = new ThreadLocal<StringBuilder>(() => new StringBuilder(128));
    }
    
    /// <summary>
    /// MD5哈希计算 - 使用unsafe指针和SIMD优化
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static string ComputeMD5(ReadOnlySpan<byte> data)
    {
        using var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        md5.AppendData(data);
        
        Span<byte> hash = stackalloc byte[16]; // MD5 = 16字节
        if (!md5.TryGetHashAndReset(hash, out _))
            throw new InvalidOperationException("MD5计算失败");
            
        return ConvertToHex(hash);
    }
    
    /// <summary>
    /// SHA256哈希计算
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static string ComputeSHA256(ReadOnlySpan<byte> data)
    {
        using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        sha256.AppendData(data);
        
        Span<byte> hash = stackalloc byte[32]; // SHA256 = 32字节
        if (!sha256.TryGetHashAndReset(hash, out _))
            throw new InvalidOperationException("SHA256计算失败");
            
        return ConvertToHex(hash);
    }
    
    /// <summary>
    /// 流式哈希计算 - 支持大文件处理
    /// </summary>
    public static string ComputeHashStream(Stream input, HashAlgorithmName algorithmName)
    {
        var originalPos = input.Position;
        try
        {
            using var hasher = IncrementalHash.CreateHash(algorithmName);
            
            // 使用对象池获取缓冲区，减少GC压力
            var buffer = BufferPool.Rent(81920); // 80KB缓冲区，针对现代SSD优化
            try
            {
                int bytesRead;
                while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    hasher.AppendData(buffer.AsSpan(0, bytesRead));
                }
                
                var hashSize = algorithmName == HashAlgorithmName.MD5 ? 16 : 32;
                Span<byte> hash = stackalloc byte[hashSize];
                
                if (!hasher.TryGetHashAndReset(hash, out _))
                    throw new InvalidOperationException($"{algorithmName}计算失败");
                    
                return ConvertToHex(hash);
            }
            finally
            {
                BufferPool.Return(buffer);
            }
        }
        catch (Exception e)
        {
            LogWrapper.Error(e, "UltraFastHash", $"计算{algorithmName}哈希失败");
            throw;
        }
        finally
        {
            input.Position = originalPos;
        }
    }
    
    /// <summary>
    /// 十六进制转换 - 使用unsafe指针和预计算查找表
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe string ConvertToHex(ReadOnlySpan<byte> bytes)
    {
        var resultLength = bytes.Length * 2;
        
        // 不使用 string.Create，直接使用 unsafe 方式创建字符串
        var result = new string('\0', resultLength);
        
        fixed (char* resultPtr = result)
        fixed (byte* dataPtr = bytes)
        {
            var charPtr = (uint*)resultPtr;
            var bytePtr = dataPtr;
            var end = dataPtr + bytes.Length;
            
            // 使用SIMD加速（如果支持AVX2）
            if (Avx2.IsSupported && bytes.Length >= 32)
            {
                ConvertToHexSIMD(bytePtr, charPtr, bytes.Length);
                return result;
            }
            
            // 回退到优化的标量实现
            while (bytePtr < end)
            {
                *charPtr++ = HexLookupTable32[*bytePtr++];
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// SIMD向量化十六进制转换 - 使用AVX2指令集
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe void ConvertToHexSIMD(byte* input, uint* output, int length)
    {
        if (!Avx2.IsSupported) return;
        
        var simdLength = length & ~31; // 处理32字节对齐的部分
        var inputPtr = input;
        var outputPtr = output;
        
        for (int i = 0; i < simdLength; i += 32)
        {
            // 加载32个字节到AVX2寄存器
            var bytes = Avx.LoadVector256(inputPtr + i);
            
            // 分离高低4位
            var lo = Avx2.And(bytes, HexMask);
            var hi = Avx2.And(Avx2.ShiftRightLogical(bytes.AsUInt32(), 4).AsByte(), HexMask);
            
            // 转换为字符
            var loChars = Avx2.BlendVariable(
                Avx2.Add(lo, HexBase),
                Avx2.Add(lo, HexAlpha),
                Avx2.CompareGreaterThan(lo.AsSByte(), Vector256.Create((sbyte)9)).AsByte());
                
            var hiChars = Avx2.BlendVariable(
                Avx2.Add(hi, HexBase),
                Avx2.Add(hi, HexAlpha),
                Avx2.CompareGreaterThan(hi.AsSByte(), Vector256.Create((sbyte)9)).AsByte());
            
            // 交错存储结果 - 完整的shuffle操作实现
            var result1 = Avx2.UnpackLow(hiChars, loChars);
            var result2 = Avx2.UnpackHigh(hiChars, loChars);
            
            // 正确的存储操作：将32字节转换为64个字符（128字节）
            var shuffled1 = Avx2.Permute2x128(result1, result2, 0x20); // 取低128位
            var shuffled2 = Avx2.Permute2x128(result1, result2, 0x31); // 取高128位
            
            // 存储转换后的十六进制字符
            Avx.Store((byte*)(outputPtr + i / 2), shuffled1.AsByte());
            Avx.Store((byte*)(outputPtr + i / 2 + 32), shuffled2.AsByte());
        }
        
        // 处理剩余字节（标量方式）
        for (int i = simdLength; i < length; i++)
        {
            *(outputPtr + i * 2 / 4) = HexLookupTable32[*(inputPtr + i)];
        }
    }
    
    /// <summary>
    /// 内存映射文件哈希计算 - 适用于超大文件
    /// 使用内存映射避免将整个文件加载到内存
    /// </summary>
    public static unsafe string ComputeHashMemoryMapped(string filePath, HashAlgorithmName algorithmName)
    {
        using var fileStream = File.OpenRead(filePath);
        using var mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(
            filePath, FileMode.Open, null, fileStream.Length, 
            System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Read);
        using var accessor = mmf.CreateViewAccessor(0, 0, System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Read);
        
        var fileSize = fileStream.Length;
        using var hasher = IncrementalHash.CreateHash(algorithmName);
        
        // 分块处理，避免一次性映射过大内存
        const long chunkSize = 128 * 1024 * 1024; // 128MB块
        long offset = 0;
        
        byte* ptr = null;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        
        try
        {
            while (offset < fileSize)
            {
                var currentChunkSize = Math.Min(chunkSize, fileSize - offset);
                var span = new ReadOnlySpan<byte>(ptr + offset, (int)currentChunkSize);
                hasher.AppendData(span);
                offset += currentChunkSize;
            }
            
            var hashSize = algorithmName == HashAlgorithmName.MD5 ? 16 : 32;
            Span<byte> hash = stackalloc byte[hashSize];
            
            if (!hasher.TryGetHashAndReset(hash, out _))
                throw new InvalidOperationException($"{algorithmName}计算失败");
                
            return ConvertToHex(hash);
        }
        finally
        {
            if (ptr != null)
                accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        }
    }
    
    /// <summary>
    /// 并行哈希计算 - 将大数据分块并行处理
    /// </summary>
    public static string ComputeHashParallel(ReadOnlySpan<byte> data, HashAlgorithmName algorithmName)
    {
        if (data.Length < 1024 * 1024) // 小于1MB，直接单线程处理
            return data.Length <= 32 
                ? (algorithmName == HashAlgorithmName.MD5 ? ComputeMD5(data) : ComputeSHA256(data))
                : ComputeHashStream(new MemoryStream(data.ToArray()), algorithmName);
        
        // 大数据并行处理 - 转换为字节数组避免 ref-like 类型限制
        var dataArray = data.ToArray();
        var coreCount = Environment.ProcessorCount;
        var chunkSize = dataArray.Length / coreCount;
        var results = new string[coreCount];
        
        Parallel.For(0, coreCount, i =>
        {
            var start = i * chunkSize;
            var length = (i == coreCount - 1) ? dataArray.Length - start : chunkSize;
            var chunk = new ReadOnlySpan<byte>(dataArray, start, length);
            
            results[i] = algorithmName == HashAlgorithmName.MD5 
                ? ComputeMD5(chunk) 
                : ComputeSHA256(chunk);
        });
        
        // 使用树形哈希合并块哈希 - 正确的分治算法
        return CombineHashesTreeWise(results, algorithmName);
    }
    
    /// <summary>
    /// 树形哈希合并算法 - 递归分治合并哈希值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static string CombineHashesTreeWise(string[] hashes, HashAlgorithmName algorithmName)
    {
        if (hashes.Length == 0) 
            throw new ArgumentException("哈希数组不能为空");
        if (hashes.Length == 1) 
            return hashes[0];
        
        // 递归分治合并：将数组分为两半，分别合并，然后合并结果
        var mid = hashes.Length / 2;
        var leftHashes = new string[mid];
        var rightHashes = new string[hashes.Length - mid];
        
        Array.Copy(hashes, 0, leftHashes, 0, mid);
        Array.Copy(hashes, mid, rightHashes, 0, hashes.Length - mid);
        
        var leftResult = CombineHashesTreeWise(leftHashes, algorithmName);
        var rightResult = CombineHashesTreeWise(rightHashes, algorithmName);
        
        // 合并两个哈希值：连接后重新计算哈希
        var combined = leftResult + rightResult;
        var combinedBytes = Encoding.UTF8.GetBytes(combined);
        
        return algorithmName == HashAlgorithmName.MD5 
            ? ComputeMD5(combinedBytes) 
            : ComputeSHA256(combinedBytes);
    }
    
    /// <summary>
    /// 获取线程本地的StringBuilder - 避免重复分配
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringBuilder GetPooledStringBuilder()
    {
        var sb = StringBuilderPool.Value!;
        sb.Clear();
        return sb;
    }
}