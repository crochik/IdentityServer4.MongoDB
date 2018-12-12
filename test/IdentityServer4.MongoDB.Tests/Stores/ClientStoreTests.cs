using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using IdentityServer4.Models;
using IdentityServer4.MongoDB.Mappers;
using IdentityServer4.MongoDB.Repositories;
using IdentityServer4.Stores;
using Xunit;

namespace IdentityServer4.MongoDB.Tests.Stores
{
    public class ClientStoreTests : IClassFixture<HostContainer>
    {
        private readonly HostContainer _hostContainer;

        public ClientStoreTests(HostContainer hostContainer)
        {
            _hostContainer = hostContainer;
        }

        [Fact]
        public async Task FindClientByIdAsync_WhenClientExists_ExpectClientRetured()
        {
            // Arrange
            var testClient = new Models.Client
            {
                ClientId = Guid.NewGuid().ToString(),
                ClientName = Guid.NewGuid().ToString(),
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                ClientSecrets = new List<Secret> { new Secret("Secret") }
            };
            var mapper = _hostContainer.Container.Resolve<IMapper>();
            var repository = _hostContainer.Container.Resolve<IRepository<Entities.Client>>();
            await repository.AddAsync(mapper.Map<Entities.Client>(testClient)).ConfigureAwait(false);

            // Assert
            var clientStore = _hostContainer.Container.Resolve<IClientStore>();
            var client = await clientStore.FindClientByIdAsync(testClient.ClientId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(client);
        }

        [Fact]
        public async Task FindClientByIdAsync_WhenClientExists_ExpectClientPropertiesRetured()
        {
            // Arrange
            var testClient = new Models.Client
            {
                ClientId = Guid.NewGuid().ToString(),
                ClientName = Guid.NewGuid().ToString(),
                Properties =
                {
                    {"foo1", "bar1"},
                    {"foo2", "bar2"},
                },
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                ClientSecrets = new List<Secret> {new Secret("Secret")}
            };
            var mapper = _hostContainer.Container.Resolve<IMapper>();
            var repository = _hostContainer.Container.Resolve<IRepository<Entities.Client>>();
            await repository.AddAsync(mapper.Map<Entities.Client>(testClient)).ConfigureAwait(false);

            // Assert
            var clientStore = _hostContainer.Container.Resolve<IClientStore>();
            var client = await clientStore.FindClientByIdAsync(testClient.ClientId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(client);
            Assert.Equal(2, client.Properties.Count);
            Assert.True(client.Properties.ContainsKey("foo1"));
            Assert.True(client.Properties.ContainsKey("foo2"));
            Assert.Equal("bar1", client.Properties["foo1"]);
            Assert.Equal("bar2", client.Properties["foo2"]);
        }
    }
}