using FormulaValidator.GraphQL;
using FormulaValidator.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddScoped<IFormulaValidationService, FormulaValidationService>();
builder.Services.AddSingleton<IConstantRepository, ConfigConstantRepository>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add GraphQL
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddType<FormulaValidator.Models.ValidationRequest>()
    .AddType<FormulaValidator.Models.MeasuredValue>()
    .AddType<FormulaValidator.Models.Constant>()
    .AddType<FormulaValidator.Models.ValidationResult>()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment());

var app = builder.Build();

// Configure pipeline
app.UseCors("AllowAll");
app.MapGraphQL();

// Get port from environment variable or use default
var port = Environment.GetEnvironmentVariable("PORT") ?? "5001";
app.Urls.Add($"http://+:{port}");

Console.WriteLine($"🚀 Formula Validator API starting on port {port}");
Console.WriteLine($"📊 GraphQL Playground available at http://localhost:{port}/graphql");

app.Run();
