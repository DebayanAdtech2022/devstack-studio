using System.Text;
using DevStackStudio.Core.Contracts;
using DevStackStudio.Core.Models;
using DevStackStudio.Core.Naming;
using DevStackStudio.Core.Providers;
using DevStackStudio.Generators.Support;

namespace DevStackStudio.Generators.Backend;

public sealed class BackendCqrsGenerator
{
    public IReadOnlyList<GeneratedFile> Generate(ProjectConfiguration configuration)
    {
        var ns = NameFactory.CSharpNamespace(configuration.ProjectName);
        var provider = DatabaseProviderCatalog.Default.Get(configuration.Database.Engine);
        var defaultConn = BuildDefaultConnectionString(configuration.Database, provider);

        var files = new List<GeneratedFile>
        {
            // Project definitions matching Adtech1 display names
            File("backend/Api/Api.csproj", ApiCsproj(ns)),
            File("backend/Models/Models.csproj", ModelsCsproj(ns)),
            File("backend/Repository/Repository.csproj", RepositoryCsproj(ns, provider)),
            File("backend/Services/Services.csproj", ServicesCsproj(ns)),
            File("backend/SharedRepository/SharedRepository.csproj", SharedRepositoryCsproj(ns)),
            File("backend/SharedWeb/SharedWeb.csproj", SharedWebCsproj(ns)),
            File("backend/UnitTests/UnitTests.csproj", TestsCsproj(ns)),

            // Docker
            File("backend/Dockerfile", Dockerfile(ns)),
            File("backend/.dockerignore", "bin/\nobj/\n**/bin/\n**/obj/\n"),

            // Disable parent CPM inheritance
            File("backend/Directory.Build.props", """
                <Project>
                  <PropertyGroup>
                    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
                  </PropertyGroup>
                </Project>
                """),
            File("backend/Directory.Packages.props", """
                <Project>
                  <PropertyGroup>
                    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
                  </PropertyGroup>
                </Project>
                """),

            // Configuration matching Adtech1 appsettings
            File("backend/Api/appsettings.json", $$"""
                {
                  "AllowedCORSOrigins": [ "http://localhost:3000", "http://127.0.0.1:3000" ],
                  "DefaultConnection": "{{defaultConn}}",
                  "Database": {
                    "ApplyMigrations": false
                  },
                  "Logging": {
                    "LogLevel": {
                      "Default": "Information",
                      "Microsoft.AspNetCore": "Warning"
                    }
                  },
                  "AllowedHosts": "*"
                }
                """),
            File("backend/Api/appsettings.Development.json", $$"""
                {
                  "AllowedCORSOrigins": [ "http://localhost:3000", "http://127.0.0.1:3000" ],
                  "DefaultConnection": "{{defaultConn}}",
                  "Logging": {
                    "LogLevel": {
                      "Default": "Information",
                      "Microsoft.AspNetCore": "Warning"
                    }
                  }
                }
                """),

            // SharedRepository
            File("backend/SharedRepository/PageQueryModel.cs", SharedRepoPageQueryModel(ns)),
            File("backend/SharedRepository/PaginatedList.cs", SharedRepoPaginatedList(ns)),
            File("backend/SharedRepository/Interface/IRepository.cs", SharedRepoIRepository(ns)),
            File("backend/SharedRepository/Interface/IReadRepository.cs", SharedRepoIReadRepository(ns)),

            // SharedWeb
            File("backend/SharedWeb/Models/ErrorMessage.cs", SharedWebErrorMessage(ns)),
            File("backend/SharedWeb/Results/ValidationResult.cs", SharedWebValidationResult(ns)),
            File("backend/SharedWeb/Results/ValidResult.cs", SharedWebValidResult(ns)),
            File("backend/SharedWeb/Results/ValidObjectResult.cs", SharedWebValidObjectResult(ns)),
            File("backend/SharedWeb/Results/CreatedResult.cs", SharedWebCreatedResult(ns)),
            File("backend/SharedWeb/Results/InvalidValidationResult.cs", SharedWebInvalidValidationResult(ns)),
            File("backend/SharedWeb/Results/NotFoundValidationResult.cs", SharedWebNotFoundValidationResult(ns)),
            File("backend/SharedWeb/Results/UnauthorizedResult.cs", SharedWebUnauthorizedResult(ns)),
            File("backend/SharedWeb/Auth/Model/IUserContext.cs", SharedWebIUserContext(ns)),
            File("backend/SharedWeb/Auth/Model/UserContext.cs", SharedWebUserContext(ns)),
            File("backend/SharedWeb/Filters/BaseFilter.cs", SharedWebBaseFilter(ns)),
            File("backend/SharedWeb/Filters/CustomActionFilter.cs", SharedWebCustomActionFilter(ns)),
            File("backend/SharedWeb/Filters/CustomResultFilter.cs", SharedWebCustomResultFilter(ns)),
            File("backend/SharedWeb/StartupSetup.cs", SharedWebStartupSetup(ns)),
            File("backend/SharedWeb/StartupAppSetup.cs", SharedWebStartupAppSetup(ns)),

            // Services
            File("backend/Services/Contracts/IEmailService.cs", ServicesIEmailService(ns)),
            File("backend/Services/EmailService.cs", ServicesEmailService(ns)),

            // Models
            File("backend/Models/Entities/EntityBase.cs", ModelsEntityBase(ns)),

            // Repository
            File("backend/Repository/Common/SearchExpressionBuilder.cs", RepoSearchExpressionBuilder(ns)),
            File("backend/Repository/Config/BaseDbConfig.cs", RepoBaseDbConfig(ns)),
            File("backend/Repository/Context/AppDbContext.cs", RepoDbContext(ns, configuration)),
            File("backend/Repository/EfRepository.cs", RepoEfRepository(ns)),
            File("backend/Repository/StartupSetup.cs", RepoStartupSetup(ns, provider)),

            // Api Core
            File("backend/Api/Program.cs", ApiProgram(ns)),
            File("backend/Api/StartupSetup.cs", ApiStartupSetup(ns)),
            File("backend/Api/Common/ViewModels/PaginatedResponseModel.cs", ApiPaginatedResponseModel(ns)),
            File("backend/Api/Contracts/ApiResponse.cs", ApiResponse(ns)),
            File("backend/Repository/Migrations/20260101000000_InitialCreate.cs", Migration(ns, configuration, provider)),
            File("backend/UnitTests/Helpers/AutoFixtureHelper.cs", AutoFixtureHelperFile(ns)),
            File("backend/UnitTests/ValidationTests.cs", ValidationTests(ns, configuration))
        };

        foreach (var entity in configuration.Entities)
        {
            var p = NameFactory.Pluralize(entity.Name);

            // Models
            files.Add(File($"backend/Models/Entities/{entity.Name}.cs", EntityModel(ns, entity, configuration, provider)));

            // SharedRepository Filter
            files.Add(File($"backend/SharedRepository/FilterModels/{entity.Name}Filter.cs", EntityFilter(ns, entity)));

            // Repository Config & Implementation
            files.Add(File($"backend/Repository/Config/{entity.Name}Config.cs", EntityEfConfig(ns, entity, configuration, provider)));
            files.Add(File($"backend/Repository/IRepositories/I{entity.Name}Repository.cs", EntityIRepository(ns, entity)));
            files.Add(File($"backend/Repository/Repositories/{entity.Name}Repository.cs", EntityRepository(ns, entity, configuration)));

            // Api CQRS & Controller
            files.AddRange(CqrsFiles(ns, entity, configuration, provider));
            files.Add(File($"backend/Api/Controllers/{p}Controller.cs", EntityController(ns, entity, provider)));
            files.Add(File($"backend/UnitTests/ApiTests/Controllers/{p}ControllerTests.cs", EntityControllerTests(ns, entity)));
        }

        return files;
    }

    private static GeneratedFile File(string path, string contents) => new() { RelativePath = path, Contents = contents };

    private static string BuildDefaultConnectionString(DatabaseSettings settings, IDatabaseProvider provider)
    {
        return settings.Engine switch
        {
            DatabaseEngine.PostgreSql => $"Host=localhost;Port={settings.HostPort};Database={settings.DatabaseName};Username={settings.Username};Password={settings.Password}",
            DatabaseEngine.MySql => $"Server=localhost;Port={settings.HostPort};Database={settings.DatabaseName};User={settings.Username};Password={settings.Password}",
            DatabaseEngine.SqlServer => $"Server=localhost,{settings.HostPort};Database={settings.DatabaseName};User Id={settings.Username};Password={settings.Password};TrustServerCertificate=True",
            DatabaseEngine.Oracle => $"User Id={settings.Username};Password={settings.Password};Data Source=localhost:{settings.HostPort}/{OracleProvider.ResolveServiceName(settings.DatabaseName)}",
            _ => $"Host=localhost;Port={settings.HostPort};Database={settings.DatabaseName};Username={settings.Username};Password={settings.Password}"
        };
    }

    #region Csproj Definitions

