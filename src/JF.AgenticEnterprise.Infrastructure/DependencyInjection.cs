using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Infrastructure.Persistence;
using JF.AgenticEnterprise.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JF.AgenticEnterprise.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("InboxDb")
            ?? "Data Source=Data/inbox.db";

        services.AddDbContext<InboxDbContext>(opts =>
            opts.UseSqlite(connectionString));

        services.AddScoped<IEmailRepository, EmailRepository>();
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IClassificationRepository, ClassificationRepository>();
        services.AddScoped<ITaxonomyRepository, TaxonomyRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();

        services.AddScoped<DataSeeder>();

        return services;
    }
}
