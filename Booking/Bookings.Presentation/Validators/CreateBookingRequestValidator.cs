using FluentValidation;
using Bookings.Presentation.Models;

namespace Bookings.Presentation.Validators
{
    public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
    {
        public CreateBookingRequestValidator()
        {
            RuleFor(x => x.EventId)
                .NotEmpty().WithMessage("EventId is required.")
                .Must(id => id != Guid.Empty).WithMessage("EventId must be a valid GUID.");

            RuleFor(x => x.SeatIds)
                .NotEmpty().WithMessage("SeatIds cannot be empty.")
                .Must(ids => ids != null && ids.All(id => id != Guid.Empty)).WithMessage("All SeatIds must be valid GUIDs.");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required.");
        }
    }
}
