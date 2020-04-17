# Shirhatti.WebLogger

ILogger implementation that returns streaming logs via an HTTP endpoint

![weblogger](https://user-images.githubusercontent.com/4734691/79593211-bcddda00-80a0-11ea-8885-0f747cdf4b02.gif)

## Usage

1. Add package reference ![version](https://img.shields.io/nuget/v/Shirhatti.WebLogger)

```xml
  <ItemGroup>
    <PackageReference Include="Shirhatti.WebLogger" Version="x" />
  </ItemGroup>
```
  or

```sh
dotnet add package Shirhatti.WebLogger
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

5. Run your application- `dotnet run`

6. Navigate to `https://localhost:5001/debug/logs` to view your streaming logs

## Docs

**Incomplete** docs at https://shirhatti.com/Shirhatti.WebLogger/ 
