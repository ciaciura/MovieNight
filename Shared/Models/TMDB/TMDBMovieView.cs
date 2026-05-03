namespace Shared.Models.TMDB;

public sealed class TMDBMovieView
{
    public string? Title { get; set; }
    public string? Overview { get; set; }
    public DateTimeOffset? ReleaseDate { get; set; }
}
