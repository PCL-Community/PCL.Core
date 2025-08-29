using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using PCL.Core.UI.Effects;

namespace PCL.Core.UI.Effects;

/// <summary>
/// 性能测试和基准测试工具，验证BlurEffect优化效果
/// 对比原生BlurEffect与优化版本的性能差异
/// </summary>
public static class BlurPerformanceTest
{
    /// <summary>
    /// 执行完整的性能基准测试
    /// </summary>
    public static PerformanceTestResult RunComprehensiveTest(int testIterations = 10)
    {
        var testImage = CreateTestImage(1920, 1080);
        var result = new PerformanceTestResult();
        
        Console.WriteLine("开始BlurEffect性能基准测试...\n");

        // 测试原生BlurEffect
        Console.WriteLine("测试原生BlurEffect性能...");
        result.OriginalBlurTime = MeasureBlurPerformance(testImage, CreateOriginalBlur(), testIterations);
        Console.WriteLine($"原生BlurEffect平均耗时: {result.OriginalBlurTime:F2}ms\n");

        // 测试不同采样率的优化效果
        var samplingRates = new[] { 1.0, 0.7, 0.5, 0.3, 0.1 };
        
        foreach (var rate in samplingRates)
        {
            Console.WriteLine($"测试采样率 {rate:P0} 的性能...");
            var optimizedBlur = CreateOptimizedBlur(rate);
            var time = MeasureOptimizedBlurPerformance(testImage, optimizedBlur, testIterations);
            var improvement = ((result.OriginalBlurTime - time) / result.OriginalBlurTime) * 100;
            
            result.OptimizedResults.Add(new OptimizedTestResult
            {
                SamplingRate = rate,
                AverageTime = time,
                PerformanceImprovement = improvement
            });
            
            Console.WriteLine($"优化版本平均耗时: {time:F2}ms");
            Console.WriteLine($"性能提升: {improvement:F1}%\n");
        }

        // 显示总结
        DisplayTestSummary(result);
        
        return result;
    }

    /// <summary>
    /// 快速性能对比测试
    /// </summary>
    public static void QuickPerformanceComparison()
    {
        Console.WriteLine("快速性能对比测试 (30%采样率)\n");
        
        var testImage = CreateTestImage(1200, 800);
        var iterations = 5;

        var originalTime = MeasureBlurPerformance(testImage, CreateOriginalBlur(), iterations);
        var optimizedTime = MeasureOptimizedBlurPerformance(testImage, CreateOptimizedBlur(0.3), iterations);
        
        var improvement = ((originalTime - optimizedTime) / originalTime) * 100;

        Console.WriteLine($"原生BlurEffect: {originalTime:F2}ms");
        Console.WriteLine($"优化版本(30%采样): {optimizedTime:F2}ms");
        Console.WriteLine($"性能提升: {improvement:F1}%");
    }

    /// <summary>
    /// 内存使用测试
    /// </summary>
    public static void MemoryUsageTest()
    {
        Console.WriteLine("内存使用对比测试...\n");
        
        var testImage = CreateTestImage(1920, 1080);
        
        // 测试原生BlurEffect内存使用
        var beforeMemory = GC.GetTotalMemory(true);
        for (int i = 0; i < 100; i++)
        {
            var blur = CreateOriginalBlur();
            // 模拟应用效果的过程
        }
        var afterOriginal = GC.GetTotalMemory(true);
        var originalMemoryUsage = afterOriginal - beforeMemory;

        // 测试优化版本内存使用
        beforeMemory = GC.GetTotalMemory(true);
        for (int i = 0; i < 100; i++)
        {
            var optimizedBlur = CreateOptimizedBlur(0.3);
            var result = optimizedBlur.ApplyBlur(testImage);
            // WriteableBitmap不需要手动释放
        }
        var afterOptimized = GC.GetTotalMemory(true);
        var optimizedMemoryUsage = afterOptimized - beforeMemory;

        Console.WriteLine($"原生版本内存使用: {originalMemoryUsage / 1024.0:F1} KB");
        Console.WriteLine($"优化版本内存使用: {optimizedMemoryUsage / 1024.0:F1} KB");
        
        var memoryImprovement = ((double)(originalMemoryUsage - optimizedMemoryUsage) / originalMemoryUsage) * 100;
        Console.WriteLine($"内存节省: {memoryImprovement:F1}%");
    }

    private static double MeasureBlurPerformance(BitmapSource testImage, BlurEffect blur, int iterations)
    {
        var times = new double[iterations];
        
        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            
            // 模拟BlurEffect应用过程
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawImage(testImage, new Rect(0, 0, testImage.Width, testImage.Height));
            }
            visual.Effect = blur;
            
