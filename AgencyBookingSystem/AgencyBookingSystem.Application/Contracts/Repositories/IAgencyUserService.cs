public interface IAgencyUserService : IBaseService<AgencyUser>
{
    Task<AgencyUser?> GetByIdentityUserIdAsync(string identityUserId);
}
