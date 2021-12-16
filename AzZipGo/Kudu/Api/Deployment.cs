using System;
using System.Text.Json.Serialization;

namespace AzZipGo.Kudu.Api;

public record struct Deployment(
    [property: JsonPropertyName("id")] string? ID,
    [property: JsonPropertyName("status")] DeployStatus? Status,
    [property: JsonPropertyName("last_success_end_time")] DateTimeOffset? LastSuccessEndTime
);
