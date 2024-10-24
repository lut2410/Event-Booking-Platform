using Xunit;
using FluentValidation.TestHelper;
using Bookings.Presentation.Validators;
using Bookings.Presentation.Models;
using System;

namespace Bookings.UnitTests.Presentation.Validators
{
    public class CreateBookingDtoValidatorTests
    {
        private readonly CreateBookingDtoValidator _validator;

        public CreateBookingDtoValidatorTests()
        {
            _validator = new CreateBookingDtoValidator();
        }

        [Fact]
        public void CreateBookingDto_ShouldHaveNoValidationErrors_WhenValidData()
        {
            // Arrange
            var dto = new CreateBookingDto
            {
                UserId = 1,
                EventId = 100,
                BookingDate = DateTimeOffset.Now.AddDays(1)
            };

            // Act & Assert
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void CreateBookingDto_ShouldHaveValidationError_WhenUserIdIsInvalid()
        {
            // Arrange
            var dto = new CreateBookingDto
            {
                UserId = 0,  // Invalid UserId
                EventId = 100,
                BookingDate = DateTimeOffset.Now.AddDays(1)
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                  .WithErrorMessage("UserId must be greater than 0.");
        }

        [Fact]
        public void CreateBookingDto_ShouldHaveValidationError_WhenBookingDateIsInThePast()
        {
            // Arrange
            var dto = new CreateBookingDto
            {
                UserId = 1,
                EventId = 100,
                BookingDate = DateTimeOffset.Now.AddDays(-1)  // Past date
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.BookingDate)
                  .WithErrorMessage("Booking date must be in the future or present.");
        }

        [Fact]
        public void CreateBookingDto_ShouldHaveValidationError_WhenEventIdIsInvalid()
        {
            // Arrange
            var dto = new CreateBookingDto
            {
                UserId = 1,
                EventId = 0,  // Invalid EventId
                BookingDate = DateTimeOffset.Now.AddDays(1)
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.EventId)
                  .WithErrorMessage("EventId must be greater than 0.");
        }
    }
}
