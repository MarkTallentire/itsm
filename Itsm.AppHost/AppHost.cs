var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Itsm_Api>("itsm-api");

builder.AddProject<Projects.Itsm_Agent>("itsm-agent")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();