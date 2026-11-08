using System.Collections.Concurrent;

namespace ShutTheBox_Api.Auth;

public class UserStore
{
    private readonly ConcurrentDictionary<string, string> userIdsByName = new(StringComparer.OrdinalIgnoreCase);

    public string GetOrCreateUserId(string username)
    {
        return userIdsByName.GetOrAdd(username, _ => Guid.NewGuid().ToString());
    }
}
