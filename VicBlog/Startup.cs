﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VicBlog.Data;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Swagger;
using VicBlog.Models;
using Newtonsoft.Json;

namespace VicBlog
{
    public class Startup
    {

        public IConfiguration Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("Configs/appsettings.json")
                .AddJsonFile($"Configs/appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("Configs/QiniuConfig.json")
                .AddJsonFile($"Configs/QiniuConfig.{env.EnvironmentName}.json", optional: true);


            if (env.IsDevelopment())
            {
                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                builder.AddApplicationInsightsSettings(developerMode: true);
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }


        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddCors();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "VicBlog API", Version = "0.4.0" });
            });

            // services.AddDbContext<BlogContext>(options =>
            // options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddDbContextPool<BlogContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
            });

            services.AddMvc();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, BlogContext context)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });



            app.UseCors(builder => builder.WithOrigins("http://localhost:8080","http://viccrubs.tk").AllowAnyHeader().AllowAnyMethod());
            app.UseStaticFiles();

            app.UseSwagger();

            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "VicBlog API"));

            app.UseMvc();

            Utils.UserTokenKey = Configuration["UserTokenKey"];
            Utils.LoginExpireSeconds = long.Parse(Configuration["LoginExpireSeconds"]);

            DbInitializer.Initialize(context, env.IsDevelopment(), Configuration["RootPassword"]);

            var section = Configuration.GetSection("QiniuConfig");

            Data.Qiniu.AccessKey = section["AccessKey"];
            Data.Qiniu.AccessUrl = section["AccessUrl"];
            Data.Qiniu.Deadline = int.Parse(section["Deadline"]);
            Data.Qiniu.PostUrl = section["PostUrl"];
            Data.Qiniu.SecretKey = section["SecretKey"];
            Data.Qiniu.BucketName = section["BucketName"];


        }
    }
}
