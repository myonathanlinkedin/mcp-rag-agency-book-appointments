using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

public class AgencyServiceTests
{
    private readonly Mock<IAgencyRepository> mockAgencyRepository;
    private readonly Mock<IAgencyUserRepository> mockAgencyUserRepository;
    private readonly Mock<IAppointmentSlotRepository> mockAppointmentSlotRepository;
    private readonly Mock<IEventDispatcher> mockEventDispatcher;
    private readonly Mock<ILogger<AgencyService>> mockLogger;
    private readonly AgencyService agencyService;

    public AgencyServiceTests()
    {
        this.mockAgencyRepository = new Mock<IAgencyRepository>();
        this.mockAgencyUserRepository = new Mock<IAgencyUserRepository>();
        this.mockAppointmentSlotRepository = new Mock<IAppointmentSlotRepository>();
        this.mockEventDispatcher = new Mock<IEventDispatcher>();
        this.mockLogger = new Mock<ILogger<AgencyService>>();
        
        this.agencyService = new AgencyService(
            this.mockAgencyRepository.Object,
            this.mockAgencyUserRepository.Object,
            this.mockAppointmentSlotRepository.Object,
            this.mockEventDispatcher.Object,
            this.mockLogger.Object
        );
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnAgency_WhenAgencyExists()
    {
        // Arrange
        var agencyId = new Guid("c0711eb5-68da-4a7e-9972-5665edae2d3e");
        var expectedAgency = new Agency(
            agencyId,
            "Test Agency",
            "test@agency.com",
            false,
            10);
        
        this.mockAgencyRepository
            .Setup(repo => repo.GetByIdAsync(agencyId))
            .ReturnsAsync(expectedAgency);
        
        // Act
        var result = await this.agencyService.GetByIdAsync(agencyId);
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(agencyId);
        result.Name.Should().Be("Test Agency");
        this.mockAgencyRepository.Verify(repo => repo.GetByIdAsync(agencyId), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllAgencies()
    {
        // Arrange
        var agency1Result = Agency.Create("Agency 1", "agency1@test.com", false, 10);
        var agency2Result = Agency.Create("Agency 2", "agency2@test.com", false, 10);
        
        var agencies = new List<Agency>
        {
            agency1Result.Data,
            agency2Result.Data
        };
        
        this.mockAgencyRepository
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(agencies);
        
        // Act
        var result = await this.agencyService.GetAllAsync();
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Agency 1");
        result[1].Name.Should().Be("Agency 2");
        this.mockAgencyRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAgenciesWithUsersAsync_ShouldReturnAgenciesWithUsers()
    {
        // Arrange
        var agency1Result = Agency.Create("Agency 1", "agency1@test.com", false, 10);
        var agency2Result = Agency.Create("Agency 2", "agency2@test.com", false, 10);
        
        var agencies = new List<Agency>
        {
            agency1Result.Data,
            agency2Result.Data
        };
        
        this.mockAgencyRepository
            .Setup(repo => repo.GetAgenciesWithUsersAsync())
            .ReturnsAsync(agencies);
        
        // Act
        var result = await this.agencyService.GetAgenciesWithUsersAsync();
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        this.mockAgencyRepository.Verify(repo => repo.GetAgenciesWithUsersAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnAgency_WhenAgencyExists()
    {
        // Arrange
        var email = "agency@example.com";
        var agencyResult = Agency.Create("Test Agency", email, false, 10);
        var expectedAgency = agencyResult.Data;
        
        this.mockAgencyRepository
            .Setup(repo => repo.GetByEmailAsync(email))
            .ReturnsAsync(expectedAgency);
        
        // Act
        var result = await this.agencyService.GetByEmailAsync(email);
        
        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(email);
        this.mockAgencyRepository.Verify(repo => repo.GetByEmailAsync(email), Times.Once);
    }

    [Fact]
    public async Task RegisterAgencyAsync_ShouldRegisterAndReturnSuccess()
    {
        // Arrange
        var name = "New Agency";
        var email = "new@example.com";
        var requiresApproval = true;
        var maxAppointmentsPerDay = 10;
        
        this.mockAgencyRepository
            .Setup(repo => repo.GetByEmailAsync(email))
            .ReturnsAsync((Agency)null);
        
        this.mockAgencyRepository
            .Setup(repo => repo.UpsertAsync(It.IsAny<Agency>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        // Act
        var result = await this.agencyService.RegisterAgencyAsync(name, email, requiresApproval, maxAppointmentsPerDay);
        
        // Assert
        result.Succeeded.Should().BeTrue();
        
        this.mockAgencyRepository.Verify(
            repo => repo.UpsertAsync(
                It.Is<Agency>(a => 
                    a.Name == name && 
                    a.Email == email && 
                    a.RequiresApproval == requiresApproval &&
                    a.MaxAppointmentsPerDay == maxAppointmentsPerDay),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        this.mockEventDispatcher.Verify(
            dispatcher => dispatcher.Dispatch(It.IsAny<AgencyRegisteredEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAgencyAsync_ShouldReturnFailure_WhenEmailAlreadyExists()
    {
        // Arrange
        var name = "New Agency";
        var email = "existing@example.com";
        var requiresApproval = true;
        var maxAppointmentsPerDay = 10;
        
        var existingAgencyResult = Agency.Create("Existing Agency", email, false, 10);
        var existingAgency = existingAgencyResult.Data;
        
        this.mockAgencyRepository
            .Setup(repo => repo.GetByEmailAsync(email))
            .ReturnsAsync(existingAgency);
        
        // Act
        var result = await this.agencyService.RegisterAgencyAsync(name, email, requiresApproval, maxAppointmentsPerDay);
        
        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("already exists"));
        
        this.mockAgencyRepository.Verify(
            repo => repo.UpsertAsync(It.IsAny<Agency>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApproveAgencyAsync_ShouldApproveAndReturnSuccess()
    {
        // Arrange
        var agencyResult = Agency.Create("Test Agency", "agency@example.com", true, 10);
        var agency = agencyResult.Data;
        
        this.mockAgencyRepository
            .Setup(repo => repo.GetByIdAsync(agency.Id))
            .ReturnsAsync(agency);

        this.mockAgencyRepository
            .Setup(repo => repo.UpsertAsync(
                It.Is<Agency>(a => a.IsApproved == true),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.agencyService.ApproveAgencyAsync(agency.Id);

        // Assert
        result.Succeeded.Should().BeTrue();
        
        this.mockAgencyRepository.Verify(
            repo => repo.UpsertAsync(
                It.Is<Agency>(a => a.Id == agency.Id && a.IsApproved == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AssignUserToAgencyAsync_ShouldAssignUserAndReturnSuccess()
    {
        // Arrange
        var email = "user@example.com";
        var fullName = "John Doe";
        var roles = new List<string> { "Agent" };
        
        var agencyResult = Agency.Create("Test Agency", "agency@test.com", false, 10);
        var agency = agencyResult.Data;
        agency.Approve(); // Make sure agency is approved
        
        this.mockAgencyRepository
            .Setup(repo => repo.GetByIdAsync(agency.Id))
            .ReturnsAsync(agency);
        
        this.mockAgencyUserRepository
            .Setup(repo => repo.GetByEmailAsync(email))
            .ReturnsAsync((AgencyUser)null); // User doesn't exist yet
        
        // Act
        var result = await this.agencyService.AssignUserToAgencyAsync(agency.Id, email, fullName, roles);
        
        // Assert
        result.Succeeded.Should().BeTrue();
        
        // Verify agency was updated with the new user
        this.mockAgencyRepository.Verify(
            repo => repo.UpsertAsync(
                It.Is<Agency>(a => a.Id == agency.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        // Verify event was dispatched
        this.mockEventDispatcher.Verify(
            dispatcher => dispatcher.Dispatch(
                It.Is<AgencyUserAssignedEvent>(e => 
                    e.AgencyId == agency.Id && 
                    e.UserEmail == email && 
                    e.FullName == fullName &&
                    e.Roles.Contains("Agent")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUserEmailAsync_ShouldReturnEmail_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedEmail = "user@example.com";
        var userResult = AgencyUser.Create(
            Guid.NewGuid(),
            expectedEmail,
            "Test User",
            new[] { "Role1" });
        var user = userResult.Data;
        
        this.mockAgencyUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(user);
        
        // Act
        var result = await this.agencyService.GetUserEmailAsync(userId);
        
        // Assert
        result.Should().Be(expectedEmail);
        this.mockAgencyUserRepository.Verify(repo => repo.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenAgencyExists()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        
        this.mockAgencyRepository
            .Setup(repo => repo.ExistsAsync(agencyId))
            .ReturnsAsync(true);
        
        // Act
        var result = await this.agencyService.ExistsAsync(agencyId);
        
        // Assert
        result.Should().BeTrue();
        this.mockAgencyRepository.Verify(repo => repo.ExistsAsync(agencyId), Times.Once);
    }

    [Fact]
    public async Task UpsertAsync_ShouldCallRepositoryUpsert()
    {
        // Arrange
        var agencyResult = Agency.Create(
            "Test Agency",
            "agency@example.com",
            false,
            10);
        var agency = agencyResult.Data;
        
        this.mockAgencyRepository
            .Setup(repo => repo.UpsertAsync(agency, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        // Act
        await this.agencyService.UpsertAsync(agency);
        
        // Assert
        this.mockAgencyRepository.Verify(
            repo => repo.UpsertAsync(agency, It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}
