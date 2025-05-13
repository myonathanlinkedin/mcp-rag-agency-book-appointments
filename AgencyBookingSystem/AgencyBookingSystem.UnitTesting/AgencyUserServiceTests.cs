using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

public class AgencyUserServiceTests
{
    private readonly Mock<IAgencyUserRepository> mockAgencyUserRepository;
    private readonly Mock<ILogger<AgencyUserService>> mockLogger;
    private readonly AgencyUserService agencyUserService;

    public AgencyUserServiceTests()
    {
        this.mockAgencyUserRepository = new Mock<IAgencyUserRepository>();
        this.mockLogger = new Mock<ILogger<AgencyUserService>>();
        
        this.agencyUserService = new AgencyUserService(
            this.mockAgencyUserRepository.Object,
            this.mockLogger.Object
        );
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnAgencyUser_WhenUserExists()
    {
        // Arrange
        var userId = new Guid("1bc5eeb9-aee9-46e2-a32f-f5b4a5b06a23");
        var agencyId = Guid.NewGuid();
        var expectedUser = new AgencyUser(
            userId,
            agencyId,
            "test@user.com",
            "Test User");
        
        this.mockAgencyUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(expectedUser);
        
        // Act
        var result = await this.agencyUserService.GetByIdAsync(userId);
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Email.Should().Be("test@user.com");
        this.mockAgencyUserRepository.Verify(repo => repo.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        this.mockAgencyUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync((AgencyUser)null);
        
        // Act
        var result = await this.agencyUserService.GetByIdAsync(userId);
        
        // Assert
        result.Should().BeNull();
        this.mockAgencyUserRepository.Verify(repo => repo.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnAgencyUser_WhenUserExists()
    {
        // Arrange
        var email = "user@example.com";
        var agencyId = Guid.NewGuid();
        var userResult = AgencyUser.Create(
            agencyId,
            email,
            "Test User",
            new[] { CommonModelConstants.AgencyRole.Customer });
        if (!userResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", userResult.Errors)}");
        }
        var expectedUser = userResult.Data;
        
        this.mockAgencyUserRepository
            .Setup(repo => repo.GetByEmailAsync(email))
            .ReturnsAsync(expectedUser);
        
        // Act
        var result = await this.agencyUserService.GetByEmailAsync(email);
        
        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(email);
        this.mockAgencyUserRepository.Verify(repo => repo.GetByEmailAsync(email), Times.Once);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var email = "nonexistent@example.com";
        
        this.mockAgencyUserRepository
            .Setup(repo => repo.GetByEmailAsync(email))
            .ReturnsAsync((AgencyUser)null);
        
        // Act
        var result = await this.agencyUserService.GetByEmailAsync(email);
        
        // Assert
        result.Should().BeNull();
        this.mockAgencyUserRepository.Verify(repo => repo.GetByEmailAsync(email), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllAgencyUsers()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var user1Result = AgencyUser.Create(
            agencyId,
            "user1@example.com",
            "User 1",
            new[] { CommonModelConstants.AgencyRole.Customer });
        if (!user1Result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user 1: {string.Join(", ", user1Result.Errors)}");
        }

        var user2Result = AgencyUser.Create(
            agencyId,
            "user2@example.com",
            "User 2",
            new[] { CommonModelConstants.AgencyRole.Customer });
        if (!user2Result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user 2: {string.Join(", ", user2Result.Errors)}");
        }
        
        var users = new List<AgencyUser>
        {
            user1Result.Data,
            user2Result.Data
        };
        
        this.mockAgencyUserRepository
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(users);
        
        // Act
        var result = await this.agencyUserService.GetAllAsync();
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Email.Should().Be("user1@example.com");
        result[1].Email.Should().Be("user2@example.com");
        this.mockAgencyUserRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task UpsertAsync_ShouldCallRepositoryUpsert()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var userResult = AgencyUser.Create(
            agencyId,
            "test@user.com",
            "Test User",
            new[] { CommonModelConstants.AgencyRole.Customer });
        if (!userResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", userResult.Errors)}");
        }
        var user = userResult.Data;
        
        // Act
        await this.agencyUserService.UpsertAsync(user);
        
        // Assert
        this.mockAgencyUserRepository.Verify(
            repo => repo.UpsertAsync(user, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
