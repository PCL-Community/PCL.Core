using System;

namespace PCL.Core.App.Configuration;

public record ConfigItemInfo(
    string Key,
    ConfigStorage Storage,
    Type ValueType
);
