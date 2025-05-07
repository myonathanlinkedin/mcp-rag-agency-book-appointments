using Refit;

public interface IIdentityApi
{
    [Post("/api/Identity/Register/RegisterAsync")]
    Task<HttpResponseMessage> RegisterAsync([Body] object payload);

    [Post("/api/Identity/Login/LoginAsync")]
    Task<string> LoginAsync([Body] object payload);

    [Put("/api/Identity/ChangePassword/ChangePasswordAsync")]
    Task<HttpResponseMessage> ChangePasswordAsync([Body] object payload, [Header("Authorization")] string token);

    [Post("/api/Identity/ResetPassword/ResetPasswordAsync")]
    Task<HttpResponseMessage> ResetPasswordAsync([Body] object payload);

    [Put("/api/Identity/AssignRole/AssignRoleAsync")]
    Task<HttpResponseMessage> AssignRoleAsync([Body] object payload, [Header("Authorization")] string token);
}
