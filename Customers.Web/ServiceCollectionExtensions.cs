﻿using System;
using Customers.Web.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Observability.Core;
using Observability.MongoDb;
using Observability.RabbitMq;
using Polly;
using Polly.Extensions.Http;

namespace Customers.Web
{
    public static class ServiceCollectionExtensions
    {
        internal static void ConfigureFaultTolerantHttpClient(this IServiceCollection services)
        {
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                // Handle 404 not found
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                // Handle 401 Unauthorized
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                // What to do if any of the above erros occur:
                // Retry 3 times, each time wait 1,2 and 4 seconds before retrying.
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            services.AddHttpClient("FaultHandlingHttpClient").AddPolicyHandler(retryPolicy);
            services.AddTransient<IFaultHandlingHttpClient, FaultHandlingHttpClient>();
        }

        internal static void ConfigureRabbitMq(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RabbitMqConfiguration>(configuration.GetSection("RabbitMq"));
            services.AddTransient<IMessageProducer, RabbitMqMessageProducer>();
        }

        internal static void ConfigureRepositories(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MongoDbConfiguration>(configuration.GetSection("MongoDb"));
            services.AddTransient<ICustomerRepository, CustomerRepository>();
        }

        internal static void ConfigureApiDocumentation(this IServiceCollection services)
        {
            services.AddSwaggerGen(swagger =>
            {
                swagger.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Customers",
                    Description = "Customers API",
                    TermsOfService = new Uri("https://www.prajeeshprathap.com"),
                    Version = "v1",
                    Contact = new OpenApiContact
                    {
                        Name = "Prajeesh Prathap",
                        Url = new Uri("https://www.prajeeshprathap.com"),
                    }
                });
            });
        }
    }
}
