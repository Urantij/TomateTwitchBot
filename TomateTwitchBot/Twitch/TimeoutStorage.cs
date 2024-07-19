namespace TomateTwitchBot.Twitch;

public class TimeoutStorage
{
    class Info
    {
        public required string TwitchId { get; init; }
        public required int Time { get; set; }
        public required DateTimeOffset TimeDate { get; set; }
    }

    private readonly List<Info> _list = new();

    public void AddTime(string twitchId, int time)
    {
        lock (_list)
        {
            Info? info = _list.FirstOrDefault(l => l.TwitchId == twitchId);

            if (info == null)
            {
                info = new()
                {
                    TwitchId = twitchId,
                    Time = time,
                    TimeDate = DateTimeOffset.UtcNow
                };
                lock (_list)
                {
                    _list.Add(info);
                }

                return;
            }

            int timeLeft = info.Time - (int)(DateTimeOffset.UtcNow - info.TimeDate).TotalSeconds;

            if (timeLeft <= 0)
            {
                info.Time = time;
                info.TimeDate = DateTimeOffset.UtcNow;
            }
            else
            {
                info.Time += time;
            }
        }
    }

    public int? GetExistingTime(string twitchId)
    {
        lock (_list)
        {
            Info? info = _list.FirstOrDefault(l => l.TwitchId == twitchId);

            if (info == null)
                return null;

            int timeLeft = info.Time - (int)(DateTimeOffset.UtcNow - info.TimeDate).TotalSeconds;

            if (timeLeft <= 0)
            {
                lock (_list)
                {
                    _list.Remove(info);
                }

                return null;
            }

            return timeLeft;
        }
    }

    public void Clear()
    {
        lock (_list)
        {
            foreach (Info info in _list.ToArray())
            {
                int timeLeft = info.Time - (int)(DateTimeOffset.UtcNow - info.TimeDate).TotalSeconds;

                if (timeLeft > 0)
                    continue;

                _list.Remove(info);
            }
        }
    }
}