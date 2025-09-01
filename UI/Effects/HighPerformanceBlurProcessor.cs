using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace PCL.Core.UI.Effects;

/// <summary>
/// 精简的高性能模糊处理器，专注于核心算法和性能优化
/// </summary>
internal sealed class HighPerformanceBlurProcessor : IDisposable
{
    private static readonly ArrayPool<uint> UintPool = ArrayPool<uint>.Create();
    private static readonly ConcurrentDictionary<string, CachedResult> Cache = new();
    private readonly object _lockObject = new();
    private bool _disposed;

    private struct CachedResult
    {
        public WriteableBitmap Bitmap;
        public DateTime LastUsed;
    }

    /// <summary>
    /// 预计算的泊松盘采样点，优化采样模式
    /// </summary>
    private static readonly Vector2[] PoissonSamples = GeneratePoissonSamples();

    public void InvalidateCache()
    {
        lock (_lockObject)
        {
            Cache.Clear();
        }
    }

    /// <summary>
    /// 应用高性能模糊效果
    /// </summary>
    public WriteableBitmap? ApplyBlur(BitmapSource? source, double radius, double samplingRate, 
        RenderingBias renderingBias, KernelType kernelType)
    {
        if (source == null || radius <= 0)
            return null;

        var cacheKey = GenerateCacheKey(source, radius, samplingRate, renderingBias, kernelType);
        
        lock (_lockObject)
        {
            if (Cache.TryGetValue(cacheKey, out var cached))
            {
                cached.LastUsed = DateTime.UtcNow;
                Cache[cacheKey] = cached;
                return cached.Bitmap;
            }
        }

        var result = ProcessBlur(source, radius, samplingRate, renderingBias, kernelType);

        lock (_lockObject)
        {
            Cache[cacheKey] = new CachedResult
            {
                Bitmap = result,
                LastUsed = DateTime.UtcNow
            };

            // 清理过期缓存
            if (Cache.Count > 20)
            {
                CleanExpiredCache();
            }
        }

        return result;
    }

