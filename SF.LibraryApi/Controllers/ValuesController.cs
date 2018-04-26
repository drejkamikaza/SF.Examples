using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using SF.Library.Contracts;

namespace SF.LibraryApi.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        [HttpGet]
        public async Task<IEnumerable<Book>> Get()
        {
            List<Book> result = null;

            try
            {
                Microsoft.ServiceFabric.Services.Client.ServicePartitionKey partitionKey = new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(1);
                ILibraryService client = ServiceProxy.Create<ILibraryService>(new Uri("fabric:/SF.Examples/SF.Library"), partitionKey);
                result = await client.SearchLibraryAsync(new BookSearch(), CancellationToken.None);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.ToString());
                throw ex;
            }

            return result;
        }

        [HttpGet("{id}")]
        public async Task<Book> Get(Guid id)
        {
            Book result = null;

            try
            {
                Microsoft.ServiceFabric.Services.Client.ServicePartitionKey partitionKey = new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(1);
                ILibraryService client = ServiceProxy.Create<ILibraryService>(new Uri("fabric:/SF.Examples/SF.Library"), partitionKey);
                result = await client.GetBookAsync(id, CancellationToken.None);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.ToString());
                throw ex;
            }

            return result;
        }

        [HttpPost]
        public async Task Post([FromBody]string value)
        {
            try
            {
                Microsoft.ServiceFabric.Services.Client.ServicePartitionKey partitionKey = new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(1);
                ILibraryService client = ServiceProxy.Create<ILibraryService>(new Uri("fabric:/SF.Examples/SF.Library"), partitionKey);
                Guid Id = await client.AddBookAsync(new Book { Id = Guid.NewGuid(), Author = $"test {DateTime.Now.Ticks}", Title = $"test Title {DateTime.Now.Ticks}", Year = DateTime.Now.Year }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.ToString());
                throw ex;
            }
        }

        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
