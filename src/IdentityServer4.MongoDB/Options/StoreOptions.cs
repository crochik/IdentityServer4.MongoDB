using MongoDB.Driver;

namespace IdentityServer4.MongoDB.Options
{
    public class StoreOptions
    {
        public string ConnectionString { get; set; }
        public string CollectionNamePrefix { get; set; }
        public ReadPreference ReadPreference { get; set; }
    }
}