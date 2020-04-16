# Shirhatti.WebLogger
ILogger implementation that returns streaming logs via an HTTP endpoint

## Usage

1. Add package reference

```xml
  <ItemGroup>
    <PackageReference Include="Shirhatti.WebLogger" Version="0.0.1" />
  </ItemGroup>
```
  or

```sh
dotnet add package Shirhatti.WebLogger --version 0.0.1
```

2. Update `Program.cs`

```diff
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
+                .ConfigureLogging(logging =>
+                {
+                    logging.AddWebLogger();
+                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
```

3. Update `Startup.cs`

```diff
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
+                if (env.IsDevelopment())
+                {
+                    endpoints.MapLogs();
+                }
            });
        }
```

4. Change desired log level settings in `appSettings.Development.json`
