using System;

namespace PCL.Core.Link.Scaffolding.Abstractions;

public record ScaffoldingResponse(byte Status, ReadOnlyMemory<byte> Body);