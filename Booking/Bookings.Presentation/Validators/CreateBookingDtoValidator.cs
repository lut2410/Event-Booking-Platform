using FluentValidation;
using Bookings.Presentation.Models;

namespace Bookings.Presentation.Validators
{
    public class CreateBookingDtoValidator : AbstractValidator<CreateBookingDto>
    {
        public CreateBookingDtoValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("UserId must be greater than 0.");

            RuleFor(x => x.EventId)
                .GreaterThan(0).WithMessage("EventId must be greater than 0.");

            RuleFor(x => x.BookingDate)
                .GreaterThanOrEqualTo(DateTime.Now).WithMessage("Booking date must be in the future or present.");
        }
    }
}
