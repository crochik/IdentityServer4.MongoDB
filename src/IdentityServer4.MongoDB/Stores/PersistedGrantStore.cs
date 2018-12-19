using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.MongoDB.Mappers;
using IdentityServer4.MongoDB.Repositories;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IdentityServer4.MongoDB.Stores
{
    public class PersistedGrantStore : IPersistedGrantStore
    {
        private readonly IRepository<Entities.PersistedGrant> _persistedGrantRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<PersistedGrantStore> _logger;

        public PersistedGrantStore(IRepository<Entities.PersistedGrant> persistedGrantRepository, IMapper mapper, ILogger<PersistedGrantStore> logger)
        {
            _persistedGrantRepository = persistedGrantRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task StoreAsync(PersistedGrant grant)
        {
            var existing = await _persistedGrantRepository.AsQueryable().FirstOrDefaultAsync(x => x.Key == grant.Key).ConfigureAwait(false);
            if (existing == null)
            {
                _logger.LogDebug("{persistedGrantKey} not found in database", grant.Key);

                var persistedGrant = _mapper.Map<Entities.PersistedGrant>(grant);
                await _persistedGrantRepository.AddAsync(persistedGrant).ConfigureAwait(false);
            }
            else
            {
                _logger.LogDebug("{persistedGrantKey} found in database", grant.Key);

                var persistedGrant = _mapper.Map(grant, existing);
                await _persistedGrantRepository.UpdateAsync(persistedGrant).ConfigureAwait(false);
            }
        }

        public async Task<PersistedGrant> GetAsync(string key)
        {
            var persistedGrant = await _persistedGrantRepository.AsQueryable().FirstOrDefaultAsync(x => x.Key == key).ConfigureAwait(false);
            var model = _mapper.Map<PersistedGrant>(persistedGrant);

            _logger.LogDebug("{persistedGrantKey} found in database: {persistedGrantKeyFound}", key, model != null);

            return model;
        }

        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            var persistedGrants = await _persistedGrantRepository.AsQueryable().Where(x => x.SubjectId == subjectId).ToListAsync().ConfigureAwait(false);
            var model = _mapper.Map<IEnumerable<PersistedGrant>>(persistedGrants);

            _logger.LogDebug("{persistedGrantCount} persisted grants found for {subjectId}", persistedGrants.Count, subjectId);

            return model;
        }

        public async Task RemoveAsync(string key)
        {
            var result = await _persistedGrantRepository.Collection
                .DeleteOneAsync(x => x.Key == key)
                .ConfigureAwait(false);

            if (result.DeletedCount > 0)
            {
                _logger.LogDebug("removed {persistedGrantKey} persisted grant from database", key);
            }
            else
            {
                _logger.LogDebug("no {persistedGrantKey} persisted grant found in database", key);
            }
        }

        public async Task RemoveAllAsync(string subjectId, string clientId)
        {
            var result = await _persistedGrantRepository.Collection
                .DeleteManyAsync(x => x.SubjectId == subjectId && x.ClientId == clientId)
                .ConfigureAwait(false);

            _logger.LogDebug("removed {persistedGrantCount} persisted grants from database for subject {subjectId}, clientId {clientId}", result.DeletedCount, subjectId, clientId);
        }

        public async Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            var result = await _persistedGrantRepository.Collection
                .DeleteManyAsync(x => x.SubjectId == subjectId && x.ClientId == clientId && x.Type == type)
                .ConfigureAwait(false);

            _logger.LogDebug("removed {persistedGrantCount} persisted grants from database for subject {subjectId}, clientId {clientId}, grantType {persistedGrantType}", result.DeletedCount, subjectId, clientId, type);
        }
    }
}