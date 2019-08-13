using System;
using System.Threading.Tasks;
using Autofac;
using IdentityServer4.MongoDB.Entities;
using IdentityServer4.MongoDB.Repositories;
using IdentityServer4.MongoDB.Tokens;
using MongoDB.Driver.Linq;
using Xunit;

namespace IdentityServer4.MongoDB.Tests.Tokens
{
    public class TokenCleanupTests : IClassFixture<HostContainer>
    {
        private readonly HostContainer _hostContainer;

        public TokenCleanupTests(HostContainer hostContainer)
        {
            _hostContainer = hostContainer;

            _hostContainer.Register<TokenCleanup>();
        }

        [Fact]
        public async Task RemoveExpiredGrantsAsync()
        {
            using (var scope = _hostContainer.Container.BeginLifetimeScope())
            {
                // Arrange
                var repo = scope.Resolve<IRepository<PersistedGrant>>();
                var expiredPersistedGrant = await repo.AddAsync(new PersistedGrant
                {
                    SubjectId = Guid.NewGuid().ToString(),
                    Expiration = DateTime.UtcNow.AddDays(-1)
                }).ConfigureAwait(false);

                // Act
                var tokenCleanup = scope.Resolve<TokenCleanup>();
                await tokenCleanup.RemoveExpiredGrantsAsync();

                // Assert
                var found = await repo.AsQueryable().Where(x => x.Id == expiredPersistedGrant.Id).CountAsync() > 0;
                Assert.False(found);
            }
        }
    }
}