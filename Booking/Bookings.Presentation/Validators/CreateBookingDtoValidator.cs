using FluentValidation;
using Bookings.Presentation.Models;

namespace Bookings.Presentation.Validators
{
    public class CreateBookingDtoValidator : AbstractValidator<CreateBookingDto>
    {
        public CreateBookingDtoValidator()
        {
            RuleFor(x => x.BookingDate)
                .GreaterThanOrEqualTo(DateTime.Now).WithMessage("Booking date must be in the future or present.");
        }
    }
}
