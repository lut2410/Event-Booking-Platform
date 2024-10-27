

### `CONTRIBUTING.md`


# Contributing to Booking Service

Thank you for considering contributing to the Booking Service! This document provides guidelines to help you make a meaningful contribution.

Seting Up the Environment
1. Clone the repository:

```
git clone https://github.com/your-username/booking-service.git
cd booking-service
```

2. Run Docker Compose to set up dependencies:
    ```
    docker-compose up --build
    ```

3. Generate migrations (if code changes):
    ```
    dotnet ef migrations add AddColumns  --startup-project Booking/Bookings.Presentation --context AppDbContext --project Booking/Bookings.Infrastructure
    ```
4.
4.1. Start only Booking.Presentation
    ```
    docker-compose up booking --build
    ```
4.2. Debug via Visual Studio with any http, https

5. Submit a Pull Request (PR) to the `main` branch with a clear description of your changes.

6. Wait for feedback

## Coding Standards
- **Use .NET 6**: Ensure code is compatible with .NET 6.
- **Naming Conventions**: Follow C# naming conventions (PascalCase for classes and methods, camelCase for variables).
- **Logging**: Use structured logging with placeholders.
- **Testing**: Write unit and integration tests for new features and bug fixes.
- **Code Documentation**: Document functions, especially public ones, and include XML comments.

We appreciate your contribution and look forward to collaborating with you!
