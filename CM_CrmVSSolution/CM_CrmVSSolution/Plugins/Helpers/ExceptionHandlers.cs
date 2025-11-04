using Microsoft.Xrm.Sdk;
using System;
using System.Text;

namespace Plugins.Helpers {
    internal class ExceptionHandler {
        private readonly Exception _exception;

        private readonly ITracingService _traceService;

        public ExceptionHandler(
            //ITracingService traceService, 
            Exception exception) {
            _exception = exception;
            //_traceService = traceService;
        }

        public void Process() {
            var errorLog = new StringBuilder();
            errorLog.Append("Exception Message:\n " + _exception.Message);
            errorLog.Append("Exception Source:\n " + _exception.Source);
            errorLog.AppendLine("Exception Stack Trace:\n" + _exception.StackTrace);
            errorLog.AppendLine($"Inner Exception Message:\n{_exception.InnerException}");
            errorLog.AppendLine($"Exception Target Site:\n{_exception.TargetSite}");
            //_traceService.Trace(errorLog.ToString());
            throw new InvalidPluginExecutionException(_exception.Message);
        }
    }
}