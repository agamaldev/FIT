namespace FitApi.Dtos;

public record ProfileDto(
    string DisplayName,
    string Email,
    string? PhotoUrl,
    decimal? Weight,
    decimal? Height,
    string? Goal,
    string? Activity);

public record UpdateProfileRequest(
    string? DisplayName,
    decimal? Weight,
    decimal? Height,
    string? Goal,
    string? Activity);
