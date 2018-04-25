using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;

namespace SF.Library.Contracts
{
    public interface ILibraryService : IService
    {
        Task<Guid> AddBookAsync(Book bookToAdd);

        Task<Book> GetBookAsync(Guid id);

        Task<List<Book>> SearchLibraryAsync(BookSearch searchParameters);
    }
}
