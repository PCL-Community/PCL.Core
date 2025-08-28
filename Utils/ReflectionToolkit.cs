using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Utils;

/// <summary>
/// 反射和序列化工具包
/// </summary>
public static class ReflectionToolkit
{
    /// <summary>
    /// 属性访问器缓存
    /// </summary>
    public static class FastPropertyAccessor
    {
        // 类型缓存 - 避免重复反射
        private static readonly ConcurrentDictionary<Type, TypeInfo> TypeInfoCache = new();
        
        // 属性访问器缓存
        private static readonly ConcurrentDictionary<string, Delegate> GetterCache = new();
        private static readonly ConcurrentDictionary<string, Delegate> SetterCache = new();
        
        [StructLayout(LayoutKind.Sequential, Size = 64)] // 缓存行对齐
        private sealed class TypeInfo
        {
            public readonly PropertyInfo[] Properties;
            public readonly FieldInfo[] Fields;
            public readonly Dictionary<string, PropertyInfo> PropertyMap;
            public readonly Dictionary<string, FieldInfo> FieldMap;
            
            public TypeInfo(Type type)
            {
                Properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                Fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                PropertyMap = Properties.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
                FieldMap = Fields.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);
            }
        }
        
        /// <summary>
        /// 获取属性值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static T GetPropertyValue<T>(object obj, string propertyName)
        {
            var type = obj.GetType();
            var cacheKey = $"{type.FullName}.{propertyName}";
            
            // 快速路径：从缓存获取已编译的访问器
            if (GetterCache.TryGetValue(cacheKey, out var cachedGetter))
            {
                return ((Func<object, T>)cachedGetter)(obj);
            }
            
            // 慢路径：编译新的访问器
            return CompileAndCacheGetter<T>(type, propertyName, cacheKey)(obj);
        }
        
        /// <summary>
        /// 设置属性值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void SetPropertyValue<T>(object obj, string propertyName, T value)
        {
            var type = obj.GetType();
            var cacheKey = $"{type.FullName}.{propertyName}";
            
            // 快速路径：从缓存获取已编译的访问器
            if (SetterCache.TryGetValue(cacheKey, out var cachedSetter))
            {
                ((Action<object, T>)cachedSetter)(obj, value);
                return;
            }
            
            // 慢路径：编译新的访问器
            CompileAndCacheSetter<T>(type, propertyName, cacheKey)(obj, value);
        }
        
