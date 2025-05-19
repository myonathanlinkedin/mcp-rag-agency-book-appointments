using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

public class AgencyServiceTests
{
    private readonly Mock<IAppointmentUnitOfWork> mockUnitOfWork;
    private readonly Mock<IEventDispatcher> mockEventDispatcher;
    private readonly Mock<ILogger<AgencyService>> mockLogger;
    private readonly AgencyService agencyService;

    public AgencyServiceTests()
    {
        this.mockUnitOfWork = new Mock<IAppointmentUnitOfWork>();
        this.mockEventDispatcher = new Mock<IEventDispatcher>();
        this.mockLogger = new Mock<ILogger<AgencyService>>();
        
        this.agencyService = new AgencyService(
            this.mockUnitOfWork.Object,
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
        
        this.mockUnitOfWork
            .Setup(uow => uow.Agencies.GetByIdAsync(agencyId))
            .ReturnsAsync(expectedAgency);
        
        // Act
        var result = await this.agencyService.GetByIdAsync(agencyId);
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(agencyId);
        result.Name.Should().Be("Test Agency");
        this.mockUnitOfWork.Verify(uow => uow.Agencies.GetByIdAsync(agencyId), Times.Once);
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
        
        this.mockUnitOfWork
            .Setup(uow => uow.Agencies.GetAllAsync())
            .ReturnsAsync(agencies);
        
        // Act
        var result = await this.agencyService.GetAllAsync();
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Agency 1");
        result[1].Name.Should().Be("Agency 2");
        this.mockUnitOfWork.Verify(uow => uow.Agencies.GetAllAsync(), Times.Once);
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
        
        this.mockUnitOfWork
            .Setup(uow => uow.Agencies.GetAgenciesWithUsersAsync())
            .ReturnsAsync(agencies);
        
        // Act
        var result = await this.agencyService.GetAgenciesWithUsersAsync();
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        this.mockUnitOfWork.Verify(uow => uow.Agencies.GetAgenciesWithUsersAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnAgency_WhenAgencyExists()
    {
        // Arrange
        var email = "agency@example.com";
        var agencyResult = Agency.Create("Test Agency", email, false, 10);
        var expectedAgency = agencyResult.Data;
        
        this.mockUnitOfWork
            .Setup(uow => uow.Agencies.GetByEmailAsync(email))
            .ReturnsAsync(expectedAgency);
        
        // Act
        var result = await this.agencyService.GetByEmailAsync(email);
        
        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(email);
        this.mockUnitOfWork.Verify(uow => uow.Agencies.GetByEmailAsync(email), Times.Once);
    }

    [Fact]
    public async Task RegisterAgencyAsync_ShouldRegisterAndReturnSuccess()
    {
        // Arrange
        var name = "New Agency";
        var email = "new@example.com";
        var requiresApproval = true;
        var maxAppointmentsPerDay = 10;
        
        this.mockUnitOfWork
            .Setup(uow => uow.Agencies.GetByEmailAsync(email))
            .ReturnsAsync((Agency)null);
        
        this.mockUnitOfWork
            .Setup(uow => uow.Agencies.UpsertAsync(It.IsAny<Agency>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
        
        // Act
        var result = await this.agencyService.RegisterAgencyAsync(name, email, requiresApproval, maxAppointmentsPerDay);
        
        // Assert
        result.Succeeded.Should().BeTrue();
        
        this.mockUnitOfWork.Verify(
            uow => uow.Agencies.UpsertAsync(It.Is<Agency>(a => 
                a.Name == name && 
                a.Email == email && 
                a.RequiresApproval == requiresApproval && 
                a.MaxAppointmentsPerDay == maxAppointmentsPerDay),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAgencyAsync_ShouldReturnFailure_WhenEmailExists()
    {
        // Arrange
        var name = "New Agency";
        var email = "existing@example.com";
        var requiresApproval = true;
        var maxAppointmentsPerDay = 10;
        
        var existingAgencyResult = Agency.Create("Existing Agency", email, false, 10);
        var existingAgency = existingAgencyResult.Data;
        
        this.mockUnitOfWork
            .Setup(uow => uow.Agencies.GetByEmailAsync(email))
            .ReturnsAsync(existingAgency);
        
        // Act
        var result = await this.agencyService.RegisterAgencyAsync(name, email, requiresApproval, maxAppointmentsPerDay);
        
        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("already exists"));
        
        this.mockUnitOfWork.Verify(
            uow => uow.Agencies.UpsertAsync(It.IsAny<Agency>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApproveAgencyAsync_ShouldApproveAndReturnSuccess()
    {
        // Arrange
        var agencyResult = Agency.Create("Test Agency", "agency@example.com", true, 10);
        var agency = agencyResult.Data;
        
        this.mockUnitOfWork
            .Setup(uow => uow.Agencies.GetByIdAsync(agency.Id))
            .ReturnsAsync(agency);

        this.mockUnitOfWork
            .Setup(uow => uow.Agencies.UpsertAsync(
                It.Is<Agency>(a => a.IsApproved == true),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        // Act
        var result = await this.agencyService.ApproveAgencyAsync(agency.Id);

        // Assert
        result.Succeeded.Should().BeTrue();
        
        this.mockUnitOfWork.Verify(
            uow => uow.Agencies.UpsertAsync(
                It.Is<Agency>(a => a.Id == agency.Id && a.IsApproved == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AssignUserToAgencyAsync_ShouldAssignUserAndReturnSuccess()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var userEmail = "user@example.com";
        var agency = new Agency(agencyId, "Test Agency", "test@agency.com");
        var agencyUser = new AgencyUser(Guid.NewGuid(), userEmail, "Test User");

        mockUnitOfWork.Setup(uow => uow.Agencies.GetByIdAsync(agencyId))
            .ReturnsAsync(agency);

        mockUnitOfWork.Setup(uow => uow.AgencyUsers.GetByEmailAsync(userEmail))
            .ReturnsAsync(agencyUser);

        mockUnitOfWork.Setup(uow => uow.AgencyUsers.AddAsync(It.IsAny<AgencyUser>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(agencyUser));

        mockUnitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        // Act
        var result = await agencyService.AssignUserToAgencyAsync(agencyId, userEmail);

        // Assert
        result.Succeeded.Should().BeTrue();
        mockUnitOfWork.Verify(uow => uow.Agencies.GetByIdAsync(agencyId), Times.Once);
        mockUnitOfWork.Verify(uow => uow.AgencyUsers.GetByEmailAsync(userEmail), Times.Once);
        mockUnitOfWork.Verify(uow => uow.AgencyUsers.AddAsync(It.IsAny<AgencyUser>(), It.IsAny<CancellationToken>()), Times.Once);
        mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenAgencyExists()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        
        this.mockUnitOfWork
            .Setup(uow => uow.Agencies.ExistsAsync(agencyId))
            .ReturnsAsync(true);
        
        // Act
        var result = await this.agencyService.ExistsAsync(agencyId);
        
        // Assert
        result.Should().BeTrue();
        this.mockUnitOfWork.Verify(uow => uow.Agencies.ExistsAsync(agencyId), Times.Once);
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
        
        this.mockUnitOfWork
            .Setup(uow => uow.Agencies.UpsertAsync(agency, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
        
        // Act
        await this.agencyService.UpsertAsync(agency);
        
        // Assert
        this.mockUnitOfWork.Verify(
            uow => uow.Agencies.UpsertAsync(agency, It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}
