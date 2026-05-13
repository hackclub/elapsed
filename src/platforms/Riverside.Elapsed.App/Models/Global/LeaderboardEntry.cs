namespace Riverside.Elapsed.App.Models.Global;

public sealed class LeaderboardEntry
{
	public User.User User { get; set; } = new();
	public double SecondsThisWeek { get; set; }
}
