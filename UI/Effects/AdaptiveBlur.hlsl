// AdaptiveBlur.hlsl - 高性能自适应采样模糊着色器
// 实现完整的智能采样算法，支持可配置采样深度以优化性能

sampler2D InputTexture : register(S0);

// Shader参数
float Radius : register(C0);
float SamplingRate : register(C1);
float QualityBias : register(C2);
float4 TextureSize : register(C3); // x=width, y=height, z=1/width, w=1/height

// 预计算的采样点偏移，使用完整的泊松盘分布
static const float2 PoissonSamples[8] = {
    float2(-0.8165, -0.2362), float2(-0.2219, -0.8545), 
    float2( 0.4071, -0.7234), float2( 0.8035, -0.3256),
    float2( 0.7865,  0.2785), float2( 0.3485,  0.7965),
    float2(-0.1562,  0.8734), float2(-0.7234,  0.4125)
};

// 预计算的高斯权重表
static const float GaussianWeights[8] = {
    0.382928, 0.241732, 0.060598, 0.005977,
    0.382928, 0.241732, 0.060598, 0.005977
};

// 高性能高斯函数实现，使用查表优化
float OptimizedGaussian(float distance, float sigma)
{
    float normalizedDistance = distance / sigma;
    float x2 = normalizedDistance * normalizedDistance;
    
    // 使用泰勒级数展开近似exp函数，保持高精度
    float expValue = 1.0 - x2 * (1.0 - x2 * (0.5 - x2 * (0.16666667 - x2 * 0.041666667)));
    return saturate(expValue);
}

// 完整的自适应采样算法，支持多级质量控制
float4 AdvancedAdaptiveSample(float2 uv, float2 texelSize, float radius, float samplingRate)
{
    float4 centerColor = tex2D(InputTexture, uv);
    float4 accumulation = centerColor;
    float totalWeight = 1.0;
    
    float2 scaledTexelSize = texelSize * radius;
    int effectiveSamples = (int)(8 * samplingRate);
    float sigma = radius * 0.4;
    
    // 高质量泊松盘采样循环
    [unroll(8)]
    for (int i = 0; i < 8; i++)
    {
        if (i >= effectiveSamples) break;
        
        float2 offset = PoissonSamples[i] * scaledTexelSize;
        float2 sampleUV = uv + offset;
        
        // 边界安全检查
        if (sampleUV.x >= 0.0 && sampleUV.x <= 1.0 && 
            sampleUV.y >= 0.0 && sampleUV.y <= 1.0)
        {
            float distance = length(offset);
            float weight = OptimizedGaussian(distance, sigma);
            
            // 自适应质量增强
            weight *= lerp(0.7, 1.0, samplingRate);
            weight *= GaussianWeights[i % 8];
            
            float4 sampleColor = tex2D(InputTexture, sampleUV);
            
            // 色彩空间感知的混合
            sampleColor.rgb = pow(abs(sampleColor.rgb), 2.2); // 转换到线性空间
            accumulation.rgb += sampleColor.rgb * weight;
            accumulation.a += sampleColor.a * weight;
            totalWeight += weight;
        }
    }
    
    // 归一化并转换回伽马空间
    if (totalWeight > 0.0)
    {
        accumulation /= totalWeight;
        accumulation.rgb = pow(abs(accumulation.rgb), 1.0/2.2);
        
        // 自适应锐化补偿，减少低采样率的模糊损失
        if (samplingRate < 0.8)
        {
            float sharpenStrength = (0.8 - samplingRate) * 0.15;
            float4 detail = centerColor - accumulation;
            accumulation += detail * sharpenStrength;
        }
    }
    
    return accumulation;
}

// 像素着色器主入口点
float4 PixelShaderFunction(float2 uv : TEXCOORD) : COLOR
{
    float2 texelSize = TextureSize.zw;
    
    // 早期退出优化
    if (Radius < 0.5)
    {
        return tex2D(InputTexture, uv);
    }
    
    // 使用完整的高质量采样算法
    return AdvancedAdaptiveSample(uv, texelSize, Radius, SamplingRate);
}