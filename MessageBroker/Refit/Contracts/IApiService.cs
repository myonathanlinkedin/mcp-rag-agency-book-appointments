using Refit;

public interface IApiService
{
    [Get("/items/{id}")]
    Task<ApiResponse<object>> GetItemByIdAsync(string id);
}