public class UserResponseModel
{
    public UserResponseModel(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
    }

    public string AccessToken { get; }
    public string RefreshToken { get; }
}
