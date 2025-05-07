using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;

public interface IIdentity
{
    Task<Result<IUser>> Register(UserRequestModel userRequest);
    Task<Result<UserResponseModel>> Login(UserRequestModel userRequest);
    Task<Result> ChangePassword(ChangePasswordRequestModel changePasswordRequest);
    Task<Result> ResetPassword(string email);
    Task<Result<string>> RefreshToken(RefreshTokenRequestModel refreshTokenRequest);
    Task<Result> AssignRoleAsync(string email, string roleName);
    Result<JsonWebKey> GetPublicKey();
}
