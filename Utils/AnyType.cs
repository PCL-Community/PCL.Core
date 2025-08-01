using System;

namespace PCL.Core.Utils;

/// <summary>
/// A value with an unknown type during coding and compiling time.
/// </summary>
/// <param name="value">the origin value</param>
public class AnyType(object value)
{
    /// <summary>
    /// Get the origin value.
    /// </summary>
    public object Value() => value;
    
    /// <summary>
    /// Get the type of the value.
    /// </summary>
    public Type Type() => value.GetType();
    
    /// <summary>
    /// Get the value with an expected type.
    /// </summary>
    /// <typeparam name="T">the expected type</typeparam>
    /// <returns>the typed value</returns>
    public T Value<T>() => (T) value;

    /// <summary>
    /// Try getting the value with an expected type.
    /// </summary>
    /// <typeparam name="T">the expected type</typeparam>
    /// <returns>the typed value, or <c>null</c> if the type is incorrect</returns>
    public T? Try<T>() => (T?)((value is T) ? value : null);

    public override string ToString() => value.ToString();
    public override int GetHashCode() => value.GetHashCode();
    public override bool Equals(object? obj) => value.Equals(obj);
}
