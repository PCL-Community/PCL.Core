using System;

namespace PCL.Core.Minecraft.Compoment.Exceptions;

public sealed class CacheResultNotFoundException(string column, string queryIndex)
    : Exception($"{queryIndex} not found in {column}.");