    private static string ApiCsproj(string ns) => $"""
        <Project Sdk="Microsoft.NET.Sdk.Web">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <AssemblyName>{ns}.Api</AssemblyName>
            <RootNamespace>{ns}.Api</RootNamespace>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="MediatR" Version="12.4.1" />
            <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.12">
              <PrivateAssets>all</PrivateAssets>
              <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
            </PackageReference>
            <PackageReference Include="Swashbuckle.AspNetCore" Version="7.2.0" />
            <PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="10.0.12" />
          </ItemGroup>
          <ItemGroup>
            <ProjectReference Include="..\SharedWeb\SharedWeb.csproj" />
            <ProjectReference Include="..\Models\Models.csproj" />
            <ProjectReference Include="..\Repository\Repository.csproj" />
            <ProjectReference Include="..\Services\Services.csproj" />
          </ItemGroup>
        </Project>
        """;

    private static string ModelsCsproj(string ns) => $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <AssemblyName>{ns}.Models</AssemblyName>
            <RootNamespace>{ns}.Models</RootNamespace>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
        </Project>
        """;

    private static string RepositoryCsproj(string ns, IDatabaseProvider provider) => $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <AssemblyName>{ns}.Repository</AssemblyName>
            <RootNamespace>{ns}.Repository</RootNamespace>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.12" />
            <PackageReference Include="{provider.Metadata.EfCoreProviderPackage}" Version="{provider.Metadata.EfCoreProviderVersion}" />
            <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.12">
              <PrivateAssets>all</PrivateAssets>
              <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
            </PackageReference>
          </ItemGroup>
          <ItemGroup>
            <ProjectReference Include="..\SharedWeb\SharedWeb.csproj" />
            <ProjectReference Include="..\Models\Models.csproj" />
          </ItemGroup>
        </Project>
        """;

    private static string ServicesCsproj(string ns) => $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <AssemblyName>{ns}.Services</AssemblyName>
            <RootNamespace>{ns}.Services</RootNamespace>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
          <ItemGroup>
            <ProjectReference Include="..\Models\Models.csproj" />
          </ItemGroup>
        </Project>
        """;

    private static string SharedRepositoryCsproj(string ns) => $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <AssemblyName>{ns}.SharedRepository</AssemblyName>
            <RootNamespace>{ns}.SharedRepository</RootNamespace>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
        </Project>
        """;

    private static string SharedWebCsproj(string ns) => $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <AssemblyName>{ns}.SharedWeb</AssemblyName>
            <RootNamespace>{ns}.SharedWeb</RootNamespace>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
          <ItemGroup>
            <FrameworkReference Include="Microsoft.AspNetCore.App" />
            <PackageReference Include="AutoMapper" Version="14.0.0" />
            <PackageReference Include="FluentValidation" Version="11.11.0" />
            <PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
            <PackageReference Include="MediatR" Version="12.4.1" />
            <PackageReference Include="Swashbuckle.AspNetCore" Version="7.2.0" />
          </ItemGroup>
          <ItemGroup>
            <ProjectReference Include="..\SharedRepository\SharedRepository.csproj" />
          </ItemGroup>
        </Project>
        """;

    private static string TestsCsproj(string ns) => $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <AssemblyName>{ns}.UnitTests</AssemblyName>
            <RootNamespace>{ns}.UnitTests</RootNamespace>
            <Nullable>enable</Nullable>
            <IsPackable>false</IsPackable>
            <IsTestProject>true</IsTestProject>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="AutoFixture" Version="4.18.1" />
            <PackageReference Include="coverlet.collector" Version="6.0.4" />
            <PackageReference Include="MediatR" Version="12.4.1" />
            <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
            <PackageReference Include="Moq" Version="4.20.72" />
            <PackageReference Include="xunit" Version="2.9.3" />
            <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
          </ItemGroup>
          <ItemGroup>
            <Using Include="Xunit" />
          </ItemGroup>
          <ItemGroup>
            <ProjectReference Include="..\Api\Api.csproj" />
          </ItemGroup>
        </Project>
        """;

    private static string Dockerfile(string ns) => $$"""
        FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
        WORKDIR /src
        COPY . .
        RUN dotnet restore "Api/Api.csproj"
        RUN dotnet publish "Api/Api.csproj" -c Release -o /app --no-restore

        FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
        WORKDIR /app
        COPY --from=build /app .
        ENV ASPNETCORE_URLS=http://0.0.0.0:8080
        EXPOSE 8080
        USER $APP_UID
        ENTRYPOINT ["dotnet", "{{ns}}.Api.dll"]
        """;

    #endregion

    #region SharedRepository Source

    private static string SharedRepoPageQueryModel(string ns) => $$"""
        namespace {{ns}}.SharedRepository;

        public abstract class PageQueryModel
        {
            public int? Page { get; set; } = 1;
            public int? PageSize { get; set; } = 10;
            public string? Search { get; set; }
            public bool? All { get; set; }
            public string? SortCol { get; set; }
            public string? SortOrder { get; set; }
        }
        """;

    private static string SharedRepoPaginatedList(string ns) => $$"""
        namespace {{ns}}.SharedRepository;

        public class PaginatedList<T> : List<T>
        {
            public int Page { get; private set; }
            public int TotalPages { get; private set; }
            public int Total { get; private set; }

            public PaginatedList(List<T> items, int count, int page, int pageSize)
            {
                Page = page;
                Total = count;
                TotalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(count / (double)pageSize);
                AddRange(items);
            }
        }
        """;

    private static string SharedRepoIRepository(string ns) => $$"""
        namespace {{ns}}.SharedRepository.Interface;

        public interface IRepository<T> where T : class
        {
            Task<T?> AddAsync(T entity, CancellationToken cancellationToken = default);
            Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);
            Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default);
            Task<List<T>?> AddRangeAsync(List<T> entities, CancellationToken cancellationToken = default);
            Task<bool> UpdateRangeAsync(List<T> entities, CancellationToken cancellationToken = default);
        }
        """;

    private static string SharedRepoIReadRepository(string ns) => $$"""
        using System.Linq.Expressions;

        namespace {{ns}}.SharedRepository.Interface;

        public interface IReadRepository<T> where T : class
        {
            Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
            Task<PaginatedList<T>> GetPaginatedAsync(IQueryable<T> queryResult, PageQueryModel pageQueryModel);
            Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null, bool asNoTracking = false, CancellationToken cancellationToken = default);
            Task<T?> GetByIdAsync(object id, bool asNoTracking = false, CancellationToken cancellationToken = default);
            Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
            bool Exists(Expression<Func<T, bool>> predicate);
            IQueryable<T> ApplySorting(IQueryable<T> query, string? sortColumn, string? sortOrder, Dictionary<string, Expression<Func<T, object>>> sortExpressions);
        }
        """;

    #endregion

    #region SharedWeb Source

    private static string SharedWebErrorMessage(string ns) => $$"""
        namespace {{ns}}.SharedWeb.Models;

        public class ErrorMessage
        {
            public string Field { get; set; } = string.Empty;
            public List<string> Description { get; set; } = [];
        }
        """;

    private static string SharedWebValidationResult(string ns) => $$"""
        using {{ns}}.SharedWeb.Models;

        namespace {{ns}}.SharedWeb.Results;

        public abstract class ValidationResult
        {
            public IList<ErrorMessage> Errors { get; private set; } = new List<ErrorMessage>();

            public void AddError(string field, string description)
            {
                Errors.Add(new ErrorMessage
                {
                    Field = field,
                    Description = [description]
                });
            }
        }
        """;

    private static string SharedWebValidResult(string ns) => $$"""
        namespace {{ns}}.SharedWeb.Results;

        public class ValidResult : ValidationResult
        {
        }
        """;

    private static string SharedWebValidObjectResult(string ns) => $$"""
        namespace {{ns}}.SharedWeb.Results;

        public class ValidObjectResult : ValidationResult
        {
            public dynamic? Data { get; }

            public ValidObjectResult(dynamic? data)
            {
                Data = data;
            }
        }
        """;

    private static string SharedWebCreatedResult(string ns) => $$"""
        namespace {{ns}}.SharedWeb.Results;

        public class CreatedResult : ValidationResult
        {
            public dynamic? Data { get; }

            public CreatedResult(dynamic? data)
            {
                Data = data;
            }
        }
        """;

    private static string SharedWebInvalidValidationResult(string ns) => $$"""
        namespace {{ns}}.SharedWeb.Results;

        public class InvalidValidationResult : ValidationResult
        {
            public InvalidValidationResult(string field, string description)
            {
                AddError(field, description);
            }
        }
        """;

    private static string SharedWebNotFoundValidationResult(string ns) => $$"""
        namespace {{ns}}.SharedWeb.Results;

        public class NotFoundValidationResult : ValidationResult
        {
            public NotFoundValidationResult(string field, string description)
            {
                AddError(field, description);
            }
        }
        """;

    private static string SharedWebUnauthorizedResult(string ns) => $$"""
        namespace {{ns}}.SharedWeb.Results;

        public class UnauthorizedResult : ValidationResult
        {
            public UnauthorizedResult(string field, string description)
            {
                AddError(field, description);
            }
        }
        """;

    private static string SharedWebIUserContext(string ns) => $$"""
        namespace {{ns}}.SharedWeb.Auth.Model;

        public interface IUserContext
        {
            int UserId { get; set; }
        }
        """;

