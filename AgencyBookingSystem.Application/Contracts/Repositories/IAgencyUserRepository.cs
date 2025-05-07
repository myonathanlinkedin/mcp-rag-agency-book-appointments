using System.Threading.Tasks;

public interface IAgencyUserRepository : IDomainRepository<AgencyUser>
{
    Task<AgencyUser?> GetByIdentityUserIdAsync(string identityUserId);
}
