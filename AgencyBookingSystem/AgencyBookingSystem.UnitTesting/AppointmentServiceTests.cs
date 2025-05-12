using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

public class AppointmentServiceTests
{
    private readonly Mock<IAppointmentRepository> mockAppointmentRepository;
    private readonly Mock<IAppointmentSlotRepository> mockAppointmentSlotRepository;
    private readonly Mock<IAgencyService> mockAgencyService;
    private readonly Mock<IAgencyUserService> mockAgencyUserService;
    private readonly Mock<IEventDispatcher> mockEventDispatcher;
    private readonly Mock<ILogger<AppointmentService>> mockLogger;
    private readonly Mock<IProducer<Null, string>> mockKafkaProducer;
    private readonly string kafkaTopic = "appointments-topic";
    private readonly AppointmentService appointmentService;
    private Message<Null, string> lastKafkaMessage;

    public AppointmentServiceTests()
    {
        this.mockAppointmentRepository = new Mock<IAppointmentRepository>();
        this.mockAppointmentSlotRepository = new Mock<IAppointmentSlotRepository>();
        this.mockAgencyService = new Mock<IAgencyService>();
        this.mockAgencyUserService = new Mock<IAgencyUserService>();
        this.mockEventDispatcher = new Mock<IEventDispatcher>();
        this.mockLogger = new Mock<ILogger<AppointmentService>>();
        this.mockKafkaProducer = new Mock<IProducer<Null, string>>();

        this.appointmentService = new AppointmentService(
            this.mockAppointmentRepository.Object,
            this.mockAgencyUserService.Object,
            this.mockAgencyService.Object,
            this.mockEventDispatcher.Object,
            this.mockLogger.Object,
            this.mockKafkaProducer.Object,
            this.mockAppointmentSlotRepository.Object,
            this.kafkaTopic
        );
    }

    private void SetupKafkaProducerMock(string expectedAction)
    {
        this.mockKafkaProducer
            .Setup(producer => producer.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<Null, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<Null, string>, CancellationToken>((topic, message, token) => lastKafkaMessage = message)
            .ReturnsAsync(new DeliveryResult<Null, string>());
    }

    private void VerifyKafkaProducerMock(string domainAction, Times times)
    {
        // Map domain actions to expected Kafka operations
        var expectedKafkaAction = domainAction switch
        {
            "Created" => CommonModelConstants.KafkaOperation.Insert,
            "Rescheduled" => CommonModelConstants.KafkaOperation.Update,
            "NoShow" => CommonModelConstants.KafkaOperation.Update,
            "Cancelled" => CommonModelConstants.KafkaOperation.Update,
            _ => throw new ArgumentException($"Unsupported action: {domainAction}", nameof(domainAction))
        };

        this.mockKafkaProducer.Verify(
            producer => producer.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<Null, string>>(),
                It.IsAny<CancellationToken>()),
            times);

        // Additional assertion
        lastKafkaMessage.Should().NotBeNull();
        lastKafkaMessage.Value.Should().Contain(expectedKafkaAction);
    }

