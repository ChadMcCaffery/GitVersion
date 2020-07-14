﻿using System;
using System.Threading.Tasks;
using GitVersion.Core;
using GitVersion.Core.Infrastructure;

namespace GitVersion.Normalize
{
    public class NormalizeCommandHandler : CommandHandler<NormalizeOptions>, IRootCommandHandler
    {
        private readonly ILogger logger;
        private readonly IService service;

        public NormalizeCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(NormalizeOptions options)
        {
            var value = service.Call();
            logger.LogInformation($"Command : 'normalize', LogFile : '{options.LogFile}', WorkDir : '{options.WorkDir}' ");
            return Task.FromResult(value);
        }
    }
}