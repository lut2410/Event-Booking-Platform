using FluentValidation;
using Bookings.Application.DTOs;

namespace Bookings.Presentation.Validators
{
    public class ReserveSeatsRequestValidator : AbstractValidator<ReserveSeatsRequest>
    {
        public ReserveSeatsRequestValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required.")
                .Must(id => id != Guid.Empty).WithMessage("UserId must be a valid GUID.");

            RuleFor(x => x.EventId)
                .NotEmpty().WithMessage("EventId is required.")
                .Must(id => id != Guid.Empty).WithMessage("EventId must be a valid GUID.");

            RuleFor(x => x.SeatIds)
                .NotEmpty().WithMessage("SeatIds list cannot be empty.")
                .Must(seats => seats.All(id => id != Guid.Empty)).WithMessage("Each SeatId must be a valid GUID.");
        }
    }
}
