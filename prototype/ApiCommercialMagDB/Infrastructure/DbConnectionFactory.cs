using Microsoft.Data.SqlClient;

namespace CommercialMagDb.Api.Infrastructure;

public sealed class DbConnectionFactory(IConfiguration configuration)
{
    public SqlConnection Create()
    {
        var cs = configuration.GetConnectionString("CommercialMagDB")
            ?? throw new InvalidOperationException("La chaîne de connexion CommercialMagDB est manquante.");
        return new SqlConnection(cs);
    }
}
