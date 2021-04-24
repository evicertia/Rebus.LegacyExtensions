
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Sample.Contracts;
using Rebus.ServiceProvider;


namespace Rebus.ServerSample
{
	public class Startup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddBroker();
			//services.AddTransient<IHandleMessages<SendEmailRequest>, SendEmailRequestHandler>();
			services.AddTransient<IHandleMessages<TestSendRequest>, TestSendRequestHandler>();
			services.AddTransient<IHandleMessages<FirstSagaMessage>,TestDaemonSaga>();
			services.AddTransient<IHandleMessages<SecondSagaMessage>, TestDaemonSaga>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseRouting();

			app.ApplicationServices.UseRebus();

			var bus = app.ApplicationServices.GetRequiredService<IBus>();

			bus.DeclareSubscriptionsFor(GetType().Assembly);


			app.UseEndpoints(endpoints =>
			{
				endpoints.MapGet("/", async context =>
				{
					await bus.Subscribe<TestSendRequest>();
					await bus.Subscribe<SecondSagaMessage>();

					await context.Response.WriteAsync("Hello World!");
				});
			});
		}
	}
}
