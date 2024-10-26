// File: Bookings.Application/Validators/ConfirmPaymentRequestValidator.cs

using Bookings.Application.DTOs;
using FluentValidation;

namespace Bookings.Presentation.Validators
{
    public class ConfirmPaymentRequestValidator : AbstractValidator<ConfirmPaymentRequest>
    {
        public ConfirmPaymentRequestValidator()
        {
            RuleFor(x => x.BookingId)
                .NotEmpty().WithMessage("BookingId is required.")
                .Must(id => id != Guid.Empty).WithMessage("BookingId must be a valid GUID.");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required.")
                .Must(id => id != Guid.Empty).WithMessage("UserId must be a valid GUID.");

            RuleFor(x => x.PaymentRequest)
                .NotNull().WithMessage("Payment details are required.")
                .SetValidator(new PaymentRequestValidator());
        }
    }

    public class PaymentRequestValidator : AbstractValidator<PaymentRequest>
    {
        public PaymentRequestValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero.");

            RuleFor(x => x.PaymentMethodId)
                .NotEmpty().WithMessage("PaymentMethodId is required.");
        }
    }
}
