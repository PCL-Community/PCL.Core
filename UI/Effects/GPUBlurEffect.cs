using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace PCL.Core.UI.Effects;

/// <summary>
/// GPU加速的高性能模糊效果，使用Pixel Shader实现硬件加速
/// 提供比CPU实现更好的性能，特别适合大图像和实时应用
/// </summary>
public sealed class GPUBlurEffect : ShaderEffect
{
    private const string PixelShaderUri = "pack://application:,,,/PCL.Core;component/UI/Assets/Shaders/AdaptiveBlur.ps";
    
    private static readonly object ShaderLock = new();
    private static PixelShader? _cachedShader;

    static GPUBlurEffect()
    {
        EnsureShaderInitialized();
    }

    public GPUBlurEffect()
    {
        PixelShader = _cachedShader;
        
        // 注册shader参数映射
        UpdateShaderValue(InputProperty);
        UpdateShaderValue(RadiusProperty);
        UpdateShaderValue(SamplingRateProperty);
        UpdateShaderValue(QualityBiasProperty);
        UpdateShaderValue(TextureSizeProperty);
        
        // 设置默认值
        Radius = 16.0;
        SamplingRate = 0.8;
        RenderingBias = RenderingBias.Performance;
        SetValue(TextureSizeProperty, new Point(1920, 1080));
    }

    /// <summary>
    /// 模糊半径，与原BlurEffect完全兼容
    /// </summary>
    public double Radius
    {
        get => (double)GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, Math.Max(0.0, Math.Min(100.0, value)));
    }

    /// <summary>
    /// 采样率控制 (0.1-1.0)，GPU优化的核心参数
    /// </summary>
    public double SamplingRate
    {
        get => (double)GetValue(SamplingRateProperty);
        set => SetValue(SamplingRateProperty, Math.Max(0.1, Math.Min(1.0, value)));
    }

    /// <summary>
    /// 渲染偏向设置
    /// </summary>
    public RenderingBias RenderingBias
    {
        get => (RenderingBias)GetValue(RenderingBiasProperty);
        set => SetValue(RenderingBiasProperty, value);
    }

    /// <summary>
    /// 内核类型兼容性属性
    /// </summary>
    public KernelType KernelType
    {
        get => (KernelType)GetValue(KernelTypeProperty);
        set => SetValue(KernelTypeProperty, value);
    }

    // Dependency Properties
    public static readonly DependencyProperty InputProperty = 
        ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(GPUBlurEffect), 0);

    public static readonly DependencyProperty RadiusProperty = 
        DependencyProperty.Register(nameof(Radius), typeof(double), typeof(GPUBlurEffect), 
            new UIPropertyMetadata(16.0, PixelShaderConstantCallback(0)), ValidateRadius);

    public static readonly DependencyProperty SamplingRateProperty = 
        DependencyProperty.Register(nameof(SamplingRate), typeof(double), typeof(GPUBlurEffect), 
            new UIPropertyMetadata(0.8, PixelShaderConstantCallback(1)), ValidateSamplingRate);

    public static readonly DependencyProperty QualityBiasProperty = 
        DependencyProperty.Register("QualityBias", typeof(double), typeof(GPUBlurEffect), 
            new UIPropertyMetadata(0.0, PixelShaderConstantCallback(2)));

    public static readonly DependencyProperty TextureSizeProperty = 
        DependencyProperty.Register("TextureSize", typeof(Point), typeof(GPUBlurEffect), 
            new UIPropertyMetadata(new Point(1920, 1080), PixelShaderConstantCallback(3)));

    public static readonly DependencyProperty RenderingBiasProperty = 
        DependencyProperty.Register(nameof(RenderingBias), typeof(RenderingBias), typeof(GPUBlurEffect), 
            new PropertyMetadata(RenderingBias.Performance, OnRenderingBiasChanged));

    public static readonly DependencyProperty KernelTypeProperty = 
        DependencyProperty.Register(nameof(KernelType), typeof(KernelType), typeof(GPUBlurEffect), 
            new PropertyMetadata(KernelType.Gaussian));

    public Brush Input
    {
        get => (Brush)GetValue(InputProperty);
        set => SetValue(InputProperty, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ValidateRadius(object value) => 
        value is double d && d >= 0.0 && d <= 100.0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ValidateSamplingRate(object value) => 
        value is double d && d >= 0.1 && d <= 1.0;

    private static void OnRenderingBiasChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GPUBlurEffect effect)
        {
            var qualityBias = e.NewValue is RenderingBias.Quality ? 1.0 : 0.0;
            effect.SetValue(QualityBiasProperty, qualityBias);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void EnsureShaderInitialized()
    {
        if (_cachedShader != null) return;

        lock (ShaderLock)
        {
            if (_cachedShader == null)
            {
                try
                {
                    _cachedShader = new PixelShader
                    {
                        UriSource = new Uri(PixelShaderUri, UriKind.Absolute)
                    };
                }
                catch
                {
                    // 如果着色器文件不存在，创建一个空的着色器作为回退
                    _cachedShader = new PixelShader();
                }
            }
        }
    }

    /// <summary>
    /// 自动设置纹理大小以优化GPU渲染
    /// </summary>
    public void SetTextureSize(Size size)
    {
        SetValue(TextureSizeProperty, new Point(size.Width, size.Height));
    }

    protected override Freezable CreateInstanceCore()
    {
        return new GPUBlurEffect();
    }

    protected override void CloneCore(Freezable sourceFreezable)
    {
        if (sourceFreezable is GPUBlurEffect source)
        {
            Radius = source.Radius;
            SamplingRate = source.SamplingRate;
            RenderingBias = source.RenderingBias;
            KernelType = source.KernelType;
        }
        else if (sourceFreezable is BlurEffect originalBlur)
        {
            // 兼容原生BlurEffect
            Radius = originalBlur.Radius;
            RenderingBias = originalBlur.RenderingBias;
            KernelType = originalBlur.KernelType;
            SamplingRate = 0.8; // GPU优化的默认采样率
        }
        base.CloneCore(sourceFreezable);
    }

    /// <summary>
    /// 性能预设配置
    /// </summary>
    public static class Presets
    {
        /// <summary>
        /// GPU极致性能：10%采样，适合实时预览
        /// </summary>
        public static GPUBlurEffect UltraFast(double radius = 16.0) => new()
        {
            Radius = radius,
            SamplingRate = 0.1,
            RenderingBias = RenderingBias.Performance
        };

        /// <summary>
        /// GPU高性能：30%采样，性能优先
        /// </summary>
        public static GPUBlurEffect HighPerformance(double radius = 16.0) => new()
        {
            Radius = radius,
            SamplingRate = 0.3,
            RenderingBias = RenderingBias.Performance
        };

        /// <summary>
        /// GPU平衡模式：70%采样，质量和性能平衡
        /// </summary>
        public static GPUBlurEffect Balanced(double radius = 16.0) => new()
        {
            Radius = radius,
            SamplingRate = 0.7,
            RenderingBias = RenderingBias.Performance
        };

        /// <summary>
        /// GPU最佳质量：90%采样，接近原生质量
        /// </summary>
        public static GPUBlurEffect BestQuality(double radius = 16.0) => new()
        {
            Radius = radius,
            SamplingRate = 0.9,
            RenderingBias = RenderingBias.Quality
        };
    }
}