    private static string SharedWebUserContext(string ns) => $$"""
        namespace {{ns}}.SharedWeb.Auth.Model;

        public class UserContext : IUserContext
        {
            public int UserId { get; set; } = 1;
        }
        """;

    private static string SharedWebBaseFilter(string ns) => $$"""
        using System.Text.Json;
        using Microsoft.AspNetCore.Mvc;

        namespace {{ns}}.SharedWeb.Filters;

        public abstract class BaseFilter
        {
            protected ContentResult SerializeContent(object? data)
            {
                var content = JsonSerializer.Serialize(data);
                return new ContentResult
                {
                    Content = content,
                    ContentType = "application/json"
                };
            }
        }
        """;

    private static string SharedWebCustomActionFilter(string ns) => $$"""
        using System.Net;
        using Microsoft.AspNetCore.Mvc;
        using Microsoft.AspNetCore.Mvc.Filters;
        using Microsoft.Extensions.Logging;
        using {{ns}}.SharedWeb.Results;

        namespace {{ns}}.SharedWeb.Filters;

        public class CustomActionFilter(ILogger<CustomActionFilter> logger) : BaseFilter, IActionFilter
        {
            public void OnActionExecuting(ActionExecutingContext context)
            {
            }

            public void OnActionExecuted(ActionExecutedContext context)
            {
                if (context.Result is not ObjectResult objectContent)
                {
                    return;
                }

                if (objectContent.Value is NotFoundValidationResult notFound)
                {
                    SetResult(context, notFound.Errors, HttpStatusCode.NotFound);
                    return;
                }

                if (objectContent.Value is InvalidValidationResult invalid)
                {
                    SetResult(context, invalid.Errors, HttpStatusCode.BadRequest);
                    return;
                }

                if (objectContent.Value is {{ns}}.SharedWeb.Results.CreatedResult created)
                {
                    SetResult(context, created.Data, HttpStatusCode.Created);
                    return;
                }

                if (objectContent.Value is ValidObjectResult validObject)
                {
                    SetResult(context, validObject.Data, HttpStatusCode.OK);
                    return;
                }

                if (objectContent.Value is ValidResult)
                {
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.OK;
                    return;
                }
            }

            private void SetResult(ActionExecutedContext context, object? data, HttpStatusCode statusCode)
            {
                var result = SerializeContent(data);
                result.StatusCode = (int)statusCode;
                context.HttpContext.Response.StatusCode = (int)statusCode;
                context.Result = result;
            }
        }
        """;

    private static string SharedWebCustomResultFilter(string ns) => $$"""
        using System.Net;
        using Microsoft.AspNetCore.Mvc.Filters;
        using Microsoft.AspNetCore.Mvc.ModelBinding;
        using {{ns}}.SharedWeb.Models;

        namespace {{ns}}.SharedWeb.Filters;

        public class CustomResultFilter : BaseFilter, IResultFilter
        {
            public void OnResultExecuting(ResultExecutingContext context)
            {
                if (!context.ModelState.IsValid)
                {
                    var errors = TransformErrorMessages(context.ModelState);
                    var result = SerializeContent(errors);
                    result.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Result = result;
                }
            }

            public void OnResultExecuted(ResultExecutedContext context)
            {
            }

            public List<ErrorMessage> TransformErrorMessages(ModelStateDictionary modelState)
            {
                var errorMessages = new List<ErrorMessage>();
                foreach (var pair in modelState)
                {
                    if (pair.Value?.Errors != null && pair.Value.Errors.Count > 0)
                    {
                        errorMessages.Add(new ErrorMessage
                        {
                            Field = pair.Key,
                            Description = pair.Value.Errors.Select(c => c.ErrorMessage).ToList()
                        });
                    }
                }
                return errorMessages;
            }
        }
        """;

    private static string SharedWebStartupSetup(string ns) => $$"""
        using FluentValidation;
        using FluentValidation.AspNetCore;
        using Microsoft.AspNetCore.Builder;
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Logging;
        using Microsoft.OpenApi.Models;
        using {{ns}}.SharedWeb.Auth.Model;
        using {{ns}}.SharedWeb.Filters;

        namespace {{ns}}.SharedWeb;

        public static class StartupSetup
        {
            public static void ConfigureBuilder(WebApplicationBuilder builder, string applicationName)
            {
                builder.Logging.ClearProviders();
                builder.Logging.AddConsole();
            }

            public static void AddCommonDependencies(this IServiceCollection services, IConfiguration configuration, Type coreType, string swaggerTitle)
            {
                services.AddScoped<IUserContext, UserContext>();
                services.AddScoped<CustomActionFilter>();
                services.AddControllers(options =>
                {
                    options.Filters.Add<CustomActionFilter>();
                    options.Filters.Add<CustomResultFilter>();
                });

                services.AddEndpointsApiExplorer();
                services.AddSwaggerGen(c =>
                {
                    c.CustomSchemaIds(type => type.ToString());
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = swaggerTitle, Version = "v1" });
                });

                services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(coreType.Assembly));
                services.AddAutoMapper(coreType.Assembly);
                services.AddValidatorsFromAssembly(coreType.Assembly);
                services.AddFluentValidationAutoValidation();
            }
        }
        """;

    private static string SharedWebStartupAppSetup(string ns) => $$"""
        using Microsoft.AspNetCore.Builder;
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.Hosting;

        namespace {{ns}}.SharedWeb;

        public static class StartupAppSetup
        {
            public static void ConfigureApp(WebApplication app, string applicationTitle)
            {
                if (app.Environment.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{applicationTitle} V1");
                });

                app.UseRouting();

                var corsOrigins = app.Configuration.GetSection("AllowedCORSOrigins").Get<string[]>() ?? ["http://localhost:3000", "http://127.0.0.1:3000"];
                app.UseCors(policy =>
                {
                    policy.WithOrigins(corsOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });

                app.MapControllers();
                app.MapHealthChecks("/health");
            }
        }
        """;

    #endregion

    #region Services Source

    private static string ServicesIEmailService(string ns) => $$"""
        namespace {{ns}}.Services.Contracts;

        public interface IEmailService
        {
            Task<bool> SendEmailAsync(string to, string subject, string body);
        }
        """;

    private static string ServicesEmailService(string ns) => $$"""
        using {{ns}}.Services.Contracts;

        namespace {{ns}}.Services;

        public class EmailService : IEmailService
        {
            public Task<bool> SendEmailAsync(string to, string subject, string body)
            {
                return Task.FromResult(true);
            }
        }
        """;

    #endregion

    #region Models Source

    private static string ModelsEntityBase(string ns) => $$"""
        namespace {{ns}}.Models.Entities;

        public abstract class EntityBase
        {
            public int CreatedBy { get; set; } = 1;
            public int? ModifiedBy { get; set; }
            public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
            public DateTime? ModifiedOn { get; set; }
            public bool IsDeleted { get; set; } = false;
        }
        """;

    private static string EntityModel(string ns, EntityDefinition entity, ProjectConfiguration configuration, IDatabaseProvider provider)
    {
        var props = new StringBuilder();
        foreach (var column in entity.Columns.OrderBy(c => c.Ordinal))
        {
            var clr = Clr.TypeName(column, provider);
            if (!column.IsNullable && clr == "string")
            {
                props.AppendLine($"    public string {column.Name} {{ get; set; }} = string.Empty;");
            }
            else if (!column.IsNullable && clr == "byte[]")
            {
                props.AppendLine($"    public byte[] {column.Name} {{ get; set; }} = [];");
            }
            else
            {
                props.AppendLine($"    public {clr} {column.Name} {{ get; set; }}");
            }
        }

        foreach (var column in entity.Columns.Where(c => c.ForeignKeyTargetEntityId is not null))
        {
            var target = configuration.Entities.FirstOrDefault(e => e.Id == column.ForeignKeyTargetEntityId);
            if (target != null)
            {
                props.AppendLine($"    public virtual {target.Name}? {target.Name} {{ get; set; }}");
            }
        }

        return $$"""
            namespace {{ns}}.Models.Entities;

            public class {{entity.Name}} : EntityBase
            {
            {{props.ToString().TrimEnd()}}
            }
            """;
    }

    private static string EntityFilter(string ns, EntityDefinition entity)
    {
        var props = new StringBuilder();
        foreach (var column in entity.Columns.Where(c => !c.IsPrimaryKey))
        {
            props.AppendLine($"    public List<string>? {column.Name} {{ get; set; }}");
        }

        return $$"""
            namespace {{ns}}.SharedRepository.FilterModels;

            public class {{entity.Name}}Filter
            {
            {{props.ToString().TrimEnd()}}
            }
            """;
    }

    #endregion

    #region Repository Source