    private void VerifyRepositoryUpsert(Expression<Func<Appointment, bool>> match, Times times)
    {
        this.mockAppointmentRepository.Verify(
            repo => repo.UpsertAsync(It.Is<Appointment>(match), It.IsAny<CancellationToken>()),
            times);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnAppointment_WhenAppointmentExists()
    {
        // Arrange
        var appointmentId = Guid.Parse("067e9ab4-202b-4897-aa55-0c31412ea2ef");
        var agencyId = Guid.NewGuid();
        var agencyUserResult = AgencyUser.Create(agencyId, "user@test.com", "Test User", new[] { "Role1" });
        var agencyUser = agencyUserResult.Data;
        var expectedAppointment = new Appointment(
            appointmentId,
            agencyId,
            agencyUser.Id,
            "Test Appointment",
            DateTime.UtcNow.AddDays(1),
            AppointmentStatus.Pending,
            Guid.NewGuid().ToString("N"),
            agencyUser);

        this.mockAppointmentRepository
            .Setup(repo => repo.GetByIdAsync(appointmentId))
            .ReturnsAsync(expectedAppointment);

        // Act
        var result = await this.appointmentService.GetByIdAsync(appointmentId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(appointmentId);
        result.Name.Should().Be("Test Appointment");
        this.mockAppointmentRepository.Verify(repo => repo.GetByIdAsync(appointmentId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenAppointmentDoesNotExist()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();

        this.mockAppointmentRepository
            .Setup(repo => repo.GetByIdAsync(appointmentId))
            .ReturnsAsync((Appointment)null);

        // Act
        var result = await this.appointmentService.GetByIdAsync(appointmentId);

        // Assert
        Assert.Null(result);
        this.mockAppointmentRepository.Verify(repo => repo.GetByIdAsync(appointmentId), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllAppointments()
    {
        // Arrange
        var agencyId1 = Guid.NewGuid();
        var agencyUserResult1 = AgencyUser.Create(agencyId1, "user1@test.com", "User 1", new[] { "Role1" });
        var agencyUser1 = agencyUserResult1.Data;
        var appointment1Result = Appointment.Create(
            agencyId1,
            agencyUser1.Id,
            "Appointment 1",
            DateTime.UtcNow.AddDays(1),
            agencyUser1);

        var agencyId2 = Guid.NewGuid();
        var agencyUserResult2 = AgencyUser.Create(agencyId2, "user2@test.com", "User 2", new[] { "Role2" });
        var agencyUser2 = agencyUserResult2.Data;
        var appointment2Result = Appointment.Create(
            agencyId2,
            agencyUser2.Id,
            "Appointment 2",
            DateTime.UtcNow.AddDays(2),
            agencyUser2);

        var appointments = new List<Appointment>
        {
            appointment1Result.Data,
            appointment2Result.Data
        };

        this.mockAppointmentRepository
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(appointments);

        // Act
        var result = await this.appointmentService.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Appointment 1");
        result[1].Name.Should().Be("Appointment 2");
        this.mockAppointmentRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAppointmentsByAgencyAsync_ShouldReturnAgencyAppointments()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var agencyUserResult = AgencyUser.Create(agencyId, "user@test.com", "Test User", new[] { "Role1" });
        var agencyUser = agencyUserResult.Data;
        var appointmentResult = Appointment.Create(
            agencyId,
            agencyUser.Id,
            "Agency Appointment",
            DateTime.UtcNow.AddDays(1),
            agencyUser);

        var appointments = new List<Appointment>
        {
            appointmentResult.Data
        };

        this.mockAppointmentRepository
            .Setup(repo => repo.GetAppointmentsByAgencyAsync(agencyId))
            .ReturnsAsync(appointments);

        // Act
        var result = await this.appointmentService.GetAppointmentsByAgencyAsync(agencyId);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Agency Appointment");
        result[0].AgencyId.Should().Be(agencyId);
        this.mockAppointmentRepository.Verify(repo => repo.GetAppointmentsByAgencyAsync(agencyId), Times.Once);
    }

    [Fact]
    public async Task HasAvailableSlotAsync_ShouldReturnTrue_WhenSlotsAreAvailable()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var date = DateTime.Today;
        var agencyResult = Agency.Create("Test Agency", "test@agency.com", true, 5);
        var agency = agencyResult.Data;

        var slot = new AppointmentSlot(
            Guid.NewGuid(),
            agencyId,
            date,
            date.AddHours(1),
            5);

        this.mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        this.mockAppointmentSlotRepository
            .Setup(repo => repo.GetAvailableSlotAsync(agencyId, date))
            .ReturnsAsync(slot);

        this.mockAppointmentRepository
            .Setup(repo => repo.GetByDateAsync(date))
            .ReturnsAsync(new List<Appointment>());

        // Act
        var result = await this.appointmentService.HasAvailableSlotAsync(agencyId, date);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAvailableSlotAsync_ShouldReturnFalse_WhenNoSlotsAreAvailable()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var date = DateTime.Today;
        var agencyResult = Agency.Create("Test Agency", "test@agency.com", false, 1);
        var agency = agencyResult.Data;
        var agencyUserResult = AgencyUser.Create(agencyId, "user@test.com", "Test User", new[] { "Role1" });
        var agencyUser = agencyUserResult.Data;
        var appointmentResult = Appointment.Create(
            agencyId,
            agencyUser.Id,
            "Test Appointment",
            date,
            agencyUser);
        var appointments = new List<Appointment>
        {
            appointmentResult.Data
        };

        this.mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        this.mockAppointmentRepository
            .Setup(repo => repo.GetByDateAsync(date))
            .ReturnsAsync(appointments);

        // Act
        var result = await this.appointmentService.HasAvailableSlotAsync(agencyId, date);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleNoShowAsync_ShouldMarkAppointmentAsNoShow()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var agencyId = Guid.NewGuid();
        var agencyUserResult = AgencyUser.Create(agencyId, "user@test.com", "Test User", new[] { "Role1" });
        var agencyUser = agencyUserResult.Data;

        // Create appointment with Pending status
        var appointment = new Appointment(
            appointmentId,
            agencyId,
            agencyUser.Id,
            "Test Appointment",
            DateTime.UtcNow.AddDays(1),
            AppointmentStatus.Pending,
            Guid.NewGuid().ToString("N"),
            agencyUser);

        var agencyResult = Agency.Create("Test Agency", "test@agency.com", true, 5);
        var agency = agencyResult.Data;

        this.mockAppointmentRepository
            .Setup(repo => repo.GetByIdAsync(appointmentId))
            .ReturnsAsync(appointment);

        this.mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        this.mockAgencyUserService
            .Setup(service => service.GetByIdAsync(agencyUser.Id))
            .ReturnsAsync(agencyUser);

        SetupKafkaProducerMock("NoShow");

        // Act
        await this.appointmentService.HandleNoShowAsync(appointmentId);

        // Assert
        // Verify the appointment status was changed to NoShow
        VerifyRepositoryUpsert(
            a => a.Id == appointmentId && a.Status == AppointmentStatus.NoShow,
            Times.Once());

        // Verify Kafka event was published
        VerifyKafkaProducerMock("NoShow", Times.Once());
    }

    [Fact]
    public async Task GetNextAvailableDateAsync_ShouldReturnNextAvailableDate()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var preferredDate = DateTime.Today;
        var nextAvailableDate = preferredDate.AddDays(1);

        var agencyResult = Agency.Create("Test Agency", "test@agency.com", true, 5);
        var agency = agencyResult.Data;

        var slot = new AppointmentSlot(
            Guid.NewGuid(),
            agencyId,
            nextAvailableDate,
            nextAvailableDate.AddHours(1),
            5);

        this.mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        this.mockAppointmentSlotRepository
            .Setup(repo => repo.GetAvailableSlotAsync(agencyId, It.IsAny<DateTime>()))
            .ReturnsAsync((Guid _, DateTime date) => date == nextAvailableDate ? slot : null);

        this.mockAppointmentRepository
            .Setup(repo => repo.GetByDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Appointment>());

        // Act
        var result = await this.appointmentService.GetNextAvailableDateAsync(agencyId, preferredDate);

        // Assert
        result.Should().NotBeNull();
        result.Value.Date.Should().Be(nextAvailableDate.Date);
    }

    [Fact]
    public async Task RescheduleAppointmentAsync_ShouldSucceed_WhenValidParameters()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var agencyResult = Agency.Create("Test Agency", "test@agency.com", true, 5);
        var agency = agencyResult.Data;
        agency.Approve(); // Make sure agency is approved

        var agencyUserResult = AgencyUser.Create(agency.Id, "user@test.com", "Test User", new[] { "Role1" });
        var agencyUser = agencyUserResult.Data;

        var currentDate = DateTime.Today.AddHours(10); // Set specific time for business hours
        var newDate = DateTime.Today.AddDays(1).AddHours(10); // Set specific time for business hours

        // Create appointment with the specific ID we want to test with
        var appointment = new Appointment(
            appointmentId, // Use the specific ID we want to test with
            agency.Id,
            agencyUser.Id,
            "Test Appointment",
            currentDate,
            AppointmentStatus.Pending,
            Guid.NewGuid().ToString("N"),
            agencyUser);

        var currentSlot = new AppointmentSlot(
            Guid.NewGuid(),
            agency.Id,
            currentDate,
            currentDate.AddHours(1),
            4); // Start with 4 capacity since one is used

        var newSlot = new AppointmentSlot(
            Guid.NewGuid(),
            agency.Id,
            newDate,
            newDate.AddHours(1),
            5); // Full capacity available

        // Create some existing appointments for the new date
        var otherAppointment = new Appointment(
            Guid.NewGuid(), // Different ID
            agency.Id,
            Guid.NewGuid(), // Different user
            "Other Appointment",
            newDate,
            AppointmentStatus.Pending,
            Guid.NewGuid().ToString("N"),
            agencyUser);

        var existingAppointments = new List<Appointment> { otherAppointment };

        this.mockAppointmentRepository
            .Setup(repo => repo.GetByIdAsync(appointmentId))
            .ReturnsAsync(appointment);

        this.mockAgencyService
            .Setup(service => service.GetByIdAsync(agency.Id))
            .ReturnsAsync(agency);

        this.mockAgencyUserService
            .Setup(service => service.GetByIdAsync(agencyUser.Id))
            .ReturnsAsync(agencyUser);

        // Setup mock for getting appointments on the new date - match any date that has the same date component
        this.mockAppointmentRepository
            .Setup(repo => repo.GetByDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync((DateTime date) => existingAppointments.Where(a => a.Date.Date == date.Date).ToList());

        // Setup mock for getting available slot - this is crucial for the validation
        this.mockAppointmentSlotRepository
            .Setup(repo => repo.GetAvailableSlotAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
            .ReturnsAsync((Guid aid, DateTime date) => 
            {
                if (aid == agency.Id && date.Date == newDate.Date)
                    return newSlot;
                if (aid == agency.Id && date.Date == currentDate.Date)
                    return currentSlot;
                return null;
            });

        // Setup mock for getting slots by agency - handle both current and new dates
        this.mockAppointmentSlotRepository
            .Setup(repo => repo.GetSlotsByAgencyAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
            .ReturnsAsync((Guid aid, DateTime date) => 
            {
                // Ensure exact match on both agency ID and date
                if (aid == agency.Id && date.Date == date.Date)
                {
                    if (date.Date == currentDate.Date)
                        return new List<AppointmentSlot> { currentSlot };
                    if (date.Date == newDate.Date)
                        return new List<AppointmentSlot> { newSlot };
                }
                return new List<AppointmentSlot>();
            });

        SetupKafkaProducerMock("Rescheduled");

        // Act
        var result = await this.appointmentService.RescheduleAppointmentAsync(appointmentId, newDate);

        // Assert
        result.Succeeded.Should().BeTrue();
        
        // Verify appointment was updated with new date and status
        VerifyRepositoryUpsert(
            a => a.Date == newDate && a.Status == AppointmentStatus.Pending,
            Times.Once());
        
        // Verify old slot capacity was increased
        this.mockAppointmentSlotRepository.Verify(
            repo => repo.UpsertAsync(
                It.Is<AppointmentSlot>(s => s.StartTime == currentDate && s.Capacity == 5),
                It.IsAny<CancellationToken>()),
            Times.Once());
        
        // Verify new slot capacity was decreased
        this.mockAppointmentSlotRepository.Verify(
            repo => repo.UpsertAsync(
                It.Is<AppointmentSlot>(s => s.StartTime == newDate && s.Capacity == 4),
                It.IsAny<CancellationToken>()),
            Times.Once());
        
        // Verify Kafka event was published
        VerifyKafkaProducerMock("Rescheduled", Times.Once());
    }

    [Fact]
    public async Task RescheduleAppointmentAsync_ShouldFail_WhenAppointmentDoesNotExist()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var newDate = DateTime.Today.AddDays(3);

        this.mockAppointmentRepository
            .Setup(repo => repo.GetByIdAsync(appointmentId))
            .ReturnsAsync((Appointment)null);

        // Act
        var result = await this.appointmentService.RescheduleAppointmentAsync(appointmentId, newDate);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Appointment not found", result.Errors[0]);
        this.mockAppointmentRepository.Verify(
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

        var userResult = AgencyUser.Create(
            agencyId,
            "user@test.com",
            "Test User",
            new[] { "Role1" });
        var agencyUser = userResult.Data;

        var appointmentResult = Appointment.Create(
            agencyId,
            agencyUserId,
            "Test Appointment",
            DateTime.Today, agencyUser);
        var appointment = appointmentResult.Data;

        var agencyResult = Agency.Create("Test Agency", "agency@test.com", false, 5);
        var agency = agencyResult.Data;
        agency.AddHoliday(newDate, "National Holiday");

        this.mockAppointmentRepository
            .Setup(repo => repo.GetByIdAsync(appointmentId))
            .ReturnsAsync(appointment);

        this.mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        this.mockAgencyUserService
            .Setup(service => service.GetByIdAsync(agencyUserId))
            .ReturnsAsync(agencyUser);

        // Act
        var result = await this.appointmentService.RescheduleAppointmentAsync(appointmentId, newDate);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Selected date is a holiday"));
        this.mockAppointmentRepository.Verify(
            repo => repo.UpsertAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAppointmentAsync_ShouldCreateAppointment_WhenValidData()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var date = DateTime.Today.AddDays(1).AddHours(10); // Make sure it's during business hours
        var email = "user@test.com";
        var appointmentName = "Test Appointment";

        var agencyResult = Agency.Create("Test Agency", "test@agency.com", true, 5);
        var agency = agencyResult.Data;
        agency.Approve(); // Make sure agency is approved

        var agencyUserResult = AgencyUser.Create(agencyId, email, "Test User", new[] { "Role1" });
        var agencyUser = agencyUserResult.Data;

        var slot = new AppointmentSlot(
            Guid.NewGuid(),
            agencyId,
            date,
            date.AddHours(1),
            5);

        this.mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        this.mockAgencyUserService
            .Setup(service => service.GetByEmailAsync(email))
            .ReturnsAsync(agencyUser);

        this.mockAppointmentSlotRepository
            .Setup(repo => repo.GetSlotsByAgencyAsync(agencyId, date))
            .ReturnsAsync(new List<AppointmentSlot> { slot });

        this.mockAppointmentSlotRepository
            .Setup(repo => repo.GetAvailableSlotAsync(agencyId, date))
            .ReturnsAsync(slot);

        this.mockAppointmentRepository
            .Setup(repo => repo.GetByDateAsync(date))
            .ReturnsAsync(new List<Appointment>());

        SetupKafkaProducerMock("Created");

        // Act
        var result = await this.appointmentService.CreateAppointmentAsync(agencyId, email, appointmentName, date);

        // Assert
        result.Succeeded.Should().BeTrue();
        
        // Verify appointment was created with correct properties
        VerifyRepositoryUpsert(
            a => a.AgencyId == agencyId && 
                 a.Name == appointmentName && 
                 a.Date == date && 
                 a.Status == AppointmentStatus.Pending,
            Times.Once());

        // Verify slot capacity was decreased
        this.mockAppointmentSlotRepository.Verify(
            repo => repo.UpsertAsync(
                It.Is<AppointmentSlot>(s => s.StartTime == date && s.Capacity == 4),
                It.IsAny<CancellationToken>()),
            Times.Once());
        
        // Verify Kafka event was published
        VerifyKafkaProducerMock("Created", Times.Once());
    }

    [Fact]
    public async Task CancelAppointmentAsync_ShouldUpdateStatus_WhenValidAppointment()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var agencyId = Guid.NewGuid();

        var agencyResult = Agency.Create("Test Agency", "test@agency.com", true, 5);
        var agency = agencyResult.Data;

        var agencyUserResult = AgencyUser.Create(agencyId, "test@user.com", "Test User", new[] { "Role1" });
        var agencyUser = agencyUserResult.Data;

        var appointmentResult = Appointment.Create(
            agencyId,
            agencyUser.Id,
            "Test Appointment",
            DateTime.UtcNow.AddDays(1),
            agencyUser);
        var appointment = appointmentResult.Data;

        this.mockAppointmentRepository
            .Setup(repo => repo.GetByIdAsync(appointmentId))
            .ReturnsAsync(appointment);

        this.mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        this.mockAgencyUserService
            .Setup(service => service.GetByIdAsync(agencyUser.Id))
            .ReturnsAsync(agencyUser);

        SetupKafkaProducerMock("Cancelled");

        // Act
        await this.appointmentService.CancelAppointmentAsync(appointmentId);

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        VerifyRepositoryUpsert(
            a => a.Status == AppointmentStatus.Cancelled,
            Times.Once());
        VerifyKafkaProducerMock("Cancelled", Times.Once());
    }

    [Fact]
    public async Task IsBookingAllowedAsync_ShouldReturnTrue_WhenAgencyIsApproved()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var currentDate = DateTime.Today;
        var agencyResult = Agency.Create("Test Agency", "test@agency.com", true, 5);
        var agency = agencyResult.Data;
        agency.Approve(); // Make sure agency is approved

        var slot = new AppointmentSlot(
            Guid.NewGuid(),
            agencyId,
            currentDate.AddHours(10),
            currentDate.AddHours(11),
            5); // Has capacity

        this.mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        // Setup mock for available slot
        this.mockAppointmentSlotRepository
            .Setup(repo => repo.GetAvailableSlotAsync(agencyId, It.IsAny<DateTime>()))
            .ReturnsAsync(slot);

        // Setup mock for existing appointments (empty list = no appointments yet)
        this.mockAppointmentRepository
            .Setup(repo => repo.GetByDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Appointment>());

        // Act
        var result = await this.appointmentService.IsBookingAllowedAsync(agencyId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsBookingAllowedAsync_ShouldReturnFalse_WhenAgencyIsNotApproved()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var agencyResult = Agency.Create("Test Agency", "test@agency.com", false, 5);
        var agency = agencyResult.Data;

        this.mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        // Act
        var result = await this.appointmentService.IsBookingAllowedAsync(agencyId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAppointmentsByDateAsync_ShouldReturnAppointmentsForDate()
    {
        // Arrange
        var date = DateTime.Today;
        var agencyId = Guid.NewGuid();
        var agencyUserId = Guid.NewGuid();
        var agencyUserResult = AgencyUser.Create(agencyId, "user@test.com", "Test User", new[] { "Role1" });
        var agencyUser = agencyUserResult.Data;

        var appointmentResult = Appointment.Create(
            agencyId,
            agencyUserId,
            "Test Appointment",
            date, agencyUser);
        var appointments = new List<Appointment>
        {
            appointmentResult.Data
        };

        var agencyResult = Agency.Create("Test Agency", "test@agency.com", false, 10);
        var agency = agencyResult.Data;

        this.mockAppointmentRepository
            .Setup(repo => repo.GetByDateAsync(date))
            .ReturnsAsync(appointments);

        this.mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        this.mockAgencyUserService
            .Setup(service => service.GetByIdAsync(agencyUserId))
            .ReturnsAsync(agencyUser);

        // Act
        var result = await this.appointmentService.GetAppointmentsByDateAsync(date);

        // Assert
        result.Should().HaveCount(1);
        result[0].AppointmentName.Should().Be("Test Appointment");
        result[0].AgencyName.Should().Be("Test Agency");
        result[0].AgencyEmail.Should().Be("test@agency.com");
        result[0].UserEmail.Should().Be("user@test.com");
        result[0].Date.Should().Be(date);
        result[0].Status.Should().Be(AppointmentStatus.Pending);
    }

    [Fact]
    public async Task GetAppointmentsByDateForUserAsync_ShouldReturnUserAppointmentsForDate()
    {
        // Arrange
        var date = DateTime.Today;
        var agencyId = Guid.NewGuid();
        var agencyUserId = Guid.NewGuid();
        var userEmail = "user@test.com";
        var agencyUserResult = AgencyUser.Create(agencyId, userEmail, "Test User", new[] { "Role1" });
        var agencyUser = agencyUserResult.Data;

        var appointmentResult = Appointment.Create(
            agencyId,
            agencyUserId,
            "User Test Appointment",
            date, agencyUser);
        var appointments = new List<Appointment>
        {
            appointmentResult.Data
        };

        var agencyResult = Agency.Create("Test Agency", "agency@test.com", false, 10);
        var agency = agencyResult.Data;

        this.mockAppointmentRepository
            .Setup(repo => repo.GetByDateAndUserAsync(date, userEmail))
            .ReturnsAsync(appointments);

        this.mockAgencyService
            .Setup(service => service.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        this.mockAgencyUserService
            .Setup(service => service.GetByIdAsync(agencyUserId))
            .ReturnsAsync(agencyUser);

        // Act
        var result = await this.appointmentService.GetAppointmentsByDateForUserAsync(date, userEmail);

        // Assert
        result.Should().HaveCount(1);
        result[0].AppointmentName.Should().Be("User Test Appointment");
        result[0].AgencyName.Should().Be("Test Agency");
        result[0].AgencyEmail.Should().Be("agency@test.com");
        result[0].UserEmail.Should().Be(userEmail);
        result[0].Date.Should().Be(date);
        result[0].Status.Should().Be(AppointmentStatus.Pending);
    }
}
