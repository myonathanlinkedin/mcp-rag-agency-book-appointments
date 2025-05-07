public interface IAgencyRepository : IDomainRepository<Agency>
{
    Task<List<Agency>> GetAgenciesWithUsersAsync();
    Task<Agency?> GetByEmailAsync(string email);
    Task<List<Agency>> GetApprovedAgenciesAsync();
    Task<bool> ExistsAsync(Guid agencyId);
}
