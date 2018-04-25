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

        public async Task<Guid> AddBookAsync(Book bookToAdd, CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.ServiceMessage(this.Context, $"Method {nameof(AddBookAsync)} called");

            var addQueue = await StateManager.GetOrAddAsync<IReliableConcurrentQueue<Book>>(ADD_QUEUE);

            using (var tx = this.StateManager.CreateTransaction())
            {
                bookToAdd.Id = Guid.NewGuid();
                await addQueue.EnqueueAsync(tx, bookToAdd, cancellationToken);
                ServiceEventSource.Current.ServiceMessage(this.Context, $"Object {nameof(bookToAdd)} scheduled for add to queue");

                await tx.CommitAsync();
            }

            ServiceEventSource.Current.ServiceMessage(this.Context, $"Method {nameof(AddBookAsync)} finished");

            return bookToAdd.Id;
        }

        public async Task<Book> GetBookAsync(Guid id, CancellationToken cancellationToken)
        {
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

                return bookConditionValue.Value;
            }
        }

        public async Task<List<Book>> SearchLibraryAsync(BookSearch searchParameters, CancellationToken cancellationToken)
        {
            List<Book> result = null;

            using (var tx = this.StateManager.CreateTransaction())
            {
                var dictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, Book>>(tx, BOOK_STORE);
                if (dictionary == null)
                    return null;

                IAsyncEnumerable<KeyValuePair<Guid, Book>> enumerableCollection = await dictionary.CreateEnumerableAsync(tx);
                if (enumerableCollection == null)
                    return null;

                var enumerator = enumerableCollection.GetAsyncEnumerator();
                enumerator.Reset();

                result = new List<Book>();
                while (await enumerator.MoveNextAsync(cancellationToken))
                {
                    if (enumerator.Current.Value != null)
                        result.Add(enumerator.Current.Value);
                }
            }

            return result;
        }

        /// <summary>
        /// Processing Adds to Library
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var addQueueExists = await this.StateManager.TryGetAsync<IReliableConcurrentQueue<Book>>(ADD_QUEUE);
            if (!addQueueExists.HasValue)
            {
                ServiceEventSource.Current.Message($"Queue {ADD_QUEUE} does not exist");
                return;
            }

            IReliableConcurrentQueue<Book> addQueue = addQueueExists.Value;
            while (!cancellationToken.IsCancellationRequested)
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var bookToAddExist = await addQueue.TryDequeueAsync(tx, cancellationToken);
                    if (!bookToAddExist.HasValue)
                        continue;

                    Book bookToAdd = bookToAddExist.Value;

                    var dictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, Book>>(tx, BOOK_STORE);
                    await dictionary.AddOrUpdateAsync(tx, bookToAdd.Id, bookToAdd, (key, value) => value);

                    ServiceEventSource.Current.Message($"Added new book to the collection {nameof(Book)} - {JsonConvert.SerializeObject(bookToAdd)}");

                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(TIME_OUT), cancellationToken);
            }
        }
    }
}
