using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

public class AgencyServiceTests
{
    private readonly Mock<IAgencyRepository> mockAgencyRepository;
    private readonly Mock<IAgencyUserRepository> mockAgencyUserRepository;
    private readonly Mock<IEventDispatcher> mockEventDispatcher;
    private readonly Mock<ILogger<AgencyService>> mockLogger;
    private readonly AgencyService agencyService;

    public AgencyServiceTests()
    {
        this.mockAgencyRepository = new Mock<IAgencyRepository>();
        this.mockAgencyUserRepository = new Mock<IAgencyUserRepository>();
        this.mockEventDispatcher = new Mock<IEventDispatcher>();
        this.mockLogger = new Mock<ILogger<AgencyService>>();
        
        this.agencyService = new AgencyService(
            this.mockAgencyRepository.Object,
            this.mockAgencyUserRepository.Object,
            this.mockEventDispatcher.Object,
            this.mockLogger.Object
        );
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnAgency_WhenAgencyExists()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var expectedAgency = new Agency { Id = agencyId, Name = "Test Agency" };
        
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
        var agencies = new List<Agency>
        {
            new Agency { Id = Guid.NewGuid(), Name = "Agency 1" },
            new Agency { Id = Guid.NewGuid(), Name = "Agency 2" }
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
    public async Task SaveAsync_ShouldCallRepositoryUpsert()
    {
        // Arrange
        var agency = new Agency 
        { 
            Id = Guid.NewGuid(), 
            Name = "Test Agency",
            Email = "agency@example.com"
        };
        
        this.mockAgencyRepository
            .Setup(repo => repo.UpsertAsync(agency, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        // Act
        await this.agencyService.SaveAsync(agency);
        
        // Assert
        this.mockAgencyRepository.Verify(
            repo => repo.UpsertAsync(agency, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task GetAgenciesWithUsersAsync_ShouldReturnAgenciesWithUsers()
    {
        // Arrange
        var agencies = new List<Agency>
        {
            new Agency { Id = Guid.NewGuid(), Name = "Agency 1" },
            new Agency { Id = Guid.NewGuid(), Name = "Agency 2" }
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
        var expectedAgency = new Agency { Id = Guid.NewGuid(), Name = "Test Agency", Email = email };
        
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
            dispatcher => dispatcher.Dispatch(It.IsAny<AgencyRegisteredEvent>()),
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
        
        var existingAgency = new Agency { Id = Guid.NewGuid(), Name = "Existing Agency", Email = email };
        
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
        var agencyId = Guid.NewGuid();
        var agency = new Agency 
        { 
            Id = agencyId, 
            Name = "Test Agency", 
            Email = "agency@example.com",
            IsApproved = false 
        };
        
        this.mockAgencyRepository
            .Setup(repo => repo.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);
        
        this.mockAgencyRepository
            .Setup(repo => repo.UpsertAsync(It.IsAny<Agency>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        // Act
        var result = await this.agencyService.ApproveAgencyAsync(agencyId);
        
        // Assert
        result.Succeeded.Should().BeTrue();
        
        this.mockAgencyRepository.Verify(
            repo => repo.UpsertAsync(
                It.Is<Agency>(a => a.Id == agencyId && a.IsApproved == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        this.mockEventDispatcher.Verify(
            dispatcher => dispatcher.Dispatch(It.IsAny<AgencyUserAssignedEvent>()),
            Times.Once);
    }

    [Fact]
    public async Task AssignUserToAgencyAsync_ShouldAssignUserAndReturnSuccess()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var email = "user@example.com";
        var fullName = "John Doe";
        var roles = new List<string> { "Agent" };
        
        var agency = new Agency { Id = agencyId, Name = "Test Agency" };
        
        this.mockAgencyRepository
            .Setup(repo => repo.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);
        
        this.mockAgencyUserRepository
            .Setup(repo => repo.GetByEmailAsync(email))
            .ReturnsAsync((AgencyUser)null);
        
        this.mockAgencyUserRepository
            .Setup(repo => repo.UpsertAsync(It.IsAny<AgencyUser>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        // Act
        var result = await this.agencyService.AssignUserToAgencyAsync(agencyId, email, fullName, roles);
        
        // Assert
        result.Succeeded.Should().BeTrue();
        
        this.mockAgencyUserRepository.Verify(
            repo => repo.UpsertAsync(
                It.Is<AgencyUser>(u => 
                    u.AgencyId == agencyId && 
                    u.Email == email && 
                    u.FullName == fullName &&
                    u.Roles.Contains("Agent")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        this.mockEventDispatcher.Verify(
            dispatcher => dispatcher.Dispatch(It.IsAny<AgencyUserAssignedEvent>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUserEmailAsync_ShouldReturnEmail_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedEmail = "user@example.com";
        var user = new AgencyUser { Id = userId, Email = expectedEmail };
        
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
}