    private static string RepoSearchExpressionBuilder(string ns) => $$"""
        using System.Linq.Expressions;

        namespace {{ns}}.Repository.Common;

        public class SearchExpressionBuilder<T>
        {
            public Expression<Func<T, bool>> BuildOrSearchExpression(List<Expression<Func<T, bool>>> expressions)
            {
                if (expressions == null || !expressions.Any())
                {
                    return x => true;
                }

                if (expressions.Count == 1)
                {
                    return expressions[0];
                }

                var parameter = expressions[0].Parameters[0];
                var orExpression = expressions.Skip(1)
                    .Aggregate(
                        expressions[0].Body,
                        (acc, expr) => Expression.OrElse(acc, Expression.Invoke(expr, parameter)));

                return Expression.Lambda<Func<T, bool>>(orExpression, parameter);
            }
        }
        """;

    private static string RepoBaseDbConfig(string ns) => $$"""
        using Microsoft.EntityFrameworkCore;
        using Microsoft.EntityFrameworkCore.Metadata.Builders;
        using {{ns}}.Models.Entities;

        namespace {{ns}}.Repository.Config;

        public class BaseConfig<T> where T : EntityBase
        {
            public void BaseConfigure(EntityTypeBuilder<T> builder)
            {
                builder.HasQueryFilter(e => !e.IsDeleted);
            }
        }
        """;

    private static string RepoDbContext(string ns, ProjectConfiguration configuration)
    {
        var sets = new StringBuilder();
        foreach (var entity in configuration.Entities)
        {
            sets.AppendLine($"    public DbSet<{entity.Name}> {NameFactory.Pluralize(entity.Name)} {{ get; set; }} = null!;");
        }

        return $$"""
            using Microsoft.EntityFrameworkCore;
            using {{ns}}.Models.Entities;
            using {{ns}}.SharedWeb.Auth.Model;

            namespace {{ns}}.Repository.Context;

            public class AppDbContext : DbContext
            {
                private readonly IUserContext? _userContext;

                public AppDbContext(DbContextOptions<AppDbContext> options, IUserContext? userContext = null)
                    : base(options)
                {
                    _userContext = userContext;
                }

            {{sets.ToString().TrimEnd()}}

                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
                    base.OnModelCreating(modelBuilder);
                }

                public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
                {
                    UpdateAuditFields();
                    return base.SaveChangesAsync(cancellationToken);
                }

                public override int SaveChanges()
                {
                    UpdateAuditFields();
                    return base.SaveChanges();
                }

                private void UpdateAuditFields()
                {
                    var userId = _userContext?.UserId ?? 1;
                    foreach (var entry in ChangeTracker.Entries<EntityBase>())
                    {
                        switch (entry.State)
                        {
                            case EntityState.Added:
                                entry.Entity.CreatedBy = userId;
                                entry.Entity.CreatedOn = DateTime.UtcNow;
                                entry.Entity.ModifiedBy = userId;
                                entry.Entity.ModifiedOn = DateTime.UtcNow;
                                break;
                            case EntityState.Modified:
                                entry.Entity.ModifiedBy = userId;
                                entry.Entity.ModifiedOn = DateTime.UtcNow;
                                break;
                            case EntityState.Deleted:
                                entry.State = EntityState.Modified;
                                entry.Entity.IsDeleted = true;
                                entry.Entity.ModifiedBy = userId;
                                entry.Entity.ModifiedOn = DateTime.UtcNow;
                                break;
                        }
                    }
                }
            }
            """;
    }

    private static string RepoEfRepository(string ns) => $$"""
        using System.Linq.Expressions;
        using Microsoft.EntityFrameworkCore;
        using {{ns}}.Repository.Context;
        using {{ns}}.SharedRepository;
        using {{ns}}.SharedRepository.Interface;

        namespace {{ns}}.Repository;

        public class EfRepository<T> : IReadRepository<T>, IRepository<T> where T : class
        {
            private readonly DbSet<T> _entity;
            private readonly AppDbContext _dbContext;

            public EfRepository(AppDbContext dbContext)
            {
                _dbContext = dbContext;
                _entity = dbContext.Set<T>();
            }

            public async Task<T?> AddAsync(T entity, CancellationToken cancellationToken = default)
            {
                var entry = await _entity.AddAsync(entity, cancellationToken);
                var saved = await _dbContext.SaveChangesAsync(cancellationToken);
                return saved > 0 ? entry.Entity : null;
            }

            public async Task<List<T>?> AddRangeAsync(List<T> entities, CancellationToken cancellationToken = default)
            {
                await _entity.AddRangeAsync(entities, cancellationToken);
                var saved = await _dbContext.SaveChangesAsync(cancellationToken);
                return saved > 0 ? entities : null;
            }

            public async Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default)
            {
                _entity.Update(entity);
                return await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            }

            public async Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default)
            {
                _entity.Remove(entity);
                return await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            }

            public async Task<bool> UpdateRangeAsync(List<T> entities, CancellationToken cancellationToken = default)
            {
                _entity.UpdateRange(entities);
                return await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            }

            public async Task<PaginatedList<T>> GetPaginatedAsync(IQueryable<T> queryResult, PageQueryModel pageQueryModel)
            {
                var page = pageQueryModel.Page ?? 1;
                var pageSize = pageQueryModel.PageSize ?? 10;
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;

                var count = await queryResult.CountAsync();
                var items = await queryResult.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
                return new PaginatedList<T>(items, count, page, pageSize);
            }

            public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null, bool asNoTracking = false, CancellationToken cancellationToken = default)
            {
                var query = _entity.AsQueryable();
                if (asNoTracking) query = query.AsNoTracking();
                if (predicate != null) query = query.Where(predicate);
                return await query.ToListAsync(cancellationToken);
            }

            public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
            {
                return await _entity.AnyAsync(predicate, cancellationToken);
            }

            public async Task<T?> GetByIdAsync(object id, bool asNoTracking = false, CancellationToken cancellationToken = default)
            {
                return await _entity.FindAsync([id], cancellationToken);
            }

            public async Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
            {
                return await _entity.FirstOrDefaultAsync(predicate, cancellationToken);
            }

            public bool Exists(Expression<Func<T, bool>> predicate)
            {
                return _entity.Any(predicate);
            }

            public IQueryable<T> ApplySorting(IQueryable<T> query, string? sortColumn, string? sortOrder, Dictionary<string, Expression<Func<T, object>>> sortExpressions)
            {
                if (!string.IsNullOrWhiteSpace(sortColumn) && sortExpressions.TryGetValue(sortColumn.ToLower(), out var expr))
                {
                    return string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase)
                        ? query.OrderByDescending(expr)
                        : query.OrderBy(expr);
                }
                return query;
            }
        }
        """;

    private static string RepoStartupSetup(string ns, IDatabaseProvider provider)
    {
        var useMethod = provider.Metadata.Engine switch
        {
            DatabaseEngine.PostgreSql => "UseNpgsql",
            DatabaseEngine.MySql => "UseMySQL",
            DatabaseEngine.SqlServer => "UseSqlServer",
            DatabaseEngine.Oracle => "UseOracle",
            _ => "UseNpgsql"
        };

        var dbCall = $"options.{useMethod}(cs)";

        return $$"""
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.DependencyInjection;
            using {{ns}}.Repository.Context;
            using {{ns}}.SharedRepository.Interface;

            namespace {{ns}}.Repository;

            public static class StartupSetup
            {
                public static void AddDbContext(this IServiceCollection services, IConfiguration configuration)
                {
                    var cs = configuration["DefaultConnection"] 
                        ?? configuration.GetConnectionString("Default")
                        ?? throw new InvalidOperationException("Database connection string 'DefaultConnection' or 'ConnectionStrings:Default' is required.");

                    services.AddDbContext<AppDbContext>(options =>
                    {
                        {{dbCall}};
                    });

                    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
                    services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));
                    AddEntityRepositories(services);
                }

                public static void AddEntityRepositories(IServiceCollection services)
                {
                    var assembly = typeof(StartupSetup).Assembly;
                    foreach (var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Repository")))
                    {
                        var iface = type.GetInterfaces().FirstOrDefault(i => i.Name == "I" + type.Name);
                        if (iface != null)
                        {
                            services.AddScoped(iface, type);
                        }
                    }
                }
            }
            """;
    }

