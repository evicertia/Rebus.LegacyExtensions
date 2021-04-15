
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Rebus.Bus;

using Rebus.ServiceProvider;

namespace Rebus.ClientSample
{
	public class Startup
	{
		private ISendTestService _sendTestSvc;

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddBroker();

			services.AddTransient<ISendTestService, SendTestService>();
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
					//await bus.Subscribe<TestSendRequest>();
					//await bus.Subscribe<SendEmailResponse>();
					//await bus.Subscribe<TestSendResponse>();

					//_sendEmailService = app.ApplicationServices.GetRequiredService<ISendEmailService>();

					//var res = await _sendEmailService.SendEmail();

					_sendTestSvc = app.ApplicationServices.GetRequiredService<ISendTestService>();

					await _sendTestSvc.SendMessage();

					await context.Response.WriteAsync($"Response to SendEmail => {""}");
				});
			});
		}
	}
}
