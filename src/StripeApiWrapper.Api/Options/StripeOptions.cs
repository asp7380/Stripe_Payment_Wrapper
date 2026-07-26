using System.ComponentModel.DataAnnotations;

namespace StripeApiWrapper.Api.Options;

public sealed class StripeOptions
{
    [Required]
    public string SecretKey { get; init; } = string.Empty;
}
