using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using SF.Library.Contracts;
using SF.LibraryApi.Models;

namespace SF.LibraryApi.Controllers
{
    [Route("api/[controller]")]
    public class LibraryController : Controller
    {
        private readonly Lazy<ILibraryService> _libraryService = new Lazy<ILibraryService>(() =>
        {
            Microsoft.ServiceFabric.Services.Client.ServicePartitionKey partitionKey = new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(1);
            ILibraryService client = ServiceProxy.Create<ILibraryService>(new Uri("fabric:/SF.Examples/SF.Library"), partitionKey);
            return client;
        });

        [HttpGet]
        public async Task<IEnumerable<BookModel>> Get()
        {
            List<BookModel> result = null;

            ServiceEventSource.Current.Message("Started GET ALL");

            try
            {
                var books = await _libraryService.Value.SearchLibraryAsync(new BookSearch(), CancellationToken.None);
                if (books != null && books.Any())
                    return new List<BookModel>();

                result = new List<BookModel>();
                books.ForEach(x => result.Add(new BookModel
                {
                    Id = x.Id,
                    Author = x.Author,
                    Title = x.Title,
                    Year = x.Year
                }));
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.ToString());
                throw ex;
            }

            ServiceEventSource.Current.Message("Finished GET ALL");

            return result;
        }

        [HttpGet("{id}")]
        public async Task<BookModel> Get(Guid id)
        {
            ServiceEventSource.Current.Message("Started GET ONE");

            BookModel result = null;

            try
            {
                var singleBook = await _libraryService.Value.GetBookAsync(id, CancellationToken.None);
                if (singleBook == null)
                    return null;

                result = new BookModel
                {
                    Id = singleBook.Id,
                    Author = singleBook.Author,
                    Title = singleBook.Title,
                    Year = singleBook.Year
                };
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.ToString());
                throw ex;
            }

            ServiceEventSource.Current.Message("Finished GET ONE");

            return result;
        }

        [HttpPost]
        public async Task Post([FromBody]BookModel value)
        {
            ServiceEventSource.Current.Message("Started ADD NEW");

            try
            {
                if (value == null)
                    return;

                Book book = new Book
                {
                    Id = value.Id,
                    Author = value.Author,
                    Title = value.Title,
                    Year = value.Year
                };

                Guid Id = await _libraryService.Value.AddOrUpdateBookAsync(book, CancellationToken.None);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.ToString());
                throw ex;
            }

            ServiceEventSource.Current.Message("Finished ADD NEW");
        }

        [HttpPut("{id}")]
        public async Task Put(Guid id, [FromBody]BookModel value)
        {
            ServiceEventSource.Current.Message("Started UPDATE");

            try
            {
                if (id == Guid.Empty)
                    return;

                if (value == null)
                    return;

                Book book = new Book
                {
                    Id = id,
                    Author = value.Author,
                    Title = value.Title,
                    Year = value.Year
                };

                Guid Id = await _libraryService.Value.AddOrUpdateBookAsync(book, CancellationToken.None);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.ToString());
                throw ex;
            }

            ServiceEventSource.Current.Message("Finished UPATE");
        }

        [HttpDelete("{id}")]
        public async Task Delete(Guid id)
        {
            ServiceEventSource.Current.Message("Started DELETE");

            try
            {
                if (id == Guid.Empty)
                    return;

                await _libraryService.Value.RemoveBookAsync(id, CancellationToken.None);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.ToString());
                throw ex;
            }

            ServiceEventSource.Current.Message("Finished DELETE");
        }
    }
}
