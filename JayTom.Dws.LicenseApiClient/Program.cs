using MudBlazor.Services;
using JayTom.Dws.LicenseApiClient.Api;
using JayTom.Dws.LicenseApiClient.Data;
using JayTom.Dws.LicenseApiClient.Notification;

namespace JayTom.Dws.LicenseApiClient;

internal class Program {

    private static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddMudServices();

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddSingleton<WeatherForecastService>();
        builder.Services.AddHttpClient("INSURANCE", httpClient => {
            // httpClient.Timeout = TimeSpan.FromSeconds(10);
        }).ConfigurePrimaryHttpMessageHandler(() => {
            var handler = new HttpClientHandler() {
                UseDefaultCredentials = true,
                MaxConnectionsPerServer = 600,
                ServerCertificateCustomValidationCallback = (m, c, ch, _) => true,
                //UseProxy = false
            };

            return handler;
        });
        //接口注入
        builder.Services.AddScoped<ILicenseApiRequest, LicenseApiRequest>();
        //订阅事件
        builder.Services.AddScoped<NotificationService>();
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment()) {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseRouting();

        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.Run();
    }
}