using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Moq;

public class AppointmentServiceTests
{
    private readonly Mock<IAppointmentRepository> mockAppointmentRepository;
    private readonly Mock<IAppointmentSlotRepository> mockAppointmentSlotRepository;
    private readonly Mock<IAgencyService> mockAgencyService;
    private readonly Mock<IAgencyUserService> mockAgencyUserService;
    private readonly Mock<IEventDispatcher> mockEventDispatcher;
    private readonly Mock<ILogger<AppointmentService>> mockLogger;
    private readonly Mock<IProducer<Null, string>> mockKafkaProducer;
    private readonly string _kafkaTopic = "appointments-topic";
    private readonly AppointmentService appointmentService;

    public AppointmentServiceTests()
    {
        mockAppointmentRepository = new Mock<IAppointmentRepository>();
        mockAppointmentSlotRepository = new Mock<IAppointmentSlotRepository>();
        mockAgencyService = new Mock<IAgencyService>();
        mockAgencyUserService = new Mock<IAgencyUserService>();
        mockEventDispatcher = new Mock<IEventDispatcher>();
        mockLogger = new Mock<ILogger<AppointmentService>>();
        mockKafkaProducer = new Mock<IProducer<Null, string>>();

        appointmentService = new AppointmentService(
            mockAppointmentRepository.Object,
            mockAgencyUserService.Object,
            mockAgencyService.Object,
            mockEventDispatcher.Object,
            mockLogger.Object,
            mockKafkaProducer.Object,
            mockAppointmentSlotRepository.Object,
            _kafkaTopic
        );
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnAppointment_WhenAppointmentExists()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var expectedAppointment = new Appointment { Id = appointmentId, Name = "Test Appointment" };

        mockAppointmentRepository
            .Setup(repo => repo.GetByIdAsync(appointmentId))
            .ReturnsAsync(expectedAppointment);

        // Act
        var result = await appointmentService.GetByIdAsync(appointmentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(appointmentId, result.Id);
        Assert.Equal("Test Appointment", result.Name);
        mockAppointmentRepository.Verify(repo => repo.GetByIdAsync(appointmentId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenAppointmentDoesNotExist()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();

        mockAppointmentRepository
            .Setup(repo => repo.GetByIdAsync(appointmentId))
            .ReturnsAsync((Appointment)null);

        // Act
        var result = await appointmentService.GetByIdAsync(appointmentId);

        // Assert
        Assert.Null(result);
        mockAppointmentRepository.Verify(repo => repo.GetByIdAsync(appointmentId), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllAppointments()
    {
        // Arrange
        var appointments = new List<Appointment>
        {
            new Appointment { Id = Guid.NewGuid(), Name = "Appointment 1" },
            new Appointment { Id = Guid.NewGuid(), Name = "Appointment 2" }
        };

        mockAppointmentRepository
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(appointments);

        // Act
        var result = await appointmentService.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Appointment 1", result[0].Name);
        Assert.Equal("Appointment 2", result[1].Name);
        mockAppointmentRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAppointmentsByAgencyAsync_ShouldReturnAgencyAppointments()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var appointments = new List<Appointment>
        {
            new Appointment { Id = Guid.NewGuid(), AgencyId = agencyId, Name = "Agency Appointment" }
        };

        mockAppointmentRepository
            .Setup(repo => repo.GetAppointmentsByAgencyAsync(agencyId))
            .ReturnsAsync(appointments);

        // Act
        var result = await appointmentService.GetAppointmentsByAgencyAsync(agencyId);

        // Assert
        Assert.Single(result);
        Assert.Equal("Agency Appointment", result[0].Name);
        Assert.Equal(agencyId, result[0].AgencyId);
        mockAppointmentRepository.Verify(repo => repo.GetAppointmentsByAgencyAsync(agencyId), Times.Once);
    }

    [Fact]
    public async Task HasAvailableSlotAsync_ShouldReturnTrue_WhenSlotsAreAvailable()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var date = DateTime.Today;
        var agency = new Agency { Id = agencyId, MaxAppointmentsPerDay = 5 };
        var appointments = new List<Appointment>
        {
            new Appointment { Id = Guid.NewGuid(), AgencyId = agencyId, Date = date }
        };

        mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        mockAppointmentRepository
            .Setup(repo => repo.GetAppointmentsByAgencyAsync(agencyId))
            .ReturnsAsync(appointments);

        // Act
        var result = await appointmentService.HasAvailableSlotAsync(agencyId, date);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasAvailableSlotAsync_ShouldReturnFalse_WhenNoSlotsAreAvailable()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var date = DateTime.Today;
        var agency = new Agency { Id = agencyId, MaxAppointmentsPerDay = 1 };
        var appointments = new List<Appointment>
        {
            new Appointment { Id = Guid.NewGuid(), AgencyId = agencyId, Date = date }
        };

        mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        mockAppointmentRepository
            .Setup(repo => repo.GetAppointmentsByAgencyAsync(agencyId))
            .ReturnsAsync(appointments);

        // Act
        var result = await appointmentService.HasAvailableSlotAsync(agencyId, date);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HandleNoShowAsync_ShouldUpdateAppointmentStatus()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var appointment = new Appointment
        {
            Id = appointmentId,
            Status = AppointmentStatus.Pending
        };

        mockAppointmentRepository
            .Setup(repo => repo.GetByIdAsync(appointmentId))
            .ReturnsAsync(appointment);

        // Act
        await appointmentService.HandleNoShowAsync(appointmentId);

        // Assert
        Assert.Equal(AppointmentStatus.Expired, appointment.Status);
        mockAppointmentRepository.Verify(
            repo => repo.UpsertAsync(It.Is<Appointment>(a => a.Id == appointmentId && a.Status == AppointmentStatus.Expired),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetNextAvailableDateAsync_ShouldReturnNextAvailableDate()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var preferredDate = DateTime.Today;
        var nextAvailableDate = preferredDate.AddDays(1);
        var agency = new Agency { Id = agencyId, MaxAppointmentsPerDay = 1 };

        // Day 1 - No slots available
        var appointmentsDay1 = new List<Appointment>
        {
            new Appointment { Id = Guid.NewGuid(), AgencyId = agencyId, Date = preferredDate }
        };

        // Day 2 - Slots available
        var appointmentsDay2 = new List<Appointment>();

        mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        mockAppointmentRepository
            .Setup(repo => repo.GetAppointmentsByAgencyAsync(agencyId))
            .ReturnsAsync(appointmentsDay1) // First day is full
            .Callback(() => {
                // Change the setup for the second call to simulate available slots on next day
                mockAppointmentRepository
                    .Setup(repo => repo.GetAppointmentsByAgencyAsync(agencyId))
                    .ReturnsAsync(appointmentsDay2);
            });

        // Act
        var result = await appointmentService.GetNextAvailableDateAsync(agencyId, preferredDate);

        // Assert
        Assert.Equal(nextAvailableDate.Date, result?.Date);
    }

    [Fact]
    public async Task RescheduleAppointmentAsync_ShouldSucceed_WhenValidParameters()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var agencyId = Guid.NewGuid();
        var agencyUserId = Guid.NewGuid();
        var newDate = DateTime.Today.AddDays(3);

        var appointment = new Appointment
        {
            Id = appointmentId,
            AgencyId = agencyId,
            AgencyUserId = agencyUserId,
            Name = "Test Appointment",
            Date = DateTime.Today,
            Status = AppointmentStatus.Pending
        };

        var agency = new Agency
        {
            Id = agencyId,
            Name = "Test Agency",
            Email = "agency@test.com",
            MaxAppointmentsPerDay = 5,
            Holidays = new List<Holiday>(),
            Slots = new List<AppointmentSlot>
            {
                new AppointmentSlot
                {
                    StartTime = newDate,
                    Capacity = 3
                }
            }
        };

        var agencyUser = new AgencyUser
        {
            Id = agencyUserId,
            Email = "user@test.com"
        };

        mockAppointmentRepository
            .Setup(repo => repo.GetByIdAsync(appointmentId))
            .ReturnsAsync(appointment);

        mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        mockAgencyUserService
            .Setup(service => service.GetByIdAsync(agencyUserId))
            .ReturnsAsync(agencyUser);

        mockKafkaProducer
            .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<Null, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<Null, string>());

        // Act
        var result = await appointmentService.RescheduleAppointmentAsync(appointmentId, newDate);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(newDate, appointment.Date);
        mockAppointmentRepository.Verify(
            repo => repo.UpsertAsync(It.Is<Appointment>(a => a.Id == appointmentId && a.Date == newDate),
            It.IsAny<CancellationToken>()),
            Times.Once);
        mockEventDispatcher.Verify(
            dispatcher => dispatcher.Dispatch(It.IsAny<AppointmentEvent>()),
            Times.Once);
        mockKafkaProducer.Verify(
            producer => producer.ProduceAsync(
                It.Is<string>(topic => topic == _kafkaTopic),
                It.IsAny<Message<Null, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RescheduleAppointmentAsync_ShouldFail_WhenAppointmentDoesNotExist()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var newDate = DateTime.Today.AddDays(3);

        mockAppointmentRepository
            .Setup(repo => repo.GetByIdAsync(appointmentId))
            .ReturnsAsync((Appointment)null);

        // Act
        var result = await appointmentService.RescheduleAppointmentAsync(appointmentId, newDate);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Appointment does not exist", result.Errors[0]);
        mockAppointmentRepository.Verify(
            repo => repo.UpsertAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RescheduleAppointmentAsync_ShouldFail_WhenNewDateIsHoliday()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var agencyId = Guid.NewGuid();
        var agencyUserId = Guid.NewGuid();
        var newDate = DateTime.Today.AddDays(3);

        var appointment = new Appointment
        {
            Id = appointmentId,
            AgencyId = agencyId,
            AgencyUserId = agencyUserId,
            Name = "Test Appointment",
            Date = DateTime.Today,
            Status = AppointmentStatus.Pending
        };

        var agency = new Agency
        {
            Id = agencyId,
            Name = "Test Agency",
            Email = "agency@test.com",
            MaxAppointmentsPerDay = 5,
            Holidays = new List<Holiday>
            {
                new Holiday { Date = newDate, Reason = "National Holiday" }
            },
            Slots = new List<AppointmentSlot>()
        };

        var agencyUser = new AgencyUser
        {
            Id = agencyUserId,
            Email = "user@test.com"
        };

        mockAppointmentRepository
            .Setup(repo => repo.GetByIdAsync(appointmentId))
            .ReturnsAsync(appointment);

        mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        mockAgencyUserService
            .Setup(service => service.GetByIdAsync(agencyUserId))
            .ReturnsAsync(agencyUser);

        // Act
        var result = await appointmentService.RescheduleAppointmentAsync(appointmentId, newDate);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Selected date is a holiday", result.Errors[0]);
        mockAppointmentRepository.Verify(
            repo => repo.UpsertAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAppointmentAsync_ShouldSucceed_WhenValidParameters()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var agencyUserId = Guid.NewGuid();
        var email = "user@test.com";
        var appointmentName = "Test Appointment";
        var date = DateTime.Today.AddDays(1);

        var agency = new Agency
        {
            Id = agencyId,
            Name = "Test Agency",
            Email = "agency@test.com",
            IsApproved = true,
            MaxAppointmentsPerDay = 5,
            Holidays = new List<Holiday>(),
            Slots = new List<AppointmentSlot>
            {
                new AppointmentSlot
                {
                    StartTime = date,
                    Capacity = 3
                }
            }
        };

        var agencyUser = new AgencyUser
        {
            Id = agencyUserId,
            Email = email
        };

        mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        mockAgencyUserService
            .Setup(service => service.GetByEmailAsync(email))
            .ReturnsAsync(agencyUser);

        mockKafkaProducer
            .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<Null, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<Null, string>());

        // Act
        var result = await appointmentService.CreateAppointmentAsync(agencyId, email, appointmentName, date);

        // Assert
        Assert.True(result.Succeeded);
        mockAppointmentRepository.Verify(
            repo => repo.UpsertAsync(
                It.Is<Appointment>(a =>
                    a.AgencyId == agencyId &&
                    a.AgencyUserId == agencyUserId &&
                    a.Name == appointmentName &&
                    a.Date == date &&
                    a.Status == AppointmentStatus.Pending),
                It.IsAny<CancellationToken>()),
            Times.Once);
        mockAppointmentSlotRepository.Verify(
            repo => repo.UpsertAsync(
                It.Is<AppointmentSlot>(s => s.StartTime == date && s.Capacity == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
        mockEventDispatcher.Verify(
            dispatcher => dispatcher.Dispatch(It.IsAny<AppointmentEvent>()),
            Times.Once);
        mockKafkaProducer.Verify(
            producer => producer.ProduceAsync(
                It.Is<string>(topic => topic == _kafkaTopic),
                It.IsAny<Message<Null, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAppointmentAsync_ShouldFail_WhenDateIsHoliday()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var agencyUserId = Guid.NewGuid();
        var email = "user@test.com";
        var appointmentName = "Test Appointment";
        var date = DateTime.Today.AddDays(1);

        var agency = new Agency
        {
            Id = agencyId,
            Name = "Test Agency",
            Email = "agency@test.com",
            IsApproved = true,
            MaxAppointmentsPerDay = 5,
            Holidays = new List<Holiday>
            {
                new Holiday { Date = date, Reason = "Public Holiday" }
            },
            Slots = new List<AppointmentSlot>()
        };

        var agencyUser = new AgencyUser
        {
            Id = agencyUserId,
            Email = email
        };

        mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        mockAgencyUserService
            .Setup(service => service.GetByEmailAsync(email))
            .ReturnsAsync(agencyUser);

        // Act
        var result = await appointmentService.CreateAppointmentAsync(agencyId, email, appointmentName, date);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Selected date is a holiday", result.Errors[0]);
        mockAppointmentRepository.Verify(
            repo => repo.UpsertAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelAppointmentAsync_ShouldUpdateStatus()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var agencyId = Guid.NewGuid();
        var agencyUserId = Guid.NewGuid();

        var appointment = new Appointment
        {
            Id = appointmentId,
            AgencyId = agencyId,
            AgencyUserId = agencyUserId,
            Name = "Test Appointment",
            Date = DateTime.Today,
            Status = AppointmentStatus.Pending
        };

        var agency = new Agency
        {
            Id = agencyId,
            Name = "Test Agency",
            Email = "agency@test.com"
        };

        var agencyUser = new AgencyUser
        {
            Id = agencyUserId,
            Email = "user@test.com"
        };

        mockAppointmentRepository
            .Setup(repo => repo.GetByIdAsync(appointmentId))
            .ReturnsAsync(appointment);

        mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        mockAgencyUserService
            .Setup(service => service.GetByIdAsync(agencyUserId))
            .ReturnsAsync(agencyUser);

        mockKafkaProducer
            .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<Null, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<Null, string>());

        // Act
        await appointmentService.CancelAppointmentAsync(appointmentId);

        // Assert
        Assert.Equal("Canceled", appointment.Status);
        mockAppointmentRepository.Verify(
            repo => repo.UpsertAsync(It.Is<Appointment>(a => a.Id == appointmentId && a.Status == "Canceled"),
            It.IsAny<CancellationToken>()),
            Times.Once);
        mockEventDispatcher.Verify(
            dispatcher => dispatcher.Dispatch(It.IsAny<AppointmentEvent>()),
            Times.Once);
        mockKafkaProducer.Verify(
            producer => producer.ProduceAsync(
                It.Is<string>(topic => topic == _kafkaTopic),
                It.IsAny<Message<Null, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IsBookingAllowedAsync_ShouldReturnTrue_WhenAgencyIsApproved()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var agency = new Agency
        {
            Id = agencyId,
            IsApproved = true,
            MaxAppointmentsPerDay = 5
        };

        var appointments = new List<Appointment>
        {
            new Appointment { AgencyId = agencyId, Date = DateTime.UtcNow }
        };

        mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        mockAppointmentRepository
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(appointments);

        // Act
        var result = await appointmentService.IsBookingAllowedAsync(agencyId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsBookingAllowedAsync_ShouldReturnFalse_WhenAgencyIsNotApproved()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var agency = new Agency
        {
            Id = agencyId,
            IsApproved = false,
            MaxAppointmentsPerDay = 5
        };

        mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        // Act
        var result = await appointmentService.IsBookingAllowedAsync(agencyId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAppointmentsByDateAsync_ShouldReturnAppointmentsForDate()
    {
        // Arrange
        var date = DateTime.Today;
        var agencyId = Guid.NewGuid();
        var agencyUserId = Guid.NewGuid();

        var appointments = new List<Appointment>
        {
            new Appointment
            {
                Id = Guid.NewGuid(),
                AgencyId = agencyId,
                AgencyUserId = agencyUserId,
                Name = "Test Appointment",
                Date = date,
                Status = AppointmentStatus.Pending
            }
        };

        var agency = new Agency
        {
            Id = agencyId,
            Name = "Test Agency",
            Email = "agency@test.com"
        };

        var agencyUser = new AgencyUser
        {
            Id = agencyUserId,
            Email = "user@test.com"
        };

        mockAppointmentRepository
            .Setup(repo => repo.GetByDateAsync(date))
            .ReturnsAsync(appointments);

        mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        mockAgencyUserService
            .Setup(service => service.GetByIdAsync(agencyUserId))
            .ReturnsAsync(agencyUser);

        // Act
        var result = await appointmentService.GetAppointmentsByDateAsync(date);

        // Assert
        Assert.Single(result);
        Assert.Equal("Test Appointment", result[0].AppointmentName);
        Assert.Equal("Test Agency", result[0].AgencyName);
        Assert.Equal("user@test.com", result[0].UserEmail);
    }

    [Fact]
    public async Task GetAppointmentsByDateForUserAsync_ShouldReturnUserAppointmentsForDate()
    {
        // Arrange
        var date = DateTime.Today;
        var agencyId = Guid.NewGuid();
        var agencyUserId = Guid.NewGuid();
        var userEmail = "user@test.com";

        var appointments = new List<Appointment>
        {
            new Appointment
            {
                Id = Guid.NewGuid(),
                AgencyId = agencyId,
                AgencyUserId = agencyUserId,
                Name = "User Test Appointment",
                Date = date,
                Status = AppointmentStatus.Pending
            }
        };

        var agency = new Agency
        {
            Id = agencyId,
            Name = "Test Agency",
            Email = "agency@test.com"
        };

        var agencyUser = new AgencyUser
        {
            Id = agencyUserId,
            Email = userEmail
        };

        mockAppointmentRepository
            .Setup(repo => repo.GetByDateAndUserAsync(date, userEmail))
            .ReturnsAsync(appointments);

        mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        mockAgencyUserService
            .Setup(service => service.GetByIdAsync(agencyUserId))
            .ReturnsAsync(agencyUser);

        // Act
        var result = await appointmentService.GetAppointmentsByDateForUserAsync(date, userEmail);

        // Assert
        Assert.Single(result);
        Assert.Equal("User Test Appointment", result[0].AppointmentName);
        Assert.Equal("Test Agency", result[0].AgencyName);
        Assert.Equal(userEmail, result[0].UserEmail);
    }
}