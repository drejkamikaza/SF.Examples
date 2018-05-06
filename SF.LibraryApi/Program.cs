using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Diagnostics.EventFlow;
using Microsoft.Diagnostics.EventFlow.ServiceFabric;
using Microsoft.ServiceFabric.Services.Runtime;

namespace SF.LibraryApi
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                using (DiagnosticPipeline pipeline = ServiceFabricDiagnosticPipelineFactory.CreatePipeline("SFExamples-DiagnosticPipeline"))
                {
                    ServiceRuntime.RegisterServiceAsync("SF.LibraryApiType",
                        context => new LibraryApi(context, pipeline)).GetAwaiter().GetResult();

                    ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(LibraryApi).Name);

                    // Prevents this host process from terminating so services keeps running. 
                    Thread.Sleep(Timeout.Infinite);
                }
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
