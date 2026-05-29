using System.Globalization;
using AiOrchestrator.Application;
using Microsoft.Extensions.Configuration;

namespace AiOrchestrator.Infrastructure;

public sealed class PublicMarketDataProvider(HttpClient httpClient, IConfiguration configuration) : IPublicMarketDataProvider
{
    private static readonly IReadOnlyList<PublicCompanySearchResult> BuiltInCompanies =
    [
        new("AAPL", "Apple Inc.", "us", "NASDAQ", "built_in"),
        new("MSFT", "Microsoft Corporation", "us", "NASDAQ", "built_in"),
        new("GOOGL", "Alphabet Inc.", "us", "NASDAQ", "built_in"),
        new("AMZN", "Amazon.com, Inc.", "us", "NASDAQ", "built_in"),
        new("TSLA", "Tesla, Inc.", "us", "NASDAQ", "built_in"),
        new("NVDA", "NVIDIA Corporation", "us", "NASDAQ", "built_in"),
        new("BABA", "Alibaba Group Holding Limited", "us", "NYSE", "built_in"),
        new("0700", "Tencent Holdings Limited", "hk", "HKEX", "built_in"),
        new("600519", "Kweichow Moutai Co., Ltd.", "cn", "SSE", "built_in"),
        new("000001", "Ping An Bank Co., Ltd.", "cn", "SZSE", "built_in")
    ];

    public Task<IReadOnlyList<PublicCompanySearchResult>> SearchCompaniesAsync(string keyword, string? market, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Task.FromResult<IReadOnlyList<PublicCompanySearchResult>>([]);
        }

        var normalizedKeyword = keyword.Trim();
        var normalizedMarket = NormalizeMarket(market);
        var results = BuiltInCompanies
            .Where(company =>
                (string.IsNullOrWhiteSpace(normalizedMarket) || string.Equals(company.Market, normalizedMarket, StringComparison.OrdinalIgnoreCase))
                && (company.Symbol.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase)
                    || company.Name.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase)))
            .Take(20)
            .ToList();

        return Task.FromResult<IReadOnlyList<PublicCompanySearchResult>>(results);
    }

    public Task<PublicCompanyProfile?> GetCompanyProfileAsync(string symbol, string? market, CancellationToken ct = default)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        var normalizedMarket = NormalizeMarket(market);
        var match = BuiltInCompanies.FirstOrDefault(company =>
            string.Equals(company.Symbol, normalizedSymbol, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(normalizedMarket) || string.Equals(company.Market, normalizedMarket, StringComparison.OrdinalIgnoreCase)));

        if (match is null)
        {
            return Task.FromResult<PublicCompanyProfile?>(null);
        }

        return Task.FromResult<PublicCompanyProfile?>(new PublicCompanyProfile(
            match.Symbol,
            match.Name,
            match.Market,
            match.Exchange,
            InferIndustry(match.Symbol),
            null,
            "第一版内置公开公司基础资料，用于统一数据接口和后续外部数据源替换。",
            new PublicDataSource("built_in", "Built-in public company seed data", null, DateTimeOffset.UtcNow)));
    }

    public async Task<PublicStockQuote?> GetLatestQuoteAsync(string symbol, string? market, CancellationToken ct = default)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        if (string.IsNullOrWhiteSpace(normalizedSymbol))
        {
            return null;
        }

        var stooqSymbol = ToStooqSymbol(normalizedSymbol, market);
        var baseUrl = configuration["PublicData:StooqBaseUrl"]?.TrimEnd('/') ?? "https://stooq.com";
        var url = $"{baseUrl}/q/l/?s={Uri.EscapeDataString(stooqSymbol)}&f=sd2t2ohlcv&h&e=csv";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("AiBusinessOrchestrator/0.1");
        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var csv = await response.Content.ReadAsStringAsync(ct);
        return ParseStooqQuote(csv, normalizedSymbol, NormalizeMarket(market), url);
    }

    private static PublicStockQuote? ParseStooqQuote(string csv, string symbol, string? market, string url)
    {
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
        {
            return null;
        }

        var values = lines[1].Split(',');
        if (values.Length < 8 || values.Any(value => string.Equals(value, "N/D", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        if (!DateOnly.TryParse(values[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return null;
        }

        TimeOnly? time = TimeOnly.TryParse(values[2], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTime)
            ? parsedTime
            : null;

        return new PublicStockQuote(
            symbol,
            market,
            date,
            time,
            ParseDecimal(values[3]),
            ParseDecimal(values[4]),
            ParseDecimal(values[5]),
            ParseDecimal(values[6]),
            long.TryParse(values[7], NumberStyles.Any, CultureInfo.InvariantCulture, out var volume) ? volume : null,
            InferCurrency(market),
            new PublicDataSource("stooq", "Stooq public quote CSV", url, DateTimeOffset.UtcNow));
    }

    private static string NormalizeSymbol(string symbol) => symbol.Trim().ToUpperInvariant();

    private static string? NormalizeMarket(string? market) => string.IsNullOrWhiteSpace(market) ? null : market.Trim().ToLowerInvariant();

    private static decimal? ParseDecimal(string value)
    {
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static string ToStooqSymbol(string symbol, string? market)
    {
        var normalizedMarket = NormalizeMarket(market);
        if (symbol.Contains('.', StringComparison.Ordinal))
        {
            return symbol.ToLowerInvariant();
        }

        return normalizedMarket switch
        {
            "us" or "usa" => $"{symbol}.us".ToLowerInvariant(),
            "hk" => $"{symbol}.hk".ToLowerInvariant(),
            "cn" or "sh" or "sse" when symbol.StartsWith('6') => $"{symbol}.cn".ToLowerInvariant(),
            "cn" or "sz" or "szse" => $"{symbol}.sz".ToLowerInvariant(),
            _ => $"{symbol}.us".ToLowerInvariant()
        };
    }

    private static string InferCurrency(string? market)
    {
        return NormalizeMarket(market) switch
        {
            "hk" => "HKD",
            "cn" or "sh" or "sse" or "sz" or "szse" => "CNY",
            _ => "USD"
        };
    }

    private static string? InferIndustry(string symbol)
    {
        return symbol switch
        {
            "AAPL" or "MSFT" or "GOOGL" or "NVDA" => "Technology",
            "AMZN" => "Consumer Discretionary",
            "TSLA" => "Automobiles",
            "BABA" or "0700" => "Internet",
            "600519" => "Consumer Staples",
            "000001" => "Banking",
            _ => null
        };
    }
}