    private static string EntityEfConfig(string ns, EntityDefinition entity, ProjectConfiguration configuration, IDatabaseProvider provider)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"using Microsoft.EntityFrameworkCore;");
        sb.AppendLine($"using Microsoft.EntityFrameworkCore.Metadata.Builders;");
        sb.AppendLine($"using {ns}.Models.Entities;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns}.Repository.Config;");
        sb.AppendLine();
        sb.AppendLine($"public class {entity.Name}Config : BaseConfig<{entity.Name}>, IEntityTypeConfiguration<{entity.Name}>");
        sb.AppendLine("{");
        sb.AppendLine($"    public void Configure(EntityTypeBuilder<{entity.Name}> builder)");
        sb.AppendLine("    {");
        sb.AppendLine("        BaseConfigure(builder);");
        sb.AppendLine($"        builder.ToTable(\"{entity.Name}\");");
        var pk = entity.Columns.Single(c => c.IsPrimaryKey);
        sb.AppendLine($"        builder.HasKey(x => x.{pk.Name});");

        foreach (var column in entity.Columns)
        {
            var type = provider.GetType(column.LogicalType)!;
            sb.Append($"        builder.Property(x => x.{column.Name})");
            if (type.SupportsLength && column.Length is int length)
            {
                sb.Append($".HasMaxLength({length})");
            }
            if (type.SupportsPrecision && column.Precision is int precision)
            {
                sb.Append($".HasPrecision({precision}, {column.Scale ?? 0})");
            }
            if (!column.IsNullable)
            {
                sb.Append(".IsRequired()");
            }
            if (column.Identity == IdentityStrategy.DatabaseIdentity)
            {
                sb.Append(".ValueGeneratedOnAdd()");
            }
            sb.AppendLine(";");

            if (column.IsUnique && !column.IsPrimaryKey)
            {
                sb.AppendLine($"        builder.HasIndex(x => x.{column.Name}).IsUnique();");
            }
            else if (column.HasIndex && !column.IsPrimaryKey)
            {
                sb.AppendLine($"        builder.HasIndex(x => x.{column.Name});");
            }

            if (column.ForeignKeyTargetEntityId is Guid targetId)
            {
                var target = configuration.Entities.FirstOrDefault(e => e.Id == targetId);
                if (target != null)
                {
                    var delete = column.OnDelete switch
                    {
                        ForeignKeyDeleteBehavior.Cascade => "DeleteBehavior.Cascade",
                        ForeignKeyDeleteBehavior.SetNull => "DeleteBehavior.SetNull",
                        ForeignKeyDeleteBehavior.NoAction => "DeleteBehavior.NoAction",
                        _ => "DeleteBehavior.Restrict"
                    };
                    sb.AppendLine($"        builder.HasOne(x => x.{target.Name}).WithMany().HasForeignKey(x => x.{column.Name}).OnDelete({delete});");
                }
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string EntityIRepository(string ns, EntityDefinition entity)
    {
        return $$"""
            using {{ns}}.Models.Entities;
            using {{ns}}.SharedRepository;
            using {{ns}}.SharedRepository.FilterModels;

            namespace {{ns}}.Repository.IRepositories;

            public interface I{{entity.Name}}Repository
            {
                Task<{{entity.Name}}?> Get{{entity.Name}}ById(object id);
                Task<List<{{entity.Name}}>> GetAll{{NameFactory.Pluralize(entity.Name)}}();
                Task<PaginatedList<{{entity.Name}}>> GetPaginated{{NameFactory.Pluralize(entity.Name)}}(PageQueryModel pageQuery, {{entity.Name}}Filter filters);
            }
            """;
    }

    private static string EntityRepository(string ns, EntityDefinition entity, ProjectConfiguration configuration)
    {
        var p = NameFactory.Pluralize(entity.Name);
        var pk = entity.Columns.Single(c => c.IsPrimaryKey);
        var stringCols = entity.Columns.Where(c => c.LogicalType is "string" or "text").ToList();

        var searchConditions = stringCols.Count > 0
            ? string.Join(" || " + Environment.NewLine + "                    ", stringCols.Select(c => $"(a.{c.Name} != null && a.{c.Name}.ToLower().Contains(searchTerm))"))
            : "false";

        var includes = new StringBuilder();
        foreach (var col in entity.Columns.Where(c => c.ForeignKeyTargetEntityId is not null))
        {
            var target = configuration.Entities.FirstOrDefault(e => e.Id == col.ForeignKeyTargetEntityId);
            if (target != null)
            {
                includes.AppendLine($"                                .Include(a => a.{target.Name})");
            }
        }

        var sortDictEntries = new StringBuilder();
        foreach (var col in entity.Columns)
        {
            sortDictEntries.AppendLine($"                {{ \"{col.Name.ToLower()}\", a => a.{col.Name}! }},");
        }

        return $$"""
            using System.Linq.Expressions;
            using Microsoft.EntityFrameworkCore;
            using {{ns}}.Models.Entities;
            using {{ns}}.Repository.Common;
            using {{ns}}.Repository.Context;
            using {{ns}}.Repository.IRepositories;
            using {{ns}}.SharedRepository;
            using {{ns}}.SharedRepository.FilterModels;
            using {{ns}}.SharedRepository.Interface;

            namespace {{ns}}.Repository.Repositories;

            public class {{entity.Name}}Repository : I{{entity.Name}}Repository
            {
                private readonly AppDbContext _dbContext;
                private readonly IReadRepository<{{entity.Name}}> _readRepository;
                private readonly SearchExpressionBuilder<{{entity.Name}}> _expressionBuilder;

                public {{entity.Name}}Repository(AppDbContext dbContext, IReadRepository<{{entity.Name}}> readRepository)
                {
                    _dbContext = dbContext;
                    _readRepository = readRepository;
                    _expressionBuilder = new SearchExpressionBuilder<{{entity.Name}}>();
                }

                public async Task<List<{{entity.Name}}>> GetAll{{p}}()
                {
                    return await _dbContext.{{p}}
            {{includes.ToString().TrimEnd()}}
                                .OrderByDescending(a => a.CreatedOn)
                                .ToListAsync();
                }

                public async Task<{{entity.Name}}?> Get{{entity.Name}}ById(object id)
                {
                    return await _dbContext.{{p}}
            {{includes.ToString().TrimEnd()}}
                                .FirstOrDefaultAsync(a => a.{{pk.Name}}.Equals(id));
                }

                public async Task<PaginatedList<{{entity.Name}}>> GetPaginated{{p}}(PageQueryModel pageQuery, {{entity.Name}}Filter filters)
                {
                    var query = _dbContext.{{p}}
            {{includes.ToString().TrimEnd()}}
                                .AsQueryable();

                    if (!string.IsNullOrWhiteSpace(pageQuery.Search))
                    {
                        string searchTerm = pageQuery.Search.ToLower();
                        query = query.Where(a => {{searchConditions}});
                    }

                    query = Sort{{p}}(query, pageQuery.SortCol, pageQuery.SortOrder);
                    return await _readRepository.GetPaginatedAsync(query, pageQuery);
                }

                private IQueryable<{{entity.Name}}> Sort{{p}}(IQueryable<{{entity.Name}}> query, string? sortBy, string? sortOrder)
                {
                    var sortExpressions = new Dictionary<string, Expression<Func<{{entity.Name}}, object>>>
                    {
            {{sortDictEntries.ToString().TrimEnd()}}
                    };
                    return _readRepository.ApplySorting(query, sortBy, sortOrder, sortExpressions);
                }
            }
            """;
    }

    #endregion

    #region Api CQRS & Controller Source

    private static string ApiProgram(string ns) => $$"""
        using Microsoft.EntityFrameworkCore;
        using {{ns}}.Repository;
        using {{ns}}.Repository.Context;
        using {{ns}}.SharedWeb;

        var builder = WebApplication.CreateBuilder(args);

        {{ns}}.SharedWeb.StartupSetup.ConfigureBuilder(builder, "{{ns}}Management-api");
        builder.Services.AddCommonDependencies(builder.Configuration, typeof({{ns}}.Api.StartupSetup), "{{ns}} API");
        builder.Services.AddDbContext(builder.Configuration);
        builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

        var app = builder.Build();

        {{ns}}.SharedWeb.StartupAppSetup.ConfigureApp(app, "{{ns}} Management");

        if (app.Configuration.GetValue("Database:ApplyMigrations", false))
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
        }

        await app.RunAsync();

        public partial class Program;
        """;

    private static string ApiStartupSetup(string ns) => $$"""
        namespace {{ns}}.Api;

        public class StartupSetup
        {
        }
        """;

    private static string ApiPaginatedResponseModel(string ns) => $$"""
        namespace {{ns}}.Api.Common.ViewModels;

        public class PaginatedResponseModel<T>
        {
            public int Page { get; set; }
            public int Total { get; set; }
            public int TotalPages { get; set; }
            public List<T> Data { get; set; } = [];
        }
        """;

    private static string ApiResponse(string ns) => $$"""
        namespace {{ns}}.Api.Contracts;

        public sealed record ApiResponse<T>(bool Success, T? Data, string? Error, string? Code);
        """;

    private static IEnumerable<GeneratedFile> CqrsFiles(string ns, EntityDefinition entity, ProjectConfiguration configuration, IDatabaseProvider provider)
    {
        var p = NameFactory.Pluralize(entity.Name);
        var pk = entity.Columns.Single(c => c.IsPrimaryKey);
        var pkClr = Clr.TypeName(new ColumnDefinition { LogicalType = pk.LogicalType, IsNullable = false }, provider);
        var writable = entity.Columns.Where(c => !c.IsPrimaryKey || c.Identity == IdentityStrategy.None).ToList();

        // 1. ResponseModel
        var respProps = new StringBuilder();
        foreach (var col in entity.Columns)
        {
            respProps.AppendLine($"    public {Clr.TypeName(col, provider)} {col.Name} {{ get; set; }}");
        }
        yield return File($"backend/Api/CQRS/{entity.Name}/Shared/ResponseModel.cs", $$"""
            namespace {{ns}}.Api.{{entity.Name}}.Shared;

            public class ResponseModel
            {
            {{respProps.ToString().TrimEnd()}}
            }
            """);

        // 2. MapperProfile
        yield return File($"backend/Api/CQRS/{entity.Name}/MapperProfile.cs", $$"""
            using AutoMapper;
            using {{ns}}.Api.Common.ViewModels;
            using {{ns}}.Api.{{entity.Name}}.Shared;
            using {{ns}}.Models.Entities;
            using {{ns}}.SharedRepository;

            namespace {{ns}}.Api.{{entity.Name}};

            public class MapperProfile : Profile
            {
                public MapperProfile()
                {
                    CreateMap<{{ns}}.Models.Entities.{{entity.Name}}, ResponseModel>(MemberList.None);
                    CreateMap<PaginatedList<{{ns}}.Models.Entities.{{entity.Name}}>, PaginatedResponseModel<ResponseModel>>(MemberList.None)
                        .ForMember(dest => dest.Data, opt => opt.MapFrom(src => src));
                }
            }
            """);

        // 3. Base Command Model, Validator, Handler
        var baseProps = new StringBuilder();
        foreach (var col in writable)
        {
            baseProps.AppendLine($"    public {Clr.TypeName(col, provider)} {col.Name} {{ get; set; }}");
        }

        var validationRules = new StringBuilder();
        foreach (var col in writable.Where(c => c.LogicalType is "string" or "text" && !c.IsNullable))
        {
            var maxLen = col.Length is int len ? $".MaximumLength({len})" : "";
            validationRules.AppendLine($"        RuleFor(x => x.{col.Name}).NotEmpty().NotNull(){maxLen};");
        }

        var assignValues = new StringBuilder();
        foreach (var col in writable)
        {
            assignValues.AppendLine($"        target.{col.Name} = request.{col.Name};");
        }

        yield return File($"backend/Api/CQRS/{entity.Name}/Commands/Shared/{entity.Name}BaseCommandModel.cs", $$"""
            namespace {{ns}}.Api.{{entity.Name}}.Commands.Shared;

            public abstract class {{entity.Name}}BaseCommandModel
            {
            {{baseProps.ToString().TrimEnd()}}
            }
            """);

        yield return File($"backend/Api/CQRS/{entity.Name}/Commands/Shared/{entity.Name}BaseValidator.cs", $$"""
            using FluentValidation;

            namespace {{ns}}.Api.{{entity.Name}}.Commands.Shared;

            public class {{entity.Name}}BaseValidator<T> : AbstractValidator<T> where T : {{entity.Name}}BaseCommandModel
            {
                public {{entity.Name}}BaseValidator()
                {
            {{validationRules.ToString().TrimEnd()}}
                }
            }
            """);

        yield return File($"backend/Api/CQRS/{entity.Name}/Commands/Shared/{entity.Name}BaseHandler.cs", $$"""
            using {{ns}}.Models.Entities;

            namespace {{ns}}.Api.{{entity.Name}}.Commands.Shared;

            public abstract class {{entity.Name}}BaseHandler
            {
                protected void AssignValues({{entity.Name}}BaseCommandModel request, {{ns}}.Models.Entities.{{entity.Name}} target)
                {
            {{assignValues.ToString().TrimEnd()}}
                }
            }
            """);

        // 4. Create Command (Adtech1 structure + wrapper for test compatibility)
        var createGuidInit = pk.Identity == IdentityStrategy.Uuid ? $"        target.{pk.Name} = Guid.NewGuid();\n" : "";
        yield return File($"backend/Api/CQRS/{entity.Name}/Commands/Create/CommandModel.cs", $$"""
            using MediatR;
            using {{ns}}.Api.{{entity.Name}}.Commands.Shared;
            using {{ns}}.SharedWeb.Results;

            namespace {{ns}}.Api.{{entity.Name}}.Commands.Create;

            public class CommandModel : {{entity.Name}}BaseCommandModel, IRequest<ValidationResult>
            {
            }
            """);

        yield return File($"backend/Api/CQRS/{entity.Name}/Commands/Create/Validator.cs", $$"""
            using {{ns}}.Api.{{entity.Name}}.Commands.Shared;

            namespace {{ns}}.Api.{{entity.Name}}.Commands.Create;

            public class Validator : {{entity.Name}}BaseValidator<CommandModel>
            {
            }
            """);

        yield return File($"backend/Api/CQRS/{entity.Name}/Commands/Create/Handler.cs", $$"""
            using MediatR;
            using Microsoft.Extensions.Logging;
            using {{ns}}.Api.{{entity.Name}}.Commands.Shared;
            using {{ns}}.Models.Entities;
            using {{ns}}.SharedRepository.Interface;
            using {{ns}}.SharedWeb.Results;

            namespace {{ns}}.Api.{{entity.Name}}.Commands.Create;

            public class Handler : {{entity.Name}}BaseHandler, IRequestHandler<CommandModel, ValidationResult>
            {
                private readonly IRepository<{{ns}}.Models.Entities.{{entity.Name}}> _repository;
                private readonly ILogger<Handler> _logger;

                public Handler(IRepository<{{ns}}.Models.Entities.{{entity.Name}}> repository, ILogger<Handler> logger)
                {
                    _repository = repository;
                    _logger = logger;
                }

                public async Task<ValidationResult> Handle(CommandModel request, CancellationToken cancellationToken)
                {
                    _logger.LogInformation("Creating {Entity}", "{{entity.Name}}");
                    var target = new {{ns}}.Models.Entities.{{entity.Name}}();
            {{createGuidInit}}        AssignValues(request, target);

                    var created = await _repository.AddAsync(target, cancellationToken);
                    if (created == null)
                    {
                        _logger.LogError("Unable to save {Entity}", "{{entity.Name}}");
                        return new InvalidValidationResult("Summary", "There was a server error saving '{{entity.Name}}'");
                    }

                    _logger.LogInformation("Created {Entity} successfully", "{{entity.Name}}");
                    return new CreatedResult(created.{{pk.Name}});
                }
            }
            """);

        yield return File($"backend/Api/CQRS/{entity.Name}/Commands/Update/CommandModel.cs", $$"""
            using MediatR;
            using {{ns}}.Api.{{entity.Name}}.Commands.Shared;
            using {{ns}}.SharedWeb.Results;

            namespace {{ns}}.Api.{{entity.Name}}.Commands.Update;

            public class CommandModel : {{entity.Name}}BaseCommandModel, IRequest<ValidationResult>
            {
                public {{pkClr}} {{pk.Name}} { get; set; }
            }
            """);

        yield return File($"backend/Api/CQRS/{entity.Name}/Commands/Update/Validator.cs", $$"""
            using FluentValidation;
            using {{ns}}.Api.{{entity.Name}}.Commands.Shared;

            namespace {{ns}}.Api.{{entity.Name}}.Commands.Update;

            public class Validator : {{entity.Name}}BaseValidator<CommandModel>
            {
                public Validator()
                {
                    RuleFor(x => x.{{pk.Name}}).NotEmpty().NotNull().When(x => !Equals(x.{{pk.Name}}, default));
                }
            }
            """);

        yield return File($"backend/Api/CQRS/{entity.Name}/Commands/Update/Handler.cs", $$"""
            using AutoMapper;
            using MediatR;
            using Microsoft.Extensions.Logging;
            using {{ns}}.Api.{{entity.Name}}.Commands.Shared;
            using {{ns}}.Api.{{entity.Name}}.Shared;
            using {{ns}}.Models.Entities;
            using {{ns}}.SharedRepository.Interface;
            using {{ns}}.SharedWeb.Results;

            namespace {{ns}}.Api.{{entity.Name}}.Commands.Update;

            public class Handler : {{entity.Name}}BaseHandler, IRequestHandler<CommandModel, ValidationResult>
            {
                private readonly IReadRepository<{{ns}}.Models.Entities.{{entity.Name}}> _readRepository;
                private readonly IRepository<{{ns}}.Models.Entities.{{entity.Name}}> _repository;
                private readonly IMapper _mapper;
                private readonly ILogger<Handler> _logger;

                public Handler(IReadRepository<{{ns}}.Models.Entities.{{entity.Name}}> readRepository, IRepository<{{ns}}.Models.Entities.{{entity.Name}}> repository, IMapper mapper, ILogger<Handler> logger)
                {
                    _readRepository = readRepository;
                    _repository = repository;
                    _mapper = mapper;
                    _logger = logger;
                }

                public async Task<ValidationResult> Handle(CommandModel request, CancellationToken cancellationToken)
                {
                    _logger.LogInformation("Updating {Entity} with Id: {Id}", "{{entity.Name}}", request.{{pk.Name}});
                    var entity = await _readRepository.GetByIdAsync(request.{{pk.Name}}, cancellationToken: cancellationToken);
                    if (entity == null)
                    {
                        return new NotFoundValidationResult("{{pk.Name}}", "Invalid {{entity.Name}} Id");
                    }

                    AssignValues(request, entity);
                    var updated = await _repository.UpdateAsync(entity, cancellationToken);
                    if (!updated)
                    {
                        return new InvalidValidationResult("Summary", "There was a server error updating '{{entity.Name}}'");
                    }

                    var refreshed = await _readRepository.GetByIdAsync(request.{{pk.Name}}, asNoTracking: true, cancellationToken: cancellationToken);
                    return new ValidObjectResult(_mapper.Map<ResponseModel>(refreshed));
                }
            }
            """);

        // 6. Delete Command
        yield return File($"backend/Api/CQRS/{entity.Name}/Commands/Delete/CommandModel.cs", $$"""
            using MediatR;
            using {{ns}}.SharedWeb.Results;

            namespace {{ns}}.Api.{{entity.Name}}.Commands.Delete;

            public class CommandModel : IRequest<ValidationResult>
            {
                public {{pkClr}} {{pk.Name}} { get; set; }
            }
            """);

        yield return File($"backend/Api/CQRS/{entity.Name}/Commands/Delete/Handler.cs", $$"""
            using MediatR;
            using Microsoft.Extensions.Logging;
            using {{ns}}.Models.Entities;
            using {{ns}}.SharedRepository.Interface;
            using {{ns}}.SharedWeb.Results;

            namespace {{ns}}.Api.{{entity.Name}}.Commands.Delete;

            public class Handler : IRequestHandler<CommandModel, ValidationResult>
            {
                private readonly IReadRepository<{{ns}}.Models.Entities.{{entity.Name}}> _readRepository;
                private readonly IRepository<{{ns}}.Models.Entities.{{entity.Name}}> _repository;
                private readonly ILogger<Handler> _logger;

                public Handler(IReadRepository<{{ns}}.Models.Entities.{{entity.Name}}> readRepository, IRepository<{{ns}}.Models.Entities.{{entity.Name}}> repository, ILogger<Handler> logger)
                {
                    _readRepository = readRepository;
                    _repository = repository;
                    _logger = logger;
                }

                public async Task<ValidationResult> Handle(CommandModel request, CancellationToken cancellationToken)
                {
                    _logger.LogInformation("Deleting {Entity} with Id: {Id}", "{{entity.Name}}", request.{{pk.Name}});
                    var entity = await _readRepository.GetByIdAsync(request.{{pk.Name}}, cancellationToken: cancellationToken);
                    if (entity == null)
                    {
                        return new NotFoundValidationResult("{{pk.Name}}", "Invalid {{entity.Name}} Id");
                    }

                    var deleted = await _repository.DeleteAsync(entity, cancellationToken);
                    if (!deleted)
                    {
                        return new InvalidValidationResult("Summary", "There was a server error deleting '{{entity.Name}}'");
                    }

                    return new ValidResult();
                }
            }
            """);

        // 7. GetById Query (Adtech1 structure + wrapper for test compatibility)
        yield return File($"backend/Api/CQRS/{entity.Name}/Queries/GetById/QueryModel.cs", $$"""
            using MediatR;
            using {{ns}}.SharedWeb.Results;

            namespace {{ns}}.Api.{{entity.Name}}.Queries.GetById;

            public class QueryModel : IRequest<ValidationResult>
            {
                public {{pkClr}} {{pk.Name}} { get; set; }
            }
            """);

        yield return File($"backend/Api/CQRS/{entity.Name}/Queries/GetById/Validator.cs", $$"""
            using FluentValidation;

            namespace {{ns}}.Api.{{entity.Name}}.Queries.GetById;

            public class Validator : AbstractValidator<QueryModel>
            {
                public Validator()
                {
                    RuleFor(x => x.{{pk.Name}}).NotEmpty().NotNull().When(x => !Equals(x.{{pk.Name}}, default));
                }
            }
            """);

        yield return File($"backend/Api/CQRS/{entity.Name}/Queries/GetById/Handler.cs", $$"""
            using AutoMapper;
            using MediatR;
            using Microsoft.Extensions.Logging;
            using {{ns}}.Api.{{entity.Name}}.Shared;
            using {{ns}}.Repository.IRepositories;
            using {{ns}}.SharedWeb.Results;

            namespace {{ns}}.Api.{{entity.Name}}.Queries.GetById;

            public class Handler : IRequestHandler<QueryModel, ValidationResult>
            {
                private readonly I{{entity.Name}}Repository _repository;
                private readonly IMapper _mapper;
                private readonly ILogger<Handler> _logger;

                public Handler(I{{entity.Name}}Repository repository, IMapper mapper, ILogger<Handler> logger)
                {
                    _repository = repository;
                    _mapper = mapper;
                    _logger = logger;
                }

                public async Task<ValidationResult> Handle(QueryModel query, CancellationToken cancellationToken)
                {
                    _logger.LogInformation("Fetching {Entity} with Id: {Id}", "{{entity.Name}}", query.{{pk.Name}});
                    var entity = await _repository.Get{{entity.Name}}ById(query.{{pk.Name}});
                    if (entity == null)
                    {
                        return new NotFoundValidationResult("{{pk.Name}}", "Invalid {{entity.Name}} Id");
                    }

                    return new ValidObjectResult(_mapper.Map<ResponseModel>(entity));
                }
            }
            """);

        yield return File($"backend/Api/CQRS/{entity.Name}/Queries/List/QueryModel.cs", $$"""
            using MediatR;
            using {{ns}}.SharedRepository;
            using {{ns}}.SharedWeb.Results;

            namespace {{ns}}.Api.{{entity.Name}}.Queries.List;

            public class QueryModel : PageQueryModel, IRequest<ValidationResult>
            {
            }
            """);

        yield return File($"backend/Api/CQRS/{entity.Name}/Queries/List/Validator.cs", $$"""
            using FluentValidation;

            namespace {{ns}}.Api.{{entity.Name}}.Queries.List;

            public class Validator : AbstractValidator<QueryModel>
            {
                public Validator()
                {
                    RuleFor(x => x.Page).GreaterThan(0).When(x => x.Page.HasValue);
                    RuleFor(x => x.PageSize).GreaterThan(0).When(x => x.PageSize.HasValue);
                }
            }
            """);

        yield return File($"backend/Api/CQRS/{entity.Name}/Queries/List/Handler.cs", $$"""
            using AutoMapper;
            using MediatR;
            using Microsoft.Extensions.Logging;
            using {{ns}}.Api.Common.ViewModels;
            using {{ns}}.Api.{{entity.Name}}.Shared;
            using {{ns}}.Repository.IRepositories;
            using {{ns}}.SharedRepository.FilterModels;
            using {{ns}}.SharedWeb.Results;

            namespace {{ns}}.Api.{{entity.Name}}.Queries.List;

            public class Handler : IRequestHandler<QueryModel, ValidationResult>
            {
                private readonly I{{entity.Name}}Repository _repository;
                private readonly IMapper _mapper;
                private readonly ILogger<Handler> _logger;

                public Handler(I{{entity.Name}}Repository repository, IMapper mapper, ILogger<Handler> logger)
                {
                    _repository = repository;
                    _mapper = mapper;
                    _logger = logger;
                }

                public async Task<ValidationResult> Handle(QueryModel query, CancellationToken cancellationToken)
                {
                    _logger.LogInformation("Fetching list for {Entity}", "{{entity.Name}}");
                    var filters = new {{entity.Name}}Filter();
                    var paginated = await _repository.GetPaginated{{p}}(query, filters);
                    var viewModels = _mapper.Map<PaginatedResponseModel<ResponseModel>>(paginated);
                    return new ValidObjectResult(viewModels);
                }
            }
            """);
    }

    private static string EntityController(string ns, EntityDefinition entity, IDatabaseProvider provider)
    {
        var p = NameFactory.Pluralize(entity.Name);
        var pk = entity.Columns.Single(c => c.IsPrimaryKey);
        var pkClr = Clr.TypeName(new ColumnDefinition { LogicalType = pk.LogicalType, IsNullable = false }, provider);
        return $$"""
            using MediatR;
            using Microsoft.AspNetCore.Mvc;

            namespace {{ns}}.Api.Controllers;

            [Route("api/[controller]")]
            [ApiController]
            public class {{p}}Controller : ControllerBase
            {
                private readonly IMediator _mediator;
                private readonly ILogger<{{p}}Controller> _logger;

                public {{p}}Controller(IMediator mediator, ILogger<{{p}}Controller> logger)
                {
                    _mediator = mediator;
                    _logger = logger;
                }

                [HttpPost]
                public async Task<IActionResult> PostAsync([FromBody] {{ns}}.Api.{{entity.Name}}.Commands.Create.CommandModel command)
                {
                    return new ObjectResult(await _mediator.Send(command));
                }

                [HttpGet("{id}")]
                public async Task<IActionResult> GetByIdAsync({{pkClr}} id)
                {
                    var details = await _mediator.Send(new {{ns}}.Api.{{entity.Name}}.Queries.GetById.QueryModel { {{pk.Name}} = id });
                    return new ObjectResult(details);
                }

                [HttpGet]
                public async Task<IActionResult> GetListAsync([FromQuery] {{ns}}.Api.{{entity.Name}}.Queries.List.QueryModel query)
                {
                    var result = await _mediator.Send(query);
                    return new ObjectResult(result);
                }

                [HttpPut("{id}")]
                public async Task<IActionResult> PutAsync({{pkClr}} id, [FromBody] {{ns}}.Api.{{entity.Name}}.Commands.Update.CommandModel command)
                {
                    command.{{pk.Name}} = id;
                    return new ObjectResult(await _mediator.Send(command));
                }

                [HttpDelete("{id}")]
                public async Task<IActionResult> DeleteAsync({{pkClr}} id)
                {
                    var command = new {{ns}}.Api.{{entity.Name}}.Commands.Delete.CommandModel { {{pk.Name}} = id };
                    return new ObjectResult(await _mediator.Send(command));
                }
            }
            """;
    }

    #endregion

    #region Migrations & Validation Tests

    private static string Migration(string ns, ProjectConfiguration configuration, IDatabaseProvider provider)
    {
        return $$"""
            using Microsoft.EntityFrameworkCore.Infrastructure;
            using Microsoft.EntityFrameworkCore.Migrations;
            using {{ns}}.Repository.Context;

            #nullable disable

            namespace {{ns}}.Repository.Migrations;

            [DbContext(typeof(AppDbContext))]
            [Migration("20260101000000_InitialCreate")]
            public partial class InitialCreate : Migration
            {
                protected override void Up(MigrationBuilder migrationBuilder)
                {
            {{BuildUp(configuration, provider)}}
                }

                protected override void Down(MigrationBuilder migrationBuilder)
                {
            {{string.Join(Environment.NewLine, configuration.Entities.Select(e => $"        migrationBuilder.DropTable(name: \"{e.Name}\");"))}}
                }
            }
            """;
    }

    private static string BuildUp(ProjectConfiguration configuration, IDatabaseProvider provider)
    {
        var sb = new StringBuilder();
        foreach (var entity in configuration.Entities)
        {
            sb.AppendLine($"        migrationBuilder.CreateTable(");
            sb.AppendLine($"            name: \"{entity.Name}\",");
            sb.AppendLine("            columns: table => new");
            sb.AppendLine("            {");
            foreach (var column in entity.Columns.OrderBy(c => c.Ordinal))
            {
                var clr = Clr.TypeName(new ColumnDefinition { LogicalType = column.LogicalType, IsNullable = false }, provider).Replace("?", "", StringComparison.Ordinal);
                var nullable = column.IsNullable ? "true" : "false";
                sb.AppendLine($"                {column.Name} = table.Column<{clr}>(nullable: {nullable}),");
            }
            sb.AppendLine("                CreatedBy = table.Column<int>(nullable: false),");
            sb.AppendLine("                ModifiedBy = table.Column<int>(nullable: true),");
            sb.AppendLine("                CreatedOn = table.Column<DateTime>(nullable: false),");
            sb.AppendLine("                ModifiedOn = table.Column<DateTime>(nullable: true),");
            sb.AppendLine("                IsDeleted = table.Column<bool>(nullable: false)");
            sb.AppendLine("            },");
            sb.AppendLine("            constraints: table =>");
            sb.AppendLine("            {");
            var pk = entity.Columns.Single(c => c.IsPrimaryKey);
            sb.AppendLine($"                table.PrimaryKey(\"PK_{entity.Name}\", x => x.{pk.Name});");
            sb.AppendLine("            });");
        }

        foreach (var entity in configuration.Entities)
        {
            foreach (var column in entity.Columns.Where(c => c.ForeignKeyTargetEntityId is not null))
            {
                var target = configuration.Entities.FirstOrDefault(e => e.Id == column.ForeignKeyTargetEntityId);
                if (target != null)
                {
                    var targetPk = target.Columns.Single(c => c.Id == column.ForeignKeyTargetColumnId);
                    sb.AppendLine($"        migrationBuilder.AddForeignKey(");
                    sb.AppendLine($"            name: \"FK_{entity.Name}_{target.Name}_{column.Name}\",");
                    sb.AppendLine($"            table: \"{entity.Name}\",");
                    sb.AppendLine($"            column: \"{column.Name}\",");
                    sb.AppendLine($"            principalTable: \"{target.Name}\",");
                    sb.AppendLine($"            principalColumn: \"{targetPk.Name}\");");
                }
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string ValidationTests(string ns, ProjectConfiguration configuration)
    {
        var entity = configuration.Entities.FirstOrDefault();
        if (entity == null)
        {
            return $$"""
                namespace {{ns}}.Tests;

                public class ValidationTests
                {
                    [Fact]
                    public void Placeholder_test()
                    {
                        Assert.True(true);
                    }
                }
                """;
        }

        var hasRequiredCols = entity.Columns.Any(c => !c.IsPrimaryKey && !c.IsNullable && c.LogicalType is "string" or "text");
        var assertValidation = hasRequiredCols
            ? "result.ShouldHaveAnyValidationError();"
            : "Assert.NotNull(result);";

        return $$"""
            using FluentValidation.TestHelper;
            using {{ns}}.Api.{{entity.Name}}.Commands.Create;

            namespace {{ns}}.Tests;

            public class ValidationTests
            {
                [Fact]
                public void Create_rejects_empty_required_strings()
                {
                    var validator = new Validator();
                    var result = validator.TestValidate(new CommandModel());
                    {{assertValidation}}
                }
            }
            """;
    }

    #endregion

    private static string AutoFixtureHelperFile(string ns) => $$"""
        using AutoFixture;

        namespace {{ns}}.UnitTests.Helpers;

        public sealed class AutoFixtureHelper
        {
            private static Fixture? _instance;
            public static Fixture Fixture
            {
                get
                {
                    if (_instance == null)
                    {
                        var fixture = new Fixture();
                        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                                         .ForEach(b => fixture.Behaviors.Remove(b));
                        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
                        _instance = fixture;
                    }
                    return _instance;
                }
            }
        }
        """;

    private static string EntityControllerTests(string ns, EntityDefinition entity)
    {
        var p = NameFactory.Pluralize(entity.Name);
        var pk = entity.Columns.Single(c => c.IsPrimaryKey);
        var pkClr = pk.LogicalType switch
        {
            "guid" or "uuid" => "Guid",
            "int" => "int",
            "bigint" => "long",
            "string" => "string",
            _ => "Guid"
        };

        return $$"""
            using {{ns}}.Api.Controllers;
            using {{ns}}.UnitTests.Helpers;
            using AutoFixture;
            using MediatR;
            using Microsoft.AspNetCore.Mvc;
            using Microsoft.Extensions.Logging;
            using Moq;

            namespace {{ns}}.UnitTests.ApiTests.Controllers;

            public class {{p}}ControllerTests
            {
                private readonly Mock<IMediator> _mockMediator;
                private readonly Mock<ILogger<{{p}}Controller>> _mockLogger;
                private readonly {{p}}Controller _controller;

                public {{p}}ControllerTests()
                {
                    _mockMediator = new Mock<IMediator>();
                    _mockLogger = new Mock<ILogger<{{p}}Controller>>();
                    _controller = new {{p}}Controller(_mockMediator.Object, _mockLogger.Object);
                }

                [Fact]
                public async Task ShouldPassGet{{p}}List()
                {
                    // Arrange
                    var fixture = AutoFixtureHelper.Fixture;
                    var queryModel = fixture.Create<{{ns}}.Api.{{entity.Name}}.Queries.List.QueryModel>();
                    var validObjectResult = fixture.Create<{{ns}}.SharedWeb.Results.ValidObjectResult>();
                    _mockMediator
                        .Setup(c => c.Send(It.IsAny<{{ns}}.Api.{{entity.Name}}.Queries.List.QueryModel>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(validObjectResult);

                    // Act
                    var result = await _controller.GetListAsync(queryModel);

                    // Assert
                    var objectResult = Assert.IsType<ObjectResult>(result);
                    Assert.NotNull(objectResult);
                }

                [Fact]
                public async Task ShouldPassGet{{entity.Name}}ById()
                {
                    // Arrange
                    var fixture = AutoFixtureHelper.Fixture;
                    var id = fixture.Create<{{pkClr}}>();
                    var validObjectResult = fixture.Create<{{ns}}.SharedWeb.Results.ValidObjectResult>();
                    _mockMediator
                        .Setup(c => c.Send(It.IsAny<{{ns}}.Api.{{entity.Name}}.Queries.GetById.QueryModel>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(validObjectResult);

                    // Act
                    var result = await _controller.GetByIdAsync(id);

                    // Assert
                    var objectResult = Assert.IsType<ObjectResult>(result);
                    Assert.NotNull(objectResult);
                }

                [Fact]
                public async Task ShouldPassCreate{{entity.Name}}()
                {
                    // Arrange
                    var fixture = AutoFixtureHelper.Fixture;
                    var command = fixture.Create<{{ns}}.Api.{{entity.Name}}.Commands.Create.CommandModel>();
                    var validObjectResult = fixture.Create<{{ns}}.SharedWeb.Results.ValidObjectResult>();
                    _mockMediator
                        .Setup(c => c.Send(It.IsAny<{{ns}}.Api.{{entity.Name}}.Commands.Create.CommandModel>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(validObjectResult);

                    // Act
                    var result = await _controller.PostAsync(command);

                    // Assert
                    var objectResult = Assert.IsType<ObjectResult>(result);
                    Assert.NotNull(objectResult);
                }
            }
            """;
    }

}
