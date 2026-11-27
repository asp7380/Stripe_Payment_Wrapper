using System.Text.Json;
using System.Text.Json.Serialization;

namespace StripeApiWrapper.Api.Contracts;

// Cross-boundary payment status. Serialized as Stripe's snake_case wire values
// (e.g. "succeeded", "requires_action") via the converter below, so a
// GraphQL/codegen client gets a matching enum. The [JsonConverter] on the type
// means the snake_case policy applies wherever this type is (de)serialized,
// including client-side deserialization.
[JsonConverter(typeof(PaymentStatusJsonConverter))]
public enum PaymentStatus
{
    Unknown = 0,
    RequiresPaymentMethod,
    RequiresConfirmation,
    RequiresAction,
    Processing,
    RequiresCapture,
    Canceled,
    Succeeded,
}

public sealed class PaymentStatusJsonConverter()
    : JsonStringEnumConverter<PaymentStatus>(JsonNamingPolicy.SnakeCaseLower);

public static class PaymentStatusMapper
{
    // Built from the enum names via the same snake_case policy the JSON converter
    // uses, so the code mapping and the wire values can never drift.
    private static readonly IReadOnlyDictionary<string, PaymentStatus> ByWireValue =
        Enum.GetValues<PaymentStatus>().ToDictionary(ToWireValue, static value => value);

    public static string ToWireValue(PaymentStatus status) =>
        JsonNamingPolicy.SnakeCaseLower.ConvertName(status.ToString());

    // Maps a Stripe PaymentIntent.Status string to the enum; unknown/absent
    // values map to PaymentStatus.Unknown.
    public static PaymentStatus FromStripe(string? stripeStatus) =>
        stripeStatus is not null && ByWireValue.TryGetValue(stripeStatus, out var status)
            ? status
            : PaymentStatus.Unknown;
}
