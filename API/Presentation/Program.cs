using Presentation;
using Service.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.ConfigureS3(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddControllers(options => { options.Filters.Add<ModelValidationFilter>(); });

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Video uploader API",
        Description = "API for uploading files to S3/MinIO",
    });
});
var app = builder.Build();

app.UseCors(option => option
    .SetIsOriginAllowed(_ => true)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
);

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "File Upload API v1");
        c.RoutePrefix = "swagger"; 
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;

// TODO:
// MAX LIMIT OF FILE SIZE
// SET LIFECYCLE FOR MULTIPART
