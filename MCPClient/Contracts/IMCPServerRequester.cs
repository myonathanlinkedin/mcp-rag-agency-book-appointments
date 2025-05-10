using Microsoft.Extensions.AI;

public interface IMCPServerRequester
{
    Task<Result<string>> RequestAsync(string prompt, ChatRole? chatRole = null, bool useSession = false, CancellationToken cancellationToken = default);
}