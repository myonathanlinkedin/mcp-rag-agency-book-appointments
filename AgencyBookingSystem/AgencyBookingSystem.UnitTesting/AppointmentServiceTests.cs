using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using static ApplicationSettings;

public class AppointmentServiceTests
{
    private readonly Mock<IAppointmentUnitOfWork> mockUnitOfWork;
    private readonly Mock<IEventDispatcher> mockEventDispatcher;
    private readonly Mock<ILogger<AppointmentService>> mockLogger;
    private readonly Mock<IProducer<Null, string>> mockKafkaProducer;
    private readonly ApplicationSettings appSettings;
    private readonly AppointmentService appointmentService;
    private Message<Null, string> lastKafkaMessage;

    public AppointmentServiceTests()
    {
        this.mockUnitOfWork = new Mock<IAppointmentUnitOfWork>();
        this.mockEventDispatcher = new Mock<IEventDispatcher>();
        this.mockLogger = new Mock<ILogger<AppointmentService>>();
        this.mockKafkaProducer = new Mock<IProducer<Null, string>>();
        this.appSettings = new ApplicationSettings
        {
            Kafka = new KafkaSettings
            {
                Topic = "test-topic",
                BootstrapServers = "localhost:9092"
            }
        };

        this.mockKafkaProducer
            .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<Null, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, Message<Null, string>, CancellationToken>((topic, message, token) => 
            {
                this.lastKafkaMessage = message;
            })
            .Returns(Task.FromResult(new DeliveryResult<Null, string>()));

        this.appointmentService = new AppointmentService(
            this.mockUnitOfWork.Object,
            this.mockEventDispatcher.Object,
            this.mockLogger.Object,
            this.mockKafkaProducer.Object,
            this.appSettings
        );

        this.mockUnitOfWork
            .Setup(uow => uow.Appointments.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
            
        this.mockUnitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
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

    [Fact]
    public async Task GetByIdAsync_ShouldReturnAppointment_WhenAppointmentExists()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var agencyId = Guid.NewGuid();
        var agencyUserResult = AgencyUser.Create(agencyId, "test@example.com", "Test User", new[] { "Customer" });
        var agencyUser = agencyUserResult.Data;
        var appointmentResult = Appointment.Create(
            agencyId,
            agencyUser.Id,
            "Test Appointment",
            DateTime.UtcNow.AddDays(1),
            agencyUser);
        var expectedAppointment = appointmentResult.Data;
        
        this.mockUnitOfWork
            .Setup(uow => uow.Appointments.GetByIdAsync(appointmentId))
            .Returns(Task.FromResult(expectedAppointment));
        
        // Act
        var result = await this.appointmentService.GetByIdAsync(appointmentId);
        
        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Appointment");
        this.mockUnitOfWork.Verify(uow => uow.Appointments.GetByIdAsync(appointmentId), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllAppointments()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var agencyUserResult = AgencyUser.Create(agencyId, "test@example.com", "Test User", new[] { "Customer" });
        var agencyUser = agencyUserResult.Data;
        
        var appointment1Result = Appointment.Create(
            agencyId,
            agencyUser.Id,
            "Appointment 1",
            DateTime.UtcNow.AddDays(1),
            agencyUser);
        var appointment2Result = Appointment.Create(
            agencyId,
            agencyUser.Id,
            "Appointment 2",
            DateTime.UtcNow.AddDays(2),
            agencyUser);
        
        var appointments = new List<Appointment>
        {
            appointment1Result.Data,
            appointment2Result.Data
        };
        
        this.mockUnitOfWork
            .Setup(uow => uow.Appointments.GetAllAsync())
            .Returns(Task.FromResult(appointments));
        
        // Act
        var result = await this.appointmentService.GetAllAsync();
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Appointment 1");
        result[1].Name.Should().Be("Appointment 2");
        this.mockUnitOfWork.Verify(uow => uow.Appointments.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAppointmentsByAgencyAsync_ShouldReturnAgencyAppointments()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var agencyUserResult = AgencyUser.Create(agencyId, "test@example.com", "Test User", new[] { "Customer" });
        var agencyUser = agencyUserResult.Data;
        
        var appointment1Result = Appointment.Create(
            agencyId,
            agencyUser.Id,
            "Appointment 1",
            DateTime.UtcNow.AddDays(1),
            agencyUser);
        var appointment2Result = Appointment.Create(
            agencyId,
            agencyUser.Id,
            "Appointment 2",
            DateTime.UtcNow.AddDays(2),
            agencyUser);
        
        var appointments = new List<Appointment>
        {
            appointment1Result.Data,
            appointment2Result.Data
        };
        
        this.mockUnitOfWork
            .Setup(uow => uow.Appointments.GetAppointmentsByAgencyAsync(agencyId))
            .Returns(Task.FromResult(appointments));
        
        // Act
        var result = await this.appointmentService.GetAppointmentsByAgencyAsync(agencyId);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Appointment 1");
        result[1].Name.Should().Be("Appointment 2");
        this.mockUnitOfWork.Verify(uow => uow.Appointments.GetAppointmentsByAgencyAsync(agencyId), Times.Once);
    }

    [Fact]
    public async Task CreateAppointmentAsync_ShouldCreateAppointment_WhenValid()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var name = "Test Appointment";
        var appointmentDate = DateTime.UtcNow.AddDays(1).Date.AddHours(9); // Set to 9 AM
        var agencyUser = new AgencyUser(userId, agencyId, email, "Test User");
        var agency = new Agency(agencyId, "Test Agency", "agency@test.com", false, 10);
        var appointment = new Appointment(Guid.NewGuid(), agencyId, userId, name, appointmentDate, AppointmentStatus.Initiated, Guid.NewGuid().ToString("N"), agencyUser);
        var appointmentSlot = new AppointmentSlot(Guid.NewGuid(), agencyId, appointmentDate, appointmentDate.AddHours(1), 5);

        // Setup mocks for validation
        mockUnitOfWork.Setup(uow => uow.AgencyUsers.GetByEmailAsync(email))
            .ReturnsAsync(agencyUser);

        mockUnitOfWork.Setup(uow => uow.Agencies.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        mockUnitOfWork.Setup(uow => uow.AppointmentSlots.GetSlotsByAgencyAsync(agencyId, appointmentDate))
            .ReturnsAsync(new List<AppointmentSlot> { appointmentSlot });

        mockUnitOfWork.Setup(uow => uow.AppointmentSlots.GetAvailableSlotAsync(agencyId, appointmentDate))
            .ReturnsAsync(appointmentSlot);

        mockUnitOfWork.Setup(uow => uow.Appointments.GetByDateAndUserAsync(appointmentDate, email))
            .ReturnsAsync(new List<Appointment>());

        mockUnitOfWork.Setup(uow => uow.Appointments.GetByDateAsync(appointmentDate))
            .Returns(Task.FromResult(new List<Appointment>()));

        mockUnitOfWork.Setup(uow => uow.Appointments.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(appointment));

        mockUnitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        mockKafkaProducer.Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<Null, string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new DeliveryResult<Null, string>()));

        // Act
        var result = await appointmentService.CreateAppointmentAsync(agencyId, email, name, appointmentDate);

        // Debug output for errors
        if (!result.Succeeded && result.Errors != null)
        {
            System.Console.WriteLine("Test Failure Errors: " + string.Join(", ", result.Errors));
        }

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        mockUnitOfWork.Verify(uow => uow.Appointments.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Once);
        mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockKafkaProducer.Verify(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<Null, string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
