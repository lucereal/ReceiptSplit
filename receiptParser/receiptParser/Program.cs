using Microsoft.Extensions.Configuration;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver;
using receiptParser.Repository.impl;
using receiptParser.Repository.inter;
using receiptParser.Service.impl;
using receiptParser.Service.inter;
using receiptParser.Hubs;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>()
    .Build();

string? connectionString = configuration["connectionString"];


builder.Services.AddCors();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IUserReceiptService, UserReceiptService>();
builder.Services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));
builder.Services.AddSingleton<IMongoDBContext>(new MongoDBContext(MongoClientSettings.FromUrl(new MongoUrl(connectionString))));

builder.Services.AddSignalR();
    //.AddJsonProtocol();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(builder =>
{
    //builder.AllowAnyOrigin()
    //       .AllowAnyMethod()
    //       .AllowAnyHeader();
    builder.AllowAnyMethod()
       .AllowAnyHeader()
       .AllowCredentials()
       .WithOrigins("http://localhost:3000", "https://lemon-tree-0c66a350f.5.azurestaticapps.net", "https://checkmates.us");
});


app.UseRouting();
app.UseAuthorization();


#pragma warning disable ASP0014 // Suggest using top level route registrations
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<ChatHub>("/chatHub");
});
#pragma warning restore ASP0014 // Suggest using top level route registrations




app.Run();
