namespace TomateTwitchBot.Twitch;

public class ChatCache
{
    public class Info
    {
        public required string Username { get; init; }
        public required string? DisplayName { get; init; }
        public required string Id { get; init; }
    }

    private const int CacheLimit = 50;

    private readonly List<Info> _list = new();

    public void Add(string username, string? displayName, string id)
    {
        lock (_list)
        {
            _list.Add(new Info()
            {
                Username = username,
                DisplayName = displayName,
                Id = id
            });

            if (_list.Count > CacheLimit)
            {
                _list.RemoveAt(0);
            }
        }
    }

    public Info? Resolve(string name)
    {
        lock (_list)
        {
            return _list.FirstOrDefault(l => l.Username.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                                             l.DisplayName?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
        }
    }
}