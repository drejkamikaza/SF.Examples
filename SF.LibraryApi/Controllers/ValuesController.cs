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
        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
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

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
