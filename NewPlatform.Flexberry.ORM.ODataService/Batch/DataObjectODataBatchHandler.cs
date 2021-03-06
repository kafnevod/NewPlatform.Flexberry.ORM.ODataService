﻿namespace NewPlatform.Flexberry.ORM.ODataService.Batch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;
    using System.Web.Http.Batch;
    using System.Web.OData.Batch;
    using ICSSoft.STORMNET;
    using ICSSoft.STORMNET.Business;
    using Microsoft.OData.Core;
    using NewPlatform.Flexberry.ORM.ODataService.Controllers;
    using NewPlatform.Flexberry.ORM.ODataService.Events;

    /// <summary>
    /// Batch handler for DataService.
    /// </summary>
    internal class DataObjectODataBatchHandler : DefaultODataBatchHandler
    {
        /// <summary>
        /// Request Properties collection key for DataObjectsToUpdate list.
        /// </summary>
        public const string DataObjectsToUpdatePropertyKey = "DataObjectsToUpdate";

        /// <summary>
        /// Request Properties collection key for DataObjectCache instance.
        /// </summary>
        public const string DataObjectCachePropertyKey = "DataObjectCache";

        /// <summary>
        /// The container with registered events.
        /// </summary>
        private IEventHandlerContainer _events;

        /// <summary>
        /// if set to true then use synchronous mode for call subrequests.
        /// </summary>
        private readonly bool isSyncMode;

        /// <summary>
        /// DataService instance for execute queries.
        /// </summary>
        private readonly IDataService dataService;

        /// <summary>
        /// Initializes a new instance of the NewPlatform.Flexberry.ORM.ODataService.Batch.DataObjectODataBatchHandler class.
        /// </summary>
        /// <param name="dataService">DataService instance for execute queries.</param>
        /// <param name="httpServer">The System.Web.Http.HttpServer for handling the individual batch requests.</param>
        /// <param name="isSyncMode">Use synchronous mode for call subrequests.</param>
        public DataObjectODataBatchHandler(IDataService dataService, HttpServer httpServer, bool? isSyncMode = null)
            : base(httpServer)
        {
            this.dataService = dataService;

            this.isSyncMode = isSyncMode ?? Type.GetType("Mono.Runtime") != null;
        }

        /// <summary>
        /// Initializes the container with registered events.
        /// </summary>
        /// <param name="events">The container with registered events.</param>
        public void InitializeEvents(IEventHandlerContainer events)
        {
            _events = events;
        }

        /// <inheritdoc />
        public override async Task<HttpResponseMessage> ProcessBatchAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            ValidateRequest(request);

            IList<ODataBatchRequestItem> subRequests = isSyncMode
                ? ParseBatchRequestsAsync(request, cancellationToken).Result
                : await ParseBatchRequestsAsync(request, cancellationToken);

            try
            {
                if (isSyncMode)
                {
                    IList<ODataBatchResponseItem> responses = ExecuteRequestMessagesAsync(subRequests, cancellationToken).Result;
                    return CreateResponseMessageAsync(responses, request, cancellationToken).Result;
                }
                else
                {
                    IList<ODataBatchResponseItem> responses = await ExecuteRequestMessagesAsync(subRequests, cancellationToken);
                    return await CreateResponseMessageAsync(responses, request, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                return DataObjectController.InternalServerErrorMessage(ex, _events, request);
            }
            finally
            {
                foreach (ODataBatchRequestItem subRequest in subRequests)
                {
                    request.RegisterForDispose(subRequest.GetResourcesForDisposal());
                    request.RegisterForDispose(subRequest);
                }
            }
        }

        /// <inheritdoc />
        public override async Task<IList<ODataBatchRequestItem>> ParseBatchRequestsAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            ODataMessageReaderSettings oDataReaderSettings = new ODataMessageReaderSettings
            {
                DisableMessageStreamDisposal = true,
                MessageQuotas = MessageQuotas,
                BaseUri = GetBaseUri(request)
            };

            ODataMessageReader reader = isSyncMode
                ? request.Content.GetODataMessageReaderAsync(oDataReaderSettings, cancellationToken).Result
                : await request.Content.GetODataMessageReaderAsync(oDataReaderSettings, cancellationToken);

            request.RegisterForDispose(reader);

            List<ODataBatchRequestItem> requests = new List<ODataBatchRequestItem>();
            ODataBatchReader batchReader = reader.CreateODataBatchReader();
            Guid batchId = Guid.NewGuid();
            while (batchReader.Read())
            {
                switch (batchReader.State)
                {
                    case ODataBatchReaderState.ChangesetStart:
                        IList<HttpRequestMessage> changeSetRequests = isSyncMode
                            ? batchReader.ReadChangeSetRequestAsync(batchId, cancellationToken).Result
                            : await batchReader.ReadChangeSetRequestAsync(batchId, cancellationToken);

                        foreach (HttpRequestMessage changeSetRequest in changeSetRequests)
                        {
                            changeSetRequest.CopyBatchRequestProperties(request);
                        }

                        requests.Add(new ChangeSetRequestItem(changeSetRequests));
                        break;
                    case ODataBatchReaderState.Operation:
                        HttpRequestMessage operationRequest = isSyncMode
                            ? batchReader.ReadOperationRequestAsync(batchId, true, cancellationToken).Result
                            : await batchReader.ReadOperationRequestAsync(batchId, true, cancellationToken);

                        operationRequest.CopyBatchRequestProperties(request);
                        requests.Add(new OperationRequestItem(operationRequest));
                        break;
                }
            }

            return requests;
        }

        /// <inheritdoc />
        public override async Task<IList<ODataBatchResponseItem>> ExecuteRequestMessagesAsync(
                   IEnumerable<ODataBatchRequestItem> requests,
                   CancellationToken cancellation)
        {
            if (requests == null)
            {
                throw new ArgumentNullException(nameof(requests));
            }

            IList<ODataBatchResponseItem> responses = new List<ODataBatchResponseItem>();
            try
            {
                foreach (ODataBatchRequestItem request in requests)
                {
                    ODataBatchResponseItem response;
                    switch (request)
                    {
                        case OperationRequestItem operation:
                            response = isSyncMode
                                ? request.SendRequestAsync(Invoker, cancellation).Result
                                : await request.SendRequestAsync(Invoker, cancellation);
                            break;
                        case ChangeSetRequestItem change:
                            response = isSyncMode
                                ? ExecuteChangeSet(change, cancellation).Result
                                : await ExecuteChangeSet(change, cancellation);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException($"Unsupported request of type `{request.GetType()}`");
                    }

                    responses.Add(response);
                }
            }
            catch
            {
                foreach (ODataBatchResponseItem response in responses)
                {
                    response?.Dispose();
                }

                throw;
            }

            return responses;
        }

        /// <summary>
        /// Execute changeset processing.
        /// </summary>
        /// <param name="changeSet">Changeset for processing.</param>
        /// <param name="cancellation">Cancelation token.</param>
        /// <returns>Task for changeset processing.</returns>
        private async Task<ODataBatchResponseItem> ExecuteChangeSet(ChangeSetRequestItem changeSet, CancellationToken cancellation)
        {
            if (changeSet == null)
            {
                throw new ArgumentNullException(nameof(changeSet));
            }

            List<DataObject> dataObjectsToUpdate = new List<DataObject>();
            DataObjectCache dataObjectCache = new DataObjectCache();
            dataObjectCache.StartCaching(false);

            foreach (HttpRequestMessage request in changeSet.Requests)
            {
                if (!request.Properties.ContainsKey(DataObjectsToUpdatePropertyKey))
                {
                    request.Properties.Add(DataObjectsToUpdatePropertyKey, dataObjectsToUpdate);
                }

                if (!request.Properties.ContainsKey(DataObjectCachePropertyKey))
                {
                    request.Properties.Add(DataObjectCachePropertyKey, dataObjectCache);
                }
            }

            ChangeSetResponseItem changeSetResponse = isSyncMode
                ? (ChangeSetResponseItem)changeSet.SendRequestAsync(Invoker, cancellation).Result
                : (ChangeSetResponseItem)await changeSet.SendRequestAsync(Invoker, cancellation);

            if (changeSetResponse.Responses.All(r => r.IsSuccessStatusCode))
            {
                try
                {
                    Dictionary<object, ObjectStatus> stateDictionary = new Dictionary<object, ObjectStatus>();
                    foreach (DataObject dataObjectToUpdate in dataObjectsToUpdate)
                    {
                        if (!stateDictionary.ContainsKey(dataObjectToUpdate.__PrimaryKey))
                        {
                            stateDictionary.Add(dataObjectToUpdate.__PrimaryKey, dataObjectToUpdate.GetStatus());
                        }
                    }

                    DataObject[] dataObjects = dataObjectsToUpdate.ToArray();

                    dataService.UpdateObjects(ref dataObjects);

                    foreach (DataObject dataObject in dataObjectsToUpdate)
                    {
                        var state = stateDictionary[dataObject.__PrimaryKey];
                        switch (state)
                        {
                            case ObjectStatus.Created:
                                _events.CallbackAfterCreate?.Invoke(dataObject);
                                break;
                            case ObjectStatus.Altered:
                                _events.CallbackAfterUpdate?.Invoke(dataObject);
                                break;
                            case ObjectStatus.Deleted:
                                _events.CallbackAfterDelete?.Invoke(dataObject);
                                break;
                            default:
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
            }

            return changeSetResponse;
        }
    }
}