    /// <summary>
    /// 核心模糊处理算法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private WriteableBitmap ProcessBlur(BitmapSource source, double radius, double samplingRate,
        RenderingBias renderingBias, KernelType kernelType)
    {
        var width = source.PixelWidth;
        var height = source.PixelHeight;
        var stride = (width * source.Format.BitsPerPixel + 7) / 8;

        var sourceBuffer = UintPool.Rent(width * height);
        var targetBuffer = UintPool.Rent(width * height);

        try
        {
            // 复制源图像数据
            var sourceBytes = new byte[stride * height];
            source.CopyPixels(sourceBytes, stride, 0);
            CopyBytesToUints(sourceBytes, sourceBuffer, width * height);

            // 根据采样率和质量要求选择算法
            if (samplingRate >= 0.8 || renderingBias == RenderingBias.Quality)
            {
                ApplySeparableBlur(sourceBuffer, targetBuffer, width, height, radius, samplingRate);
            }
            else
            {
                ApplySamplingBlur(sourceBuffer, targetBuffer, width, height, radius, samplingRate);
            }

            // 创建结果位图
            var result = new WriteableBitmap(width, height, source.DpiX, source.DpiY, PixelFormats.Bgra32, null);
            result.Lock();

            try
            {
                unsafe
                {
                    var resultPtr = (uint*)result.BackBuffer;
                    fixed (uint* targetPtr = targetBuffer)
                    {
                        Buffer.MemoryCopy(targetPtr, resultPtr, width * height * 4, width * height * 4);
                    }
                }

                result.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            finally
            {
                result.Unlock();
            }

            return result;
        }
        finally
        {
            UintPool.Return(sourceBuffer);
            UintPool.Return(targetBuffer);
        }
    }

    /// <summary>
    /// 分离高斯模糊：先水平后垂直，性能优化版本
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void ApplySeparableBlur(uint[] source, uint[] target, int width, int height, 
        double radius, double samplingRate)
    {
        var intRadius = Math.Max(1, (int)Math.Ceiling(radius * samplingRate));
        var tempBuffer = UintPool.Rent(width * height);
        
        try
        {
            var weights = GenerateGaussianKernel(intRadius);

            // 水平模糊
            Parallel.For(0, height, y =>
            {
                var rowStart = y * width;
                for (var x = 0; x < width; x++)
                {
                    var (a, r, g, b) = SampleHorizontal(source, width, x, rowStart, weights, samplingRate);
                    tempBuffer[rowStart + x] = PackColor(a, r, g, b);
                }
            });

            // 垂直模糊
            Parallel.For(0, width, x =>
            {
                for (var y = 0; y < height; y++)
                {
                    var (a, r, g, b) = SampleVertical(tempBuffer, width, height, x, y, weights, samplingRate);
                    target[y * width + x] = PackColor(a, r, g, b);
                }
            });
        }
        finally
        {
            UintPool.Return(tempBuffer);
        }
    }

    /// <summary>
    /// 智能采样模糊：使用泊松盘采样降低计算量
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void ApplySamplingBlur(uint[] source, uint[] target, int width, int height,
        double radius, double samplingRate)
    {
        var sampleCount = Math.Max(4, (int)(PoissonSamples.Length * samplingRate));

        Parallel.For(0, height, y =>
        {
            for (var x = 0; x < width; x++)
            {
                var (a, r, g, b) = SampleWithPoisson(source, width, height, x, y, radius, sampleCount);
                target[y * width + x] = PackColor(a, r, g, b);
            }
        });
    }

    /// <summary>
    /// 水平方向采样
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (byte a, byte r, byte g, byte b) SampleHorizontal(uint[] source, int width, int x, int rowStart,
        double[] weights, double samplingRate)
    {
        double totalA = 0, totalR = 0, totalG = 0, totalB = 0, totalWeight = 0;
        var kernelRadius = weights.Length / 2;
        var sampleStep = samplingRate >= 0.8 ? 1 : Math.Max(1, (int)(2.0 - samplingRate));

        for (var k = -kernelRadius; k <= kernelRadius; k += sampleStep)
        {
            var sampleX = Math.Max(0, Math.Min(width - 1, x + k));
            var pixel = source[rowStart + sampleX];
            var weight = weights[kernelRadius + k];

            totalA += ((pixel >> 24) & 0xFF) * weight;
            totalR += ((pixel >> 16) & 0xFF) * weight;
            totalG += ((pixel >> 8) & 0xFF) * weight;
            totalB += (pixel & 0xFF) * weight;
            totalWeight += weight;
        }

        if (totalWeight > 0)
        {
            var invWeight = 1.0 / totalWeight;
            return (
                (byte)Math.Min(255, totalA * invWeight),
                (byte)Math.Min(255, totalR * invWeight),
                (byte)Math.Min(255, totalG * invWeight),
                (byte)Math.Min(255, totalB * invWeight)
            );
        }

        var originalPixel = source[rowStart + x];
        return (
            (byte)((originalPixel >> 24) & 0xFF),
            (byte)((originalPixel >> 16) & 0xFF),
            (byte)((originalPixel >> 8) & 0xFF),
            (byte)(originalPixel & 0xFF)
        );
    }

    /// <summary>
    /// 垂直方向采样
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (byte a, byte r, byte g, byte b) SampleVertical(uint[] source, int width, int height, int x, int y,
        double[] weights, double samplingRate)
    {
        double totalA = 0, totalR = 0, totalG = 0, totalB = 0, totalWeight = 0;
        var kernelRadius = weights.Length / 2;
        var sampleStep = samplingRate >= 0.8 ? 1 : Math.Max(1, (int)(2.0 - samplingRate));

        for (var k = -kernelRadius; k <= kernelRadius; k += sampleStep)
        {
            var sampleY = Math.Max(0, Math.Min(height - 1, y + k));
            var pixel = source[sampleY * width + x];
            var weight = weights[k + kernelRadius];

            totalA += ((pixel >> 24) & 0xFF) * weight;
            totalR += ((pixel >> 16) & 0xFF) * weight;
            totalG += ((pixel >> 8) & 0xFF) * weight;
            totalB += (pixel & 0xFF) * weight;
            totalWeight += weight;
        }

        if (totalWeight > 0)
        {
            var invWeight = 1.0 / totalWeight;
            return (
                (byte)Math.Min(255, totalA * invWeight),
                (byte)Math.Min(255, totalR * invWeight),
                (byte)Math.Min(255, totalG * invWeight),
                (byte)Math.Min(255, totalB * invWeight)
            );
        }

        var originalPixel = source[y * width + x];
        return (
            (byte)((originalPixel >> 24) & 0xFF),
            (byte)((originalPixel >> 16) & 0xFF),
            (byte)((originalPixel >> 8) & 0xFF),
            (byte)(originalPixel & 0xFF)
        );
    }

    /// <summary>
    /// 泊松盘采样
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (byte a, byte r, byte g, byte b) SampleWithPoisson(uint[] source, int width, int height, 
        int centerX, int centerY, double radius, int sampleCount)
    {
        double totalA = 0, totalR = 0, totalG = 0, totalB = 0;
        var validSamples = 0;

        for (var i = 0; i < sampleCount && i < PoissonSamples.Length; i++)
        {
            var offset = PoissonSamples[i] * (float)radius;
            var sampleX = centerX + (int)Math.Round(offset.X);
            var sampleY = centerY + (int)Math.Round(offset.Y);

            if (sampleX >= 0 && sampleX < width && sampleY >= 0 && sampleY < height)
            {
                var pixel = source[sampleY * width + sampleX];
                var distance = offset.Length();
                var weight = Math.Max(0.1, 1.0 - distance / radius);

                totalA += ((pixel >> 24) & 0xFF) * weight;
                totalR += ((pixel >> 16) & 0xFF) * weight;
                totalG += ((pixel >> 8) & 0xFF) * weight;
                totalB += (pixel & 0xFF) * weight;
                validSamples++;
            }
        }

        if (validSamples > 0)
        {
            var invSamples = 1.0 / validSamples;
            return (
                (byte)Math.Min(255, totalA * invSamples),
                (byte)Math.Min(255, totalR * invSamples),
                (byte)Math.Min(255, totalG * invSamples),
                (byte)Math.Min(255, totalB * invSamples)
            );
        }

        var originalPixel = source[centerY * width + centerX];
        return (
            (byte)((originalPixel >> 24) & 0xFF),
            (byte)((originalPixel >> 16) & 0xFF),
            (byte)((originalPixel >> 8) & 0xFF),
            (byte)(originalPixel & 0xFF)
        );
    }

    /// <summary>
    /// 生成高斯卷积核
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static double[] GenerateGaussianKernel(int radius)
    {
        var size = radius * 2 + 1;
        var kernel = new double[size];
        var sigma = radius / 3.0;
        var twoSigmaSquared = 2.0 * sigma * sigma;
        double totalWeight = 0;

        for (var i = 0; i < size; i++)
        {
            var x = i - radius;
            var weight = Math.Exp(-(x * x) / twoSigmaSquared);
            kernel[i] = weight;
            totalWeight += weight;
        }

        // 归一化
        if (totalWeight > 0)
        {
            var invTotal = 1.0 / totalWeight;
            for (var i = 0; i < size; i++)
            {
                kernel[i] *= invTotal;
            }
        }

        return kernel;
    }

    /// <summary>
    /// 生成泊松盘采样点
    /// </summary>
    private static Vector2[] GeneratePoissonSamples()
    {
        const int sampleCount = 16; // 平衡质量和性能
        const float minDistance = 0.8f;
        var samples = new Vector2[sampleCount];
        var random = new Random(42); // 固定种子确保一致性
        var validSamples = 0;
        var attempts = 0;

        while (validSamples < sampleCount && attempts < 500)
        {
            var candidate = new Vector2(
                (float)(random.NextDouble() * 2.0 - 1.0),
                (float)(random.NextDouble() * 2.0 - 1.0)
            );

            if (candidate.LengthSquared() > 1.0f)
            {
                attempts++;
                continue;
            }

            var valid = true;
            for (var i = 0; i < validSamples; i++)
            {
                if (Vector2.DistanceSquared(candidate, samples[i]) < minDistance * minDistance)
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                samples[validSamples++] = candidate;
            }
            attempts++;
        }

        // 填充剩余样本
        while (validSamples < sampleCount)
        {
            var angle = 2.0 * Math.PI * validSamples / sampleCount;
            var radius = 0.8f;
            samples[validSamples++] = new Vector2(
                (float)(Math.Cos(angle) * radius),
                (float)(Math.Sin(angle) * radius)
            );
        }

        return samples;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint PackColor(byte a, byte r, byte g, byte b) =>
        ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;

    private static void CopyBytesToUints(byte[] source, uint[] target, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var baseIndex = i * 4;
            if (baseIndex + 3 < source.Length)
            {
                target[i] = ((uint)source[baseIndex + 3] << 24) |
                           ((uint)source[baseIndex + 2] << 16) |
                           ((uint)source[baseIndex + 1] << 8) |
                           source[baseIndex];
            }
        }
    }

    private static string GenerateCacheKey(BitmapSource source, double radius, double samplingRate,
        RenderingBias renderingBias, KernelType kernelType)
    {
        return $"{source.GetHashCode()}_{radius:F1}_{samplingRate:F2}_{renderingBias}_{kernelType}";
    }

    private void CleanExpiredCache()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-2);
        var keysToRemove = new List<string>();
        
        foreach (var kvp in Cache)
        {
            if (kvp.Value.LastUsed < cutoff)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            Cache.TryRemove(key, out _);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Cache.Clear();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~HighPerformanceBlurProcessor()
    {
        Dispose();
    }
}
