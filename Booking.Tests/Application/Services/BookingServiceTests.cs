//using Bookings.Application.Services;
//using Bookings.Core.Entities;
//using Bookings.Core.Interfaces;
//using Moq;
//using Xunit;
//namespace Bookings.Tests.Application.Services
//{
//    public class BookingServiceTests
//    {
//        private readonly Mock<IBookingRepository> _mockBookingRepository;
//        private readonly BookingService _bookingService;

//        public BookingServiceTests()
//        {
//            _mockBookingRepository = new Mock<IBookingRepository>();
//            _bookingService = new BookingService(_mockBookingRepository.Object);
//        }

//        [Fact]
//        public void AddBooking_WithValidBooking_ShouldCallRepositoryOnce()
//        {
//            // Arrange
//            var validBooking = new Booking
//            {
//                UserId = 1,
//                EventId = 1,
//                BookingDate = DateTimeOffset.Now.AddDays(1)
//            };

//            // Act
//            _bookingService.AddBooking(validBooking);

//            // Assert
//            _mockBookingRepository.Verify(r => r.AddBooking(validBooking), Times.Once);
//        }

//        //[Fact]
//        //public void GetBookingById_WithExistingId_ShouldReturnBooking()
//        //{
//        //    // Arrange
//        //    var booking = new Booking { Id = 1, UserId = 1, EventId = 100, BookingDate = DateTimeOffset.Now.AddDays(1) };
//        //    _mockBookingRepository.Setup(r => r.GetBookingById(1)).Returns(booking);

//        //    // Act
//        //    var result = _bookingService.GetBookingById(1);

//        //    // Assert
//        //    Assert.NotNull(result);
//        //    Assert.Equal(1, result.Id);
//        //}

//        //[Fact]
//        //public void GetBookingById_WithNonExistingId_ShouldReturnNull()
//        //{
//        //    // Arrange
//        //    _mockBookingRepository.Setup(r => r.GetBookingById(It.IsAny<int>())).Returns((Booking)null);

//        //    // Act
//        //    var result = _bookingService.GetBookingById(99);

//        //    // Assert
//        //    Assert.Null(result);
//        //}

//        //[Fact]
//        //public void GetAllBookings_ShouldReturnListOfBookings()
//        //{
//        //    // Arrange
//        //    var bookings = new[]
//        //    {
//        //        new Booking { Id = 1, UserId = 1, EventId = 100, BookingDate = DateTimeOffset.Now.AddDays(1) },
//        //        new Booking { Id = 2, UserId = 2, EventId = 101, BookingDate = DateTimeOffset.Now.AddDays(2) }
//        //    };
//        //    _mockBookingRepository.Setup(r => r.GetAllBookings()).Returns(bookings);

//        //    // Act
//        //    var result = _bookingService.GetAllBookings().ToList();

//        //    // Assert
//        //    Assert.Equal(2, result.Count);
//        //    Assert.Equal(1, result[0].Id);
//        //    Assert.Equal(2, result[1].Id);
//        //}
//    }
//}
