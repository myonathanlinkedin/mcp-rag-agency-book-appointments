public class RefreshTokenRequestModel
{
    public RefreshTokenRequestModel(string userId, string refreshToken)
    {
        UserId = userId;
        RefreshToken = refreshToken;
    }

    public string UserId { get; }
    public string RefreshToken { get; }
}
