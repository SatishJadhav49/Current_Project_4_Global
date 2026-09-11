using API_PQ_Global_Reporting.Data;
using API_PQ_Global_Reporting.Utils;
using API_PQ_Global_Reporting.Middleware;
using API_PQ_Global_Reporting.Models;
using API_PQ_Global_Reporting.Mail;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Preserve original property naming (PascalCase with underscores)
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add connection string as a service
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

// Configure SMTP Settings
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// Configure System Settings
builder.Services.Configure<SystemSettings>(builder.Configuration.GetSection("SystemSettings"));

// Register Data Services
builder.Services.AddScoped<MM_EmployeeDataService>();
builder.Services.AddScoped<MM_ErrorLogDataService>();
builder.Services.AddScoped<MM_User_RolesDataService>();
builder.Services.AddScoped<MM_CommonDataService>();
builder.Services.AddScoped<MM_Documents_MasterDataService>();
builder.Services.AddScoped<MM_Global_SearchDataService>();

builder.Services.AddScoped<MailNotificationService>();
// Register Utils Services
builder.Services.AddSingleton<IApiResponseHelper, ApiResponseHelper>();

// Add CORS
builder.Services.AddCors(options =>
{   
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add custom exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
