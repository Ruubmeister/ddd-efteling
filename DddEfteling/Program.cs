using DddEfteling.Park.Employees.Controls;
using DddEfteling.Park.Entrances.Controls;
using DddEfteling.Park.Realms.Controls;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace DddEfteling
{
    public class Program
    {
        protected Program()
        {

        }

        public static async Task Main(string[] args)
        {
            

            var host = CreateHostBuilder(args).Build();

            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;

                services.GetRequiredService<IRealmControl>();
                IEntranceControl entranceControl = services.GetRequiredService<IEntranceControl>();
                services.GetRequiredService<IEmployeeControl>();

                entranceControl.OpenPark();

                _ = Task.Run(() =>
                {
                    while (true)
                    {
                        visitorControl.HandleIdleVisitors();
                        Task.Delay(100).Wait();
                    }
                });

                _ = Task.Run(() =>
                 {
                     Random random = new Random();
                     int maxVisitors = 5000;
                     int currentVisitors = 0;
                     while (currentVisitors <= maxVisitors)
                     {
                         int newVisitors = random.Next(2, 10);

                         visitorControl.AddVisitors(newVisitors);
                         currentVisitors += newVisitors;
                         Task.Delay(5000).Wait();
                     }
                 });
                

            }

            await host.RunAsync();

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
