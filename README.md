
# **Event Booking Platform**

The **Event Booking Platform** consists of multiple microservices such as managing seats, searching seats, booking event seats, and notifications. This repository focuses only on some services; other platform-level details are addressed in the **Architecture Design Document**.

Currently, this repository contains two services:

-   **Event Management Service**: This service is included to give an overview of the platform structure but is not the focus in terms of logic or development at this stage.
-   **Booking Service**: This is the primary service that requires attention and development.

----------

## 1. System Architecture (of Booking Service)

-   **Clean Architecture**: The system is designed with a layered approach, separating Core, Application, Infrastructure, and Presentation layers.
-   **Global Exception Handling**: Exception handling and validation are implemented using **FluentValidation**.
-   **Payment Integration**: Integration with **Stripe** for secure payment handling.
-   **Dockerized**: The service is containerized using **Docker** for scalability and easy deployment.

----------

## 2. Tech Stack

-   **.NET 8.0**: The latest version of the .NET framework for building scalable web APIs.
-   **Stripe SDK**: To handle payment processing.
-   **Entity Framework Core**: For database interactions using object-relational mapping (ORM).
-   **Docker**: For containerizing the application and ensuring consistency across environments.
-   **FluentValidation**: To validate API inputs and enforce data integrity.
-   **Prometheus + Grafana**: For real-time monitoring and metrics visualization.
-   **Elasticsearch + Logstash + Kibana (ELK Stack)**: For centralized logging and log visualization.
-   **xUnit**: For unit and integration tests to ensure code reliability.

----------

## 3. Project Structure

```
EventBooking/
│
├── Booking/
│   ├── Core/              # Core domain for Booking
│   ├── Application/       # Application logic for Booking
│   ├── Infrastructure/    # Infrastructure logic for Booking
│   └── Presentation/      # Presentation layer for Booking
│ 
├── Booking.Tests/
│   ├── UnitTests/         # Unit tests for Booking
│   └── IntegrationTests/  # Integration tests for Booking
│   
├── EventManagement/
│   ├── Core/              # Core domain for Event Management
│   ├── Application/       # Application logic for Event Management
│   ├── Infrastructure/    # Infrastructure logic for Event Management
│   └── Presentation/      # Presentation layer for Event Management
│ 
├── EventManagement.Tests/
│   ├── UnitTests/         # Unit tests for Event Management
│   └── IntegrationTests/  # Integration tests for Event Management
│
└── Solution items         # Global-level items, such as docker-compose.yml and README.md
```
----------

## 4. Setup Instructions

To run the system, follow these steps:

### 0. Prerequisites

-   **Docker** and **Docker Compose**
-   **.NET SDK (v8.0)** for running tests locally

### 1. Clone the Repository

```
git clone <repository-link>
cd "EB Assignment"
```

### 2. Configure Environment Variables

Create a `.env` file in the root of your project:

```
cp .env.example .env
``` 

Then, modify values in the `.env` file if necessary. For example, adjust the following variables:

```
# Application environment
ASPNETCORE_ENVIRONMENT=Development

# Payment gateway API keys (e.g., Stripe)
STRIPE_API_KEY=sk_test_4eC39HqLyjWDarjtT1zdp7dc

# Elasticsearch URL
ELASTICSEARCH_HOST=http://elasticsearch:9200

# Elasticsearch credentials
ELASTICSEARCH_USERNAME=elasticadmin
ELASTICSEARCH_PASSWORD=elasticPass@123` 
```
### 3. Start the Services

```
docker-compose up --build
```

### 4. Verify the Services

-   **Booking Service**: [http://localhost:6001](http://localhost:6001)
-   **Event Management Service**: [http://localhost:6002](http://localhost:6002)
-   **Prometheus**: [http://localhost:9090](http://localhost:9090)
-   **Grafana**: [http://localhost:3000](http://localhost:3000)
-   **Kibana**: [http://localhost:5601](http://localhost:5601)

### 5. Set Up Kibana and Grafana

#### 5.1 Kibana

1.  Open Kibana at [http://localhost:5601](http://localhost:5601).
2.  Navigate to **Stack Management** → **Index Patterns**.
3.  Create a new index pattern:
    -   Use `logstash-*` or `booking-logs-*`, depending on how the logs are indexed.
    -   Select `@timestamp` as the time filter field.
4.  Go to **Discover** to view logs from the Booking and Event Management services.

#### 5.2 Grafana

1.  Open Grafana at [http://localhost:3000](http://localhost:3000).
    -   Default username/password: `admin/admin`.
2.  Add **Prometheus** as a data source:
    -   Navigate to **Configuration** → **Data Sources**.
    -   Click **Add data source** and select **Prometheus**.
    -   Set the URL to `http://prometheus:9090`.
    -   Click **Save & Test**.
3.  Create dashboards to monitor metrics from the **Booking** and **Event Management** services.

### 6. Reset the System

To stop and remove all running services:

```
docker-compose down
```

To remove all containers, networks, volumes, and images:

```
docker-compose down --volumes --rmi all
```

----------

## 5. Testing Instructions

This system includes both **unit tests** and **integration tests** for all services to ensure functionality and reliability.

### 1. Running Unit Tests

#### For the **Booking** service:

```
cd Booking.Tests
dotnet test
```

#### For the **Event Management** service:

```
cd EventManagement.Tests/EventManagement.UnitTests
dotnet test
```

Wait for the execution to complete and check the results summary.

### 2. Running Integration Tests

Integration tests ensure that the services work correctly with external dependencies like databases and APIs.

#### For the **Booking** service:

```
cd Booking.IntegrationTests
dotnet test --filter Category=Integration
```

#### For the **Event Management** service:

```
cd EventManagement.Tests/EventManagement.IntegrationTests
dotnet test --filter Category=Integration
```

### 3. Test Coverage

To ensure high code coverage, run the following commands:

#### For the **Booking** service:

```
cd Booking.Tests
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

You can find the coverage report in the `coverage/` directory.

----------

## 6. CONTRIBUTING

Please refer to the [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on how to contribute to this project.

----------

## References

-   [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
-   [Elasticsearch Documentation](https://www.elastic.co/guide/en/elasticsearch/reference/current/index.html)
-   [Prometheus Documentation](https://prometheus.io/docs/introduction/overview/)
-   [Grafana Documentation](https://grafana.com/docs/)
-   [.NET Test Documentation](https://docs.microsoft.com/en-us/dotnet/core/testing/)