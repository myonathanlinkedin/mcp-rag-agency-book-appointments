using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;

public interface IIdentity
{
    Task<Result<IUser>> Register(UserRequestModel userRequest, CancellationToken cancellationToken);
    Task<Result<UserResponseModel>> Login(UserRequestModel userRequest, CancellationToken cancellationToken);
    Task<Result> ChangePassword(ChangePasswordRequestModel changePasswordRequest, CancellationToken cancellationToken);
    Task<Result> ResetPassword(string email, CancellationToken cancellationToken);
    Task<Result<string>> RefreshToken(RefreshTokenRequestModel refreshTokenRequest, CancellationToken cancellationToken);
    Task<Result> AssignRoleAsync(string email, string roleName, CancellationToken cancellationToken);
    Result<JsonWebKey> GetPublicKey();
}
