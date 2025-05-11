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
        var userId = Guid.NewGuid();
        var expectedUser = new AgencyUser { Id = userId, Email = "user@example.com" };
        
        this.mockAgencyUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(expectedUser);
        
        // Act
        var result = await this.agencyUserService.GetByIdAsync(userId);
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Email.Should().Be("user@example.com");
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
        var expectedUser = new AgencyUser { Id = Guid.NewGuid(), Email = email };
        
        this.mockAgencyUserRepository
            .Setup(repo => repo.GetByEmailAsync(email))
            .ReturnsAsync(expectedUser);
        
        // Act
        var result = await this.agencyUserService.GetByEmailAsync(email);
        
        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(email);
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
        var users = new List<AgencyUser>
        {
            new AgencyUser { Id = Guid.NewGuid(), Email = "user1@example.com" },
            new AgencyUser { Id = Guid.NewGuid(), Email = "user2@example.com" }
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
    public async Task SaveAsync_ShouldCallRepositoryUpsert()
    {
        // Arrange
        var user = new AgencyUser 
        { 
            Id = Guid.NewGuid(), 
            Email = "user@example.com",
            FullName = "John Doe"
        };
        
        this.mockAgencyUserRepository
            .Setup(repo => repo.UpsertAsync(user, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        // Act
        await this.agencyUserService.SaveAsync(user);
        
        // Assert
        this.mockAgencyUserRepository.Verify(
            repo => repo.UpsertAsync(user, It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}
