var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").AddDatabase("itsmdb");

var api = builder.AddProject<Projects.Itsm_Api>("itsm-api")
    .WithReference(postgres)
    .WaitFor(postgres);

builder.AddProject<Projects.Itsm_Agent>("itsm-agent")
    .WithReference(api)
    .WaitFor(api);

builder.AddNpmApp("itsm-frontend", "../Itsm.Frontend", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
