using AiOrchestrator.Application;
using Microsoft.Extensions.DependencyInjection;

namespace AiOrchestrator.Skills;

public static class DependencyInjection
{
    public static IServiceCollection AddSkillRuntime(this IServiceCollection services)
    {
        services.AddScoped<ISkillExecutor, SkillExecutor>();
        services.AddSingleton<IAiSkill, CompanyResolutionSkill>();
        services.AddSingleton<IAiSkill, CollectMaterialsSkill>();
        services.AddSingleton<IAiSkill, ParseUploadedFileSkill>();
        services.AddSingleton<IAiSkill, ExtractBasicFactsSkill>();
        services.AddSingleton<IAiSkill, CalculateFinancialRatiosSkill>();
        services.AddSingleton<IAiSkill, GenerateMarkdownReportSkill>();
        services.AddSingleton<IAiSkill, GenerateCreditReportDocxSkill>();
        services.AddSingleton<ISkillRegistry, SkillRegistry>();
        return services;
    }
}
