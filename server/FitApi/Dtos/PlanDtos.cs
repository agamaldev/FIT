namespace FitApi.Dtos;

public record PlanItemDto(
    string Id,
    string? NameAr,
    string? NameEn,
    string? Tab,
    int Sets,
    int Reps,
    int Order);

public record AddPlanItemRequest(
    string Id,
    string? NameAr,
    string? NameEn,
    string? Tab,
    int? Sets,
    int? Reps);
