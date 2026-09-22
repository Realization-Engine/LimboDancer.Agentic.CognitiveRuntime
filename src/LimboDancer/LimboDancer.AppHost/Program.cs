var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.LimboDancer_Host>("limbodancer")
    .WithHttpHealthCheck("/health/ready");

builder.Build().Run();
