namespace FitApi.Dtos;

public record WeightEntryRequest(decimal Weight, decimal? Waist, decimal? Chest, decimal? Arms, string? Date);

public record WeightEntryDto(long Id, string Date, decimal Weight, decimal? Waist, decimal? Chest, decimal? Arms);
