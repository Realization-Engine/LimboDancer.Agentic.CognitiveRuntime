var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.LimboDancer_Host>("limbodancer");

builder.Build().Run();
