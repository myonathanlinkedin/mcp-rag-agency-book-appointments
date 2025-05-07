using Microsoft.Extensions.AI;

public interface IMCPServerRequester
{
   Task<Result<string>> RequestAsync(string prompt, string token, ChatRole? chatRole = null, bool useSession = false);
}