using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace EasyData.Services
{

    public class EasyDataManagerException : Exception
    {
        public EasyDataManagerException(string message) : base(message)
        { }
    }

    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(string entityContainer, string entityKey)
            : base($"Entity with id {entityKey} is not found in container: {entityContainer}")
        { }
    }

    public class ContainerNotFoundException : EasyDataManagerException
    {
        public ContainerNotFoundException(string entityContainer) : base($"Container is not found: {entityContainer}")
        { }
    }

    public abstract class EasyDataManager : IDisposable
    {

        protected readonly IServiceProvider Services;

        protected readonly EasyDataOptions Options;

        protected ConcurrentDictionary<string, MetaData> MetadataSchemas { get; private set; } = new ConcurrentDictionary<string, MetaData>();


        public EasyDataManager(IServiceProvider services, EasyDataOptions options)
        {
            Services = services;
            Options = options;
        }

        public async Task<MetaData> GetModelAsync(string modelId, CancellationToken ct = default)
        {
            if (!MetadataSchemas.TryGetValue(modelId, out var model)) {
                model = await InitializeMetadataSchemaAsync(modelId, ct);

                // See if there was a model created with specified id (maybe from a parallel thread).
                if (!MetadataSchemas.TryAdd(modelId, model)) {
                    // Discard the created model, use the one from dictionary.
                    model = MetadataSchemas[modelId];
                }
            }

            return model;
        }

        /// <summary>
        /// Create a new model instance.
        /// </summary>
        /// <param name="modelId">Id of the model.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Metadata schema.</returns>
        protected async Task<MetaData> InitializeMetadataSchemaAsync(string modelId, CancellationToken ct = default)
        {
            var model = new MetaData()
            {
                Id = modelId
            };
            await LoadModelAsync(model, ct);
            UpdateModelMetaWithOptions(model);
            return model;
        }

        /// <summary>
        /// Load metadata with properties.
        /// </summary>
        /// <param name="metaData">Metadata object.</param>
        /// <param name="ct">Cancellation token.</param>
        public virtual Task LoadModelAsync(MetaData metaData, CancellationToken ct = default)
        {
            Options.ModelTuner?.Invoke(metaData);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Update Model meta data with the properties from options.
        /// </summary>
        private void UpdateModelMetaWithOptions(MetaData metaData)
        {
            foreach (var metaBuilder in Options.EntityMetaBuilders) {
                var entity = metaData.EntityRoot.SubEntities.FirstOrDefault(e => metaBuilder.ClrType.Equals(e.ClrType));
                if (entity == null) {
                    // TODO: should we throw an exception?
                    continue;
                }

                // Remove from list if the entity is disabled in options
                if (metaBuilder.Enabled == false) {
                    metaData.EntityRoot.SubEntities.Remove(entity);
                    continue;
                }

                UpdateEntityMeta(metaBuilder, entity);
            }
        }

        /// <summary>
        /// Update single entity meta information.
        /// </summary>
        /// <param name="updateSource">Meta builder update source.</param>
        /// <param name="updateTarget">Meta Entity to update.</param>
        private static void UpdateEntityMeta(IEntityMetaBuilder updateSource, MetaEntity updateTarget)
        {
            updateTarget.Description = updateSource.Description ?? updateTarget.Description;
            updateTarget.Name = updateSource.DisplayName ?? updateTarget.Name;
            updateTarget.NamePlural = updateSource.DisplayNamePlural ?? updateTarget.NamePlural;

            // Update entity meta attributes
            foreach (var attributeBuilder in updateSource.AttributeMetaBuilders) {
                // TODO: attributes
                for (int i = updateTarget.Attributes.Count - 1; i >= 0; i--) {
                    var attribute = updateTarget.Attributes[i];

                    if (attributeBuilder.PropertyInfo.Name.Equals(attribute.PropName)) {
                        if (!attributeBuilder.IsEnabled ?? true) {
                            updateTarget.Attributes.RemoveAt(i);
                        }

                        UdpateEntityAttributeMeta(attributeBuilder, attribute);
                        updateTarget.Attributes[i] = attribute;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Update sungle entity attribute meta information.
        /// </summary>
        /// <param name="metaUpdateSource">Meta builder update source.</param>
        /// <param name="metaToUpdate">Meta Entity Attribute to update.</param>
        private static void UdpateEntityAttributeMeta(EntityAttributeMetaBuilder metaUpdateSource, MetaEntityAttr metaToUpdate)
        {
            metaToUpdate.Caption = metaUpdateSource.DisplayName ?? metaToUpdate.Caption;
            metaToUpdate.DisplayFormat = metaUpdateSource.DisplayFormat ?? metaToUpdate.DisplayFormat;
            metaToUpdate.Description = metaUpdateSource.Description ?? metaToUpdate.Description;
            metaToUpdate.IsEditable = metaUpdateSource.IsEditable ?? metaToUpdate.IsEditable;
            metaToUpdate.Index = metaUpdateSource.Index ?? metaToUpdate.Index;
            metaToUpdate.ShowInLookup = metaUpdateSource.ShowInLookup ?? metaToUpdate.ShowInLookup;
            metaToUpdate.ShowOnView = metaUpdateSource.ShowOnView ?? metaToUpdate.ShowOnView;
            metaToUpdate.ShowOnEdit = metaUpdateSource.ShowOnEdit ?? metaToUpdate.ShowOnEdit;
            metaToUpdate.ShowOnCreate = metaUpdateSource.ShowOnCreate ?? metaToUpdate.ShowOnCreate;
            metaToUpdate.Sorting = metaUpdateSource.Sorting ?? metaToUpdate.Sorting;
        }

        public abstract Task<EasyDataResultSet> GetEntitiesAsync(
            string modelId, string entityContainer,
            IEnumerable<EasyFilter> filters = null,
            IEnumerable<EasySorter> sorters = null,
            bool isLookup = false,
            int? offset = null, int? fetch = null,
            CancellationToken ct = default);

        public abstract Task<long> GetTotalEntitiesAsync(string modelId, string entityContainer, IEnumerable<EasyFilter> filters = null, bool isLookup = false, CancellationToken ct = default);

        public abstract Task<object> GetEntityAsync(string modelId, string entityContainer, string keyStr, CancellationToken ct = default);

        public abstract Task<object> CreateEntityAsync(string modelId, string entityContainer, JObject props, CancellationToken ct = default);

        public abstract Task<object> UpdateEntityAsync(string modelId, string entityContainer, string keyStr, JObject props, CancellationToken ct = default);

        public abstract Task DeleteEntityAsync(string modelId, string entityContainer, string keyStr, CancellationToken ct = default);

        public abstract Task<IEnumerable<EasySorter>> GetDefaultSortersAsync(string modelId, string entityContainer, CancellationToken ct = default);

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            //no resources to dispose
            //we just have defined this method for derived classes
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
