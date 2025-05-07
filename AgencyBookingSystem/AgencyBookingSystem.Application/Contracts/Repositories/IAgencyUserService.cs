public interface IAgencyUserService : IBaseService<AgencyUser>
{
    Task<AgencyUser?> GetByEmailAsync(string email);
}
