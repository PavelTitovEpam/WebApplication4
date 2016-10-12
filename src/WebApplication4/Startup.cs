using System.Collections.Generic;
using System.Reflection;
using AspNet5ModularApp;
using WebApplication4.Infrastructure;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.Data.Entity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using WebApplication4.Models;
using WebApplication4.Services;
using System.Linq;
using Microsoft.AspNet.FileProviders;

namespace WebApplication4
{
	public class Startup
	{

		private readonly string applicationBasePath;
		private readonly IAssemblyLoaderContainer assemblyLoaderContainer;
		private readonly IAssemblyLoadContextAccessor assemblyLoadContextAccessor;

		public Startup(IHostingEnvironment env, IApplicationEnvironment applicationEnvironment, IAssemblyLoaderContainer assemblyLoaderContainer, IAssemblyLoadContextAccessor assemblyLoadContextAccessor)
		{

			// Set up configuration sources.
			var builder = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json")
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

			if (env.IsDevelopment())
			{
				// For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
				builder.AddUserSecrets();
			}

			builder.AddEnvironmentVariables();
			Configuration = builder.Build();
			// =============================================================================================
			this.applicationBasePath = applicationEnvironment.ApplicationBasePath;
			this.assemblyLoaderContainer = assemblyLoaderContainer;
			this.assemblyLoadContextAccessor = assemblyLoadContextAccessor;
		}

		public IConfigurationRoot Configuration { get; set; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			// Add framework services.
			services.AddEntityFramework()
				.AddSqlServer()
				.AddDbContext<ApplicationDbContext>(options =>
					options.UseSqlServer(Configuration["Data:DefaultConnection:ConnectionString"]));

			services.AddIdentity<ApplicationUser, IdentityRole>()
				.AddEntityFrameworkStores<ApplicationDbContext>()
				.AddDefaultTokenProviders();

			// Add application services.
			services.AddTransient<IEmailSender, AuthMessageSender>();
			services.AddTransient<ISmsSender, AuthMessageSender>();

			//=============================================================================================================
			IEnumerable<Assembly> assemblies = AssemblyManager.LoadAssemblies(
			  this.applicationBasePath.Substring(0, this.applicationBasePath.LastIndexOf("src")) + "artifacts\\bin\\Extensions",
			  this.assemblyLoaderContainer,
			  this.assemblyLoadContextAccessor
			);

			ExtensionManager.SetAssemblies(assemblies);
			services.AddMvc()
			  .AddPrecompiledRazorViews(ExtensionManager.Assemblies.ToArray())
			  .AddRazorOptions(razorOptions => { razorOptions.FileProvider = this.GetFileProvider(assemblies, this.applicationBasePath); });

			services.AddTransient<DefaultAssemblyProvider>();
			services.AddTransient<IAssemblyProvider, ExtensionAssemblyProvider>();

			foreach (IExtension extension in ExtensionManager.Extensions)
				extension.ConfigureServices(services);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			loggerFactory.AddConsole(Configuration.GetSection("Logging"));
			loggerFactory.AddDebug();

			if (env.IsDevelopment())
			{
				app.UseBrowserLink();
				app.UseDeveloperExceptionPage();
				app.UseDatabaseErrorPage();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");

				// For more details on creating database during deployment see http://go.microsoft.com/fwlink/?LinkID=615859
				try
				{
					using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>()
						.CreateScope())
					{
						serviceScope.ServiceProvider.GetService<ApplicationDbContext>()
							 .Database.Migrate();
					}
				}
				catch { }
			}

			app.UseIISPlatformHandler(options => options.AuthenticationDescriptions.Clear());

			app.UseStaticFiles();

			app.UseIdentity();

			// To configure external authentication please see http://go.microsoft.com/fwlink/?LinkID=532715

			app.UseMvc(routes =>
			{
				routes.MapRoute(
					name: "default",
					template: "{controller=Home}/{action=Index}/{id?}");
			});

			app.UseMvc(routeBuilder =>
			{
				foreach (IExtension extension in ExtensionManager.Extensions)
					extension.RegisterRoutes(routeBuilder);
			});
		}

		// Entry point for the application.
		public static void Main(string[] args) => WebApplication.Run<Startup>(args);

		public IFileProvider GetFileProvider(IEnumerable<Assembly> assemblies, string path)
		{
			IEnumerable<IFileProvider> fileProviders = new IFileProvider[] { new PhysicalFileProvider(path) };

			return new CompositeFileProvider(
			  fileProviders.Concat(
				assemblies.Select(a => new EmbeddedFileProvider(a, a.GetName().Name))
			  )
			);
		}

	}
}
