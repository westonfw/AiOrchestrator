namespace AiOrchestrator.Application;

public interface IPublicMarketDataProvider
{
    Task<IReadOnlyList<PublicCompanySearchResult>> SearchCompaniesAsync(string keyword, string? market, CancellationToken ct = default);
    Task<PublicCompanyProfile?> GetCompanyProfileAsync(string symbol, string? market, CancellationToken ct = default);
    Task<PublicStockQuote?> GetLatestQuoteAsync(string symbol, string? market, CancellationToken ct = default);
}

public sealed record PublicDataSource(
    string Code,
    string Name,
    string? Url,
    DateTimeOffset RetrievedAt);

public sealed record PublicCompanySearchResult(
    string Symbol,
    string Name,
    string? Market,
    string? Exchange,
    string? DataSource);

public sealed record PublicCompanyProfile(
    string Symbol,
    string Name,
    string? Market,
    string? Exchange,
    string? Industry,
    string? Website,
    string? Description,
    PublicDataSource Source);

public sealed record PublicStockQuote(
    string Symbol,
    string? Market,
    DateOnly TradeDate,
    TimeOnly? TradeTime,
    decimal? Open,
    decimal? High,
    decimal? Low,
    decimal? Close,
    long? Volume,
    string Currency,
    PublicDataSource Source);