        /// <summary>
        /// 编译并缓存getter - 使用表达式树
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)] // 慢路径不内联
        private static Func<object, T> CompileAndCacheGetter<T>(Type type, string propertyName, string cacheKey)
        {
            var typeInfo = TypeInfoCache.GetOrAdd(type, t => new TypeInfo(t));
            
            if (typeInfo.PropertyMap.TryGetValue(propertyName, out var property))
            {
                // 创建表达式树：(obj) => ((TObject)obj).Property
                var objParam = Expression.Parameter(typeof(object), "obj");
                var castExpr = Expression.Convert(objParam, type);
                var propertyExpr = Expression.Property(castExpr, property);
                var convertExpr = Expression.Convert(propertyExpr, typeof(T));
                
                var lambda = Expression.Lambda<Func<object, T>>(convertExpr, objParam);
                var compiled = lambda.Compile();
                
                GetterCache.TryAdd(cacheKey, compiled);
                return compiled;
            }
            
            throw new ArgumentException($"属性 '{propertyName}' 在类型 '{type.Name}' 中不存在");
        }
        
        /// <summary>
        /// 编译并缓存setter - 使用表达式树
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)] // 慢路径不内联
        private static Action<object, T> CompileAndCacheSetter<T>(Type type, string propertyName, string cacheKey)
        {
            var typeInfo = TypeInfoCache.GetOrAdd(type, t => new TypeInfo(t));
            
            if (typeInfo.PropertyMap.TryGetValue(propertyName, out var property) && property.CanWrite)
            {
                // 创建表达式树：(obj, value) => ((TObject)obj).Property = (TProperty)value
                var objParam = Expression.Parameter(typeof(object), "obj");
                var valueParam = Expression.Parameter(typeof(T), "value");
                var castExpr = Expression.Convert(objParam, type);
                var propertyExpr = Expression.Property(castExpr, property);
                var convertValueExpr = Expression.Convert(valueParam, property.PropertyType);
                var assignExpr = Expression.Assign(propertyExpr, convertValueExpr);
                
                var lambda = Expression.Lambda<Action<object, T>>(assignExpr, objParam, valueParam);
                var compiled = lambda.Compile();
                
                SetterCache.TryAdd(cacheKey, compiled);
                return compiled;
            }
            
            throw new ArgumentException($"属性 '{propertyName}' 在类型 '{type.Name}' 中不存在或不可写");
        }
        
        /// <summary>
        /// 批量属性复制 - 超高性能对象映射
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void CopyProperties<TSource, TTarget>(TSource source, TTarget target)
        {
            var copyKey = $"{typeof(TSource).FullName}->{typeof(TTarget).FullName}";
            
            if (!PropertyCopyCache.TryGetValue(copyKey, out var copyAction))
            {
                copyAction = CompilePropertyCopyAction<TSource, TTarget>();
                PropertyCopyCache.TryAdd(copyKey, copyAction);
            }
            
            ((Action<TSource, TTarget>)copyAction)(source, target);
        }
        
        private static readonly ConcurrentDictionary<string, Delegate> PropertyCopyCache = new();
        
        /// <summary>
        /// 编译属性复制动作 - 批量操作优化
        /// </summary>
        private static Action<TSource, TTarget> CompilePropertyCopyAction<TSource, TTarget>()
        {
            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);
            
            var sourceParam = Expression.Parameter(sourceType, "source");
            var targetParam = Expression.Parameter(targetType, "target");
            var expressions = new List<Expression>();
            
            var sourceProps = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead).ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
            var targetProps = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite);
            
            foreach (var targetProp in targetProps)
            {
                if (sourceProps.TryGetValue(targetProp.Name, out var sourceProp) && 
                    targetProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType))
                {
                    var sourceValue = Expression.Property(sourceParam, sourceProp);
                    var targetProperty = Expression.Property(targetParam, targetProp);
                    var assign = Expression.Assign(targetProperty, sourceValue);
                    expressions.Add(assign);
                }
            }
            
            if (expressions.Count == 0)
                return (s, t) => { }; // 空操作
            
            var block = Expression.Block(expressions);
            var lambda = Expression.Lambda<Action<TSource, TTarget>>(block, sourceParam, targetParam);
            return lambda.Compile();
        }
    }
    
    /// <summary>
    /// JSON序列化器
    /// </summary>
    public static class UltraJsonSerializer
    {
        private static readonly ConcurrentDictionary<Type, Delegate> SerializerCache = new();
        private static readonly ConcurrentDictionary<Type, Delegate> DeserializerCache = new();
        
        // 预配置的JsonSerializerOptions - 避免重复创建
        private static readonly JsonSerializerOptions DefaultOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        
        // 线程本地缓冲区池
        private static readonly ThreadLocal<ArrayBufferWriter<byte>> BufferWriterPool = 
            new(() => new ArrayBufferWriter<byte>(4096));
        
        /// <summary>
        /// 序列化到字符串 - 使用缓冲池优化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static string SerializeToString<T>(T value)
        {
            if (value == null) return "null";
            
            var bufferWriter = BufferWriterPool.Value!;
            bufferWriter.Clear();
            
            try
            {
                using var writer = new Utf8JsonWriter(bufferWriter);
                JsonSerializer.Serialize(writer, value, DefaultOptions);
                return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
            }
            finally
            {
                if (bufferWriter.WrittenCount > 32768) // 32KB
                {
                    // 重置过大的缓冲区
                    BufferWriterPool.Value = new ArrayBufferWriter<byte>(4096);
                }
            }
        }
        
        /// <summary>
        /// 序列化到字节数组
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static byte[] SerializeToBytes<T>(T value)
        {
            if (value == null) return "null"u8.ToArray();
            
            var bufferWriter = BufferWriterPool.Value!;
            bufferWriter.Clear();
            
            try
            {
                using var writer = new Utf8JsonWriter(bufferWriter);
                JsonSerializer.Serialize(writer, value, DefaultOptions);
                return bufferWriter.WrittenSpan.ToArray();
            }
            finally
            {
                if (bufferWriter.WrittenCount > 32768)
                {
                    BufferWriterPool.Value = new ArrayBufferWriter<byte>(4096);
                }
            }
        }
        
        /// <summary>
        /// 反序列化 - 使用Span优化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static T? Deserialize<T>(ReadOnlySpan<byte> jsonUtf8)
        {
            if (jsonUtf8.IsEmpty) return default;
            
            var reader = new Utf8JsonReader(jsonUtf8);
            return JsonSerializer.Deserialize<T>(ref reader, DefaultOptions);
        }
        
        /// <summary>
        /// 反序列化 - 字符串版本
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static T? Deserialize<T>(string json)
        {
            if (string.IsNullOrEmpty(json)) return default;
            
            // 对小字符串使用栈分配
            if (json.Length <= 1024)
            {
                Span<byte> buffer = stackalloc byte[Encoding.UTF8.GetMaxByteCount(json.Length)];
                var bytesWritten = Encoding.UTF8.GetBytes(json, buffer);
                return Deserialize<T>(buffer[..bytesWritten]);
            }
            
            // 大字符串使用常规转换
            var bytes = Encoding.UTF8.GetBytes(json);
            return Deserialize<T>(bytes);
        }
        
        /// <summary>
        /// 流式序列化 - 适用于大对象
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static async ValueTask SerializeToStreamAsync<T>(T value, Stream stream, CancellationToken cancellationToken = default)
        {
            await JsonSerializer.SerializeAsync(stream, value, DefaultOptions, cancellationToken);
        }
        
        /// <summary>
        /// 流式反序列化 - 适用于大JSON
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static async ValueTask<T?> DeserializeFromStreamAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        {
            return await JsonSerializer.DeserializeAsync<T>(stream, DefaultOptions, cancellationToken);
        }
        
        /// <summary>
        /// 批量序列化 - 优化多对象处理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static string SerializeBatch<T>(IEnumerable<T> items)
        {
            var bufferWriter = BufferWriterPool.Value!;
            bufferWriter.Clear();
            
            try
            {
                using var writer = new Utf8JsonWriter(bufferWriter);
                writer.WriteStartArray();
                
                foreach (var item in items)
                {
                    JsonSerializer.Serialize(writer, item, DefaultOptions);
                }
                
                writer.WriteEndArray();
                return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
            }
            finally
            {
                if (bufferWriter.WrittenCount > 65536) // 64KB
                {
                    BufferWriterPool.Value = new ArrayBufferWriter<byte>(4096);
                }
            }
        }
    }
    
    /// <summary>
    /// 对象克隆器
    /// </summary>
    public static class ObjectCloner
    {
        private static readonly ConcurrentDictionary<Type, Delegate> CloneCache = new();
        
        /// <summary>
        /// 深度克隆对象
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static T DeepClone<T>(T original) where T : class
        {
            if (original == null) return null!;
            
            var type = typeof(T);
            if (!CloneCache.TryGetValue(type, out var cloneFunc))
            {
                cloneFunc = CompileCloneFunction<T>();
                CloneCache.TryAdd(type, cloneFunc);
            }
            
            return ((Func<T, T>)cloneFunc)(original);
        }
        
        /// <summary>
        /// 编译克隆函数
        /// </summary>
        private static Func<T, T> CompileCloneFunction<T>()
        {
            var type = typeof(T);
            var originalParam = Expression.Parameter(type, "original");
            
            // 创建新实例
            var newInstance = Expression.Variable(type, "clone");
            var constructor = type.GetConstructor(Type.EmptyTypes);
            
            if (constructor == null)
            {
                throw new ArgumentException($"类型 {type.Name} 必须有无参构造函数");
            }
            
            var createInstance = Expression.Assign(newInstance, Expression.New(constructor));
            var expressions = new List<Expression> { createInstance };
            
            // 复制所有可写属性
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite);
            
            foreach (var property in properties)
            {
                var originalValue = Expression.Property(originalParam, property);
                var cloneProperty = Expression.Property(newInstance, property);
                
                if (IsSimpleType(property.PropertyType))
                {
                    // 简单类型直接赋值
                    var assign = Expression.Assign(cloneProperty, originalValue);
                    expressions.Add(assign);
                }
                else if (property.PropertyType.IsClass)
                {
                    // 引用类型递归克隆
                    var recursiveClone = Expression.Call(
                        typeof(ObjectCloner).GetMethod(nameof(DeepClone))!.MakeGenericMethod(property.PropertyType),
                        originalValue);
                    var assign = Expression.Assign(cloneProperty, recursiveClone);
                    expressions.Add(assign);
                }
            }
            
            expressions.Add(newInstance); // 返回克隆的实例
            
            var block = Expression.Block(new[] { newInstance }, expressions);
            var lambda = Expression.Lambda<Func<T, T>>(block, originalParam);
            
            return lambda.Compile();
        }
        
        /// <summary>
        /// 判断是否为简单类型
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive || 
                   type.IsEnum || 
                   type == typeof(string) || 
                   type == typeof(decimal) || 
                   type == typeof(DateTime) || 
                   type == typeof(DateTimeOffset) || 
                   type == typeof(TimeSpan) || 
                   type == typeof(Guid);
        }
    }
    
    /// <summary>
    /// 类型激活器
    /// 使用表达式树预编译构造函数
    /// </summary>
    public static class Activator
    {
        private static readonly ConcurrentDictionary<Type, Delegate> ActivatorCache = new();
        
        /// <summary>
        /// 创建实例
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static T CreateInstance<T>() where T : new()
        {
            var type = typeof(T);
            
            if (!ActivatorCache.TryGetValue(type, out var activator))
            {
                activator = CompileActivator<T>();
                ActivatorCache.TryAdd(type, activator);
            }
            
            return ((Func<T>)activator)();
        }
        
        /// <summary>
        /// 编译激活器
        /// </summary>
        private static Func<T> CompileActivator<T>()
        {
            var constructor = typeof(T).GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                throw new ArgumentException($"类型 {typeof(T).Name} 必须有无参构造函数");
            
            var newExpr = Expression.New(constructor);
            var lambda = Expression.Lambda<Func<T>>(newExpr);
            return lambda.Compile();
        }
        
        /// <summary>
        /// 创建实例（非泛型版本）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static object CreateInstance(Type type)
        {
            if (!ActivatorCache.TryGetValue(type, out var activator))
            {
                activator = CompileNonGenericActivator(type);
                ActivatorCache.TryAdd(type, activator);
            }
            
            return ((Func<object>)activator)();
        }
        
        /// <summary>
        /// 编译非泛型激活器
        /// </summary>
        private static Func<object> CompileNonGenericActivator(Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                throw new ArgumentException($"类型 {type.Name} 必须有无参构造函数");
            
            var newExpr = Expression.New(constructor);
            var convertExpr = Expression.Convert(newExpr, typeof(object));
            var lambda = Expression.Lambda<Func<object>>(convertExpr);
            return lambda.Compile();
        }
    }
}