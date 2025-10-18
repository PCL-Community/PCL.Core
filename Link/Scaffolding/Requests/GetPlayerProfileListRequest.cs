using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using PCL.Core.Link.Scaffolding.Abstractions;
using PCL.Core.Link.Scaffolding.Models;

namespace PCL.Core.Link.Scaffolding.Requests;

public sealed class GetPlayerProfileListRequest : IRequest<IReadOnlyList<PlayerProfile>>
{
    private static readonly JsonSerializerOptions _JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <inheritdoc />
    public string RequestType { get; } = "c:player_profile_list";

    /// <inheritdoc />
    public void WriteRequestBody(IBufferWriter<byte> writer)
    {
        // empty request body
    }

    /// <inheritdoc />
    public IReadOnlyList<PlayerProfile> ParseResponseBody(ReadOnlyMemory<byte> responseBody)
    {
        var profiles = JsonSerializer.Deserialize<IReadOnlyList<PlayerProfile>>(responseBody.Span, _JsonOptions);
        return profiles ?? ArraySegment<PlayerProfile>.Empty;
    }
}