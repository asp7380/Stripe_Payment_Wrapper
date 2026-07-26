using StripeApiWrapper.Api.Contracts;

namespace StripeApiWrapper.Api.Services;

public interface IPaymentService
{
    Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken);
}
