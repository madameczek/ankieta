using Polls.Api.Common;
using Polls.Api.Common.Logging;
using Polls.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddPollsLogging();
builder.Services.AddPollsCommon();
builder.Services.AddPollsDatabase();

var app = builder.Build();

app.UsePollsCommon();

app.Run();
