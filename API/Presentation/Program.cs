using API.Services;
using Presentation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAwsS3Settings(builder.Configuration);
builder.Services.ConfigureAmazonS3();
builder.Services.AddAwsS3Service();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;

// TODO:
// MAX LIMIT OF FILE SIZE
// LIFECYCLE FOR MULTIPART OF 7 DAYS IN DOCKER-COMPOSE
// VALIDATIONS
// USING THE CORRECT RETURN OF S3 API AND NOT GENERIC ONES