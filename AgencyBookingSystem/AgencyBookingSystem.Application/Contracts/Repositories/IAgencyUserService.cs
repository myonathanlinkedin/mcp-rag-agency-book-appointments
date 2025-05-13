public interface IAgencyUserService : IBaseService<AgencyUser>
{
    Task<AgencyUser?> GetByEmailAsync(string email);
    Task AddAsync(AgencyUser entity, CancellationToken cancellationToken = default);
    void Update(AgencyUser entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
