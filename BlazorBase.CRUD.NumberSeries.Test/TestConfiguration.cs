using BlazorBase.CRUD.NumberSeries.Test.Mocks;
using BlazorBase.CRUD.Services;
using BlazorBase.MessageHandling.Interfaces;
using BlazorBase.MessageHandling.Services;
using Bunit;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorBase.CRUD.NumberSeries.Test
{
    public static class TestConfiguration
    {
        public static void AddTestConfiguration(TestServiceProvider services)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en");
            CultureInfo.DefaultThreadCurrentCulture = Thread.CurrentThread.CurrentCulture;
            CultureInfo.DefaultThreadCurrentUICulture = Thread.CurrentThread.CurrentCulture;

            services.AddTransient<DbContext, DbContextMock>();
            services.AddDbContext<DbContextMock>(options => options.UseInMemoryDatabase(databaseName: "BlazorBaseTestDbContextMock"));
            services.AddLocalization();
            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en");
                options.SupportedUICultures = new List<CultureInfo> { new CultureInfo("en") };
            });

            services.AddTransient<BaseService>();
            services.AddSingleton<NoSeriesService>();

            services.AddBlazorBaseMessageHandling();
        }
    }
}
