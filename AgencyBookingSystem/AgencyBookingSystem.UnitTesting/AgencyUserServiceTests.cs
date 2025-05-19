using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

public class AgencyUserServiceTests
{
    private readonly Mock<IAppointmentUnitOfWork> mockUnitOfWork;
    private readonly Mock<ILogger<AgencyUserService>> mockLogger;
    private readonly AgencyUserService agencyUserService;

    public AgencyUserServiceTests()
    {
        this.mockUnitOfWork = new Mock<IAppointmentUnitOfWork>();
        this.mockLogger = new Mock<ILogger<AgencyUserService>>();
        
        this.agencyUserService = new AgencyUserService(
            this.mockUnitOfWork.Object,
            this.mockLogger.Object
        );
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnAgencyUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var agencyId = Guid.NewGuid();
        var agencyUserResult = AgencyUser.Create(agencyId, "test@example.com", "Test User", new[] { "Customer" });
        var expectedUser = agencyUserResult.Data;
        
        this.mockUnitOfWork
            .Setup(uow => uow.AgencyUsers.GetByIdAsync(userId))
            .ReturnsAsync(expectedUser);
        
        // Act
        var result = await this.agencyUserService.GetByIdAsync(userId);
        
        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("test@example.com");
        this.mockUnitOfWork.Verify(uow => uow.AgencyUsers.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllAgencyUsers()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var user1Result = AgencyUser.Create(agencyId, "user1@test.com", "User 1", new[] { "Customer" });
        var user2Result = AgencyUser.Create(agencyId, "user2@test.com", "User 2", new[] { "Customer" });
        
        var users = new List<AgencyUser>
        {
            user1Result.Data,
            user2Result.Data
        };
        
        this.mockUnitOfWork
            .Setup(uow => uow.AgencyUsers.GetAllAsync())
            .ReturnsAsync(users);
        
        // Act
        var result = await this.agencyUserService.GetAllAsync();
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Email.Should().Be("user1@test.com");
        result[1].Email.Should().Be("user2@test.com");
        this.mockUnitOfWork.Verify(uow => uow.AgencyUsers.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnAgencyUser_WhenUserExists()
    {
        // Arrange
        var email = "test@example.com";
        var agencyId = Guid.NewGuid();
        var agencyUserResult = AgencyUser.Create(agencyId, email, "Test User", new[] { "Customer" });
        var expectedUser = agencyUserResult.Data;
        
        this.mockUnitOfWork
            .Setup(uow => uow.AgencyUsers.GetByEmailAsync(email))
            .ReturnsAsync(expectedUser);
        
        // Act
        var result = await this.agencyUserService.GetByEmailAsync(email);
        
        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(email);
        this.mockUnitOfWork.Verify(uow => uow.AgencyUsers.GetByEmailAsync(email), Times.Once);
    }

    [Fact]
    public async Task AddAsync_ShouldAddNewAgencyUser()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var agencyUserResult = AgencyUser.Create(agencyId, "new@example.com", "New User", new[] { "Customer" });
        var newUser = agencyUserResult.Data;
        
        this.mockUnitOfWork
            .Setup(uow => uow.AgencyUsers.AddAsync(newUser, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        // Act
        await this.agencyUserService.AddAsync(newUser);
        
        // Assert
        this.mockUnitOfWork.Verify(
            uow => uow.AgencyUsers.AddAsync(newUser, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Update_ShouldUpdateExistingAgencyUser()
    {
        // Arrange
        var agencyUser = new AgencyUser(Guid.NewGuid(), "test@example.com", "Test User");
        
        mockUnitOfWork.Setup(uow => uow.AgencyUsers.GetByIdAsync(agencyUser.Id))
            .ReturnsAsync(agencyUser);

        mockUnitOfWork.Setup(uow => uow.AgencyUsers.Update(It.IsAny<AgencyUser>()))
            .Returns(agencyUser);

        mockUnitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        // Act
        var result = await agencyUserService.Update(agencyUser);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        mockUnitOfWork.Verify(uow => uow.AgencyUsers.GetByIdAsync(agencyUser.Id), Times.Once);
        mockUnitOfWork.Verify(uow => uow.AgencyUsers.Update(It.IsAny<AgencyUser>()), Times.Once);
        mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
