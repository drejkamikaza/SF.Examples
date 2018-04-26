using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json;
using SF.Library.Contracts;

namespace SF.Library
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class Library : StatefulService, ILibraryService
    {
        private const string ADD_QUEUE = "AddNewBookQueue";
        private const string BOOK_STORE = "Libary Book store";
        private const int TIME_OUT = 5;

        public Library(StatefulServiceContext context)
            : base(context)
        { }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }

        private async Task<IReliableDictionary<Guid, Book>> GetDictionaryAsync()
        {
            IReliableDictionary<Guid, Book> dict = null;
            using (var tx = this.StateManager.CreateTransaction())
            {
                dict = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, Book>>(tx, BOOK_STORE);
                await tx.CommitAsync();
            }

            return dict;
        }

        public async Task<Guid> AddBookAsync(Book bookToAdd, CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.ServiceMessage(this.Context, $"Method {nameof(AddBookAsync)} called");

            if (bookToAdd == null)
                return Guid.Empty;

            try
            {
                IReliableDictionary<Guid, Book> books = await GetDictionaryAsync();

                using (var tx = this.StateManager.CreateTransaction())
                {
                    await books.AddOrUpdateAsync(tx, bookToAdd.Id, bookToAdd, (key, value) => value);

                    ServiceEventSource.Current.Message($"Added new book to the collection {nameof(Book)} - {JsonConvert.SerializeObject(bookToAdd)}");

                    await tx.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.ToString());
                throw;
            }

            ServiceEventSource.Current.ServiceMessage(this.Context, $"Method {nameof(AddBookAsync)} finished");

            return bookToAdd.Id;
        }

        public async Task<Book> GetBookAsync(Guid id, CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.ServiceMessage(this.Context, $"Method {nameof(GetBookAsync)} called");

            using (var tx = this.StateManager.CreateTransaction())
            {
                var dictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, Book>>(tx, BOOK_STORE);
                if (dictionary == null)
                    return null;

                bool exists = await dictionary.ContainsKeyAsync(tx, id, TimeSpan.FromSeconds(TIME_OUT), cancellationToken);
                if (!exists)
                    return null;

                var bookConditionValue = await dictionary.TryGetValueAsync(tx, id, TimeSpan.FromSeconds(TIME_OUT), cancellationToken);
                if (!bookConditionValue.HasValue)
                    return null;

                ServiceEventSource.Current.ServiceMessage(this.Context, $"Method {nameof(GetBookAsync)} finished");

                return bookConditionValue.Value;
            }
        }

        public async Task<List<Book>> SearchLibraryAsync(BookSearch searchParameters, CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.ServiceMessage(this.Context, $"Method {nameof(SearchLibraryAsync)} called");

            List<Book> result = null;

            var dictionary = await GetDictionaryAsync();
            if (dictionary == null)
                return null;

            try
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    IAsyncEnumerable<KeyValuePair<Guid, Book>> enumerableCollection = await dictionary.CreateEnumerableAsync(tx);
                    if (enumerableCollection == null)
                        return null;

                    var enumerator = enumerableCollection.GetAsyncEnumerator();

                    result = new List<Book>();
                    while (await enumerator.MoveNextAsync(cancellationToken))
                    {
                        if (enumerator.Current.Value != null)
                            result.Add(enumerator.Current.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.ToString());
                throw;
            }

            ServiceEventSource.Current.ServiceMessage(this.Context, $"Method {nameof(SearchLibraryAsync)} finished");

            return result;
        }
    }
}
