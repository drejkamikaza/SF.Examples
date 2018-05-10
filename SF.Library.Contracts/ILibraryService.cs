using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;

[assembly: FabricTransportServiceRemotingProvider(RemotingListener = RemotingListener.CompatListener, RemotingClient = RemotingClient.V2Client)]
namespace SF.Library.Contracts
{
    public interface ILibraryService : IService
    {
        Task<Guid> AddOrUpdateBookAsync(Book bookToAdd, CancellationToken cancellationToken);

        Task<Book> GetBookAsync(Guid id, CancellationToken cancellationToken);

        Task<List<Book>> SearchLibraryAsync(BookSearch searchParameters, CancellationToken cancellationToken);

        Task<bool> RemoveBookAsync(Guid id, CancellationToken none);
    }
}