            var renderTarget = new RenderTargetBitmap(
                (int)testImage.Width, (int)testImage.Height, 96, 96, PixelFormats.Pbgra32);
            renderTarget.Render(visual);
            
            sw.Stop();
            times[i] = sw.Elapsed.TotalMilliseconds;
        }

        return CalculateAverage(times);
    }

    private static double MeasureOptimizedBlurPerformance(BitmapSource testImage, OptimizedBlurEffect blur, int iterations)
    {
        var times = new double[iterations];
        
        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            
            var result = blur.ApplyBlur(testImage);
            // WriteableBitmap不需要手动释放
            
            sw.Stop();
            times[i] = sw.Elapsed.TotalMilliseconds;
        }

        return CalculateAverage(times);
    }

    private static BitmapSource CreateTestImage(int width, int height)
    {
        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);
        bitmap.Lock();
        
        try
        {
            unsafe
            {
                var buffer = (byte*)bitmap.BackBuffer;
                var stride = bitmap.BackBufferStride;
                
                // 创建渐变测试图像
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var offset = y * stride + x * 3;
                        buffer[offset] = (byte)(x * 255 / width);     // B
                        buffer[offset + 1] = (byte)(y * 255 / height); // G  
                        buffer[offset + 2] = (byte)((x + y) * 255 / (width + height)); // R
                    }
                }
            }
            
            bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        }
        finally
        {
            bitmap.Unlock();
        }
        
        return bitmap;
    }

    private static BlurEffect CreateOriginalBlur() => new()
    {
        Radius = 16.0,
        KernelType = KernelType.Gaussian,
        RenderingBias = RenderingBias.Performance
    };

    private static OptimizedBlurEffect CreateOptimizedBlur(double samplingRate) => new()
    {
        Radius = 16.0,
        SamplingRate = samplingRate,
        RenderingBias = RenderingBias.Performance,
        KernelType = KernelType.Gaussian
    };

    private static double CalculateAverage(double[] values)
    {
        if (values.Length == 0) return 0;
        
        Array.Sort(values);
        var trimCount = values.Length / 10; // 去除最高和最低10%的值
        var sum = 0.0;
        var count = 0;
        
        for (int i = trimCount; i < values.Length - trimCount; i++)
        {
            sum += values[i];
            count++;
        }
        
        return count > 0 ? sum / count : 0;
    }

    private static void DisplayTestSummary(PerformanceTestResult result)
    {
        Console.WriteLine("性能测试总结报告");
        Console.WriteLine(new string('=', 50));
        Console.WriteLine($"原生BlurEffect基准时间: {result.OriginalBlurTime:F2}ms");
        Console.WriteLine();
        
        Console.WriteLine("优化版本性能对比:");
        Console.WriteLine("采样率\t时间(ms)\t性能提升");
        Console.WriteLine(new string('-', 35));
        
        foreach (var optimized in result.OptimizedResults)
        {
            Console.WriteLine($"{optimized.SamplingRate:P0}\t{optimized.AverageTime:F2}\t\t{optimized.PerformanceImprovement:F1}%");
        }
        
        var bestResult = result.OptimizedResults.OrderByDescending(r => r.PerformanceImprovement).First();
        Console.WriteLine();
        Console.WriteLine($"🏆 最佳性能提升: {bestResult.PerformanceImprovement:F1}% (采样率 {bestResult.SamplingRate:P0})");
        Console.WriteLine(new string('=', 50));
    }
}

/// <summary>
/// 性能测试结果数据结构
/// </summary>
public class PerformanceTestResult
{
    public double OriginalBlurTime { get; set; }
    public List<OptimizedTestResult> OptimizedResults { get; set; } = new();
}

public class OptimizedTestResult
{
    public double SamplingRate { get; set; }
    public double AverageTime { get; set; }
    public double PerformanceImprovement { get; set; }
}

/// <summary>
/// 性能测试示例使用
/// </summary>
public static class PerformanceTestExamples
{
    /// <summary>
    /// 运行所有性能测试
    /// </summary>
    public static void RunAllTests()
    {
        Console.WriteLine("PCL.Core BlurEffect 性能优化测试套件\n");
        
        // 快速对比测试
        BlurPerformanceTest.QuickPerformanceComparison();
        Console.WriteLine("\n" + new string('=', 60) + "\n");
        
        // 完整基准测试
        BlurPerformanceTest.RunComprehensiveTest();
        Console.WriteLine("\n" + new string('=', 60) + "\n");
        
        // 内存使用测试
        BlurPerformanceTest.MemoryUsageTest();
        
        Console.WriteLine("\n所有性能测试完成!");
    }
}