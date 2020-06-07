using EventSourcing.CosmosDb.Services;
using EventSourcing.Table.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace EventSourcing.Web
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddControllersWithViews().AddNewtonsoftJson(options =>
				options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			);

			services.AddScoped<ITableConferenceService, TableConferenceService>();

			services.AddScoped<IConferenceCosmosDbService, CosmosDbConferenceService>();

			// In production, the React files will be served from this directory
			services.AddSpaStaticFiles(configuration => { configuration.RootPath = "ClientApp/build"; });

			services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" }); });
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			app.UseSwagger();
			app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"); });

			app.UseHttpsRedirection();
			app.UseStaticFiles();
			app.UseSpaStaticFiles();

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute(
					"default",
					"{controller}/{action=Index}/{id?}");
			});

			app.UseSpa(spa =>
			{
				spa.Options.SourcePath = "ClientApp";

				if (env.IsDevelopment())
				{
					//spa.UseProxyToSpaDevelopmentServer("http://localhost:3000/");
					spa.UseReactDevelopmentServer(npmScript: "start");
				}
			});
		}
	}
}