using Polls.Api.Common;
using Polls.Api.Common.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.AddPollsLogging();
builder.Services.AddPollsCommon();

var app = builder.Build();

app.UsePollsCommon();

app.Run();
