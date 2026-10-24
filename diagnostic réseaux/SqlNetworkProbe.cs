// Collecteur de lecture seule, independant de l'execution des operations ERP.
// Compatible avec Add-Type de Windows PowerShell 5.1 / .NET Framework 4.7.2.
// Garder le fournisseur de l'ERP ; la migration Microsoft.Data.SqlClient est hors perimetre.
#pragma warning disable 0618
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CommercialPro.Diagnostics
{
    public sealed class ProbeError
    {
        public string Type { get; set; }
        public int? Number { get; set; }
        public int? State { get; set; }
        public int? Severity { get; set; }
        public string Category { get; set; }
    }
    public sealed class ProbeStage
    {
        public string Stage { get; set; }
        public string Target { get; set; }
        public string Status { get; set; }
        public DateTime StartedUtc { get; set; }
        public DateTime EndedUtc { get; set; }
        public double DurationMs { get; set; }
        public string[] Addresses { get; set; }
        public string Transport { get; set; }
        public int? ObservedSqlPort { get; set; }
        public string ConnectionId { get; set; }
        public ProbeError[] Errors { get; set; }
        public ProbeStage(string stage, string target)
        {
            Stage = stage; Target = target; Status = "SUCCES";
            StartedUtc = DateTime.UtcNow;
            Addresses = new string[0]; Errors = new ProbeError[0];
        }
    }
    public sealed class ProbeEndpoint
    {
        public string Host { get; set; }
        public int Port { get; set; }
    }
    public static class SqlNetworkProbe
    {
        public static ProbeEndpoint ParseEndpoint(string source, int port)
        {
            string value = (source ?? "").Trim();
            if (value.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase)) value = value.Substring(4);
            if (value.Length == 0 || value.StartsWith("np:", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("lpc:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("(localdb)", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Adresse reseau SQL non determinable. Faire verifier la configuration.");
            int comma = value.LastIndexOf(',');
            if (comma >= 0)
            {
                int configured;
                if (!int.TryParse(value.Substring(comma + 1), out configured) || configured < 1 || configured > 65535)
                    throw new ArgumentException("Port SQL configure invalide.");
                if (port != 0 && port != configured) throw new ArgumentException("Le port fourni differe du port configure.");
                port = configured; value = value.Substring(0, comma).Trim();
            }
            int instance = value.IndexOf('\\');
            if (instance >= 0) value = value.Substring(0, instance);
            value = value.Trim('[', ']');
            if (value == "." || value.Equals("(local)", StringComparison.OrdinalIgnoreCase)) value = "localhost";
            if (value.Length == 0 || port < 1 || port > 65535)
                throw new ArgumentException("Fournir le port TCP reel de l'instance ; 1433 n'est jamais suppose.");
            return new ProbeEndpoint { Host = value, Port = port };
        }
        public static string ClassifySql(int number, bool opening)
        {
            switch (number)
            {
                case -2: return opening ? "DELAI_CONNEXION" : "DELAI_COMMANDE";
                case 1205: return "INTERBLOCAGE";
                case 1222: return "ATTENTE_VERROU";
                case 18456: case 18452: return "AUTHENTIFICATION";
                case 4060: case 942: return "BASE_INDISPONIBLE";
                case 53: case 26: case 11001: return "ADRESSE_INSTANCE";
                case 10054: case 10053: case 10060: case 10061: case 64: case 233: return "TRANSPORT";
                case 102: case 207: case 208: case 229: case 547: case 2601: case 2627: case 2812: return "SQL_APPLICATIF";
                default: return "SQL_NON_CLASSE";
            }
        }
        private static void Fail(ProbeStage result, Exception exception, bool opening)
        {
            result.Status = "ECHEC";
            while (exception is AggregateException && exception.InnerException != null) exception = exception.InnerException;
            var sql = exception as SqlException;
            if (sql != null)
            {
                result.ConnectionId = sql.ClientConnectionId.ToString("D");
                result.Errors = sql.Errors.Cast<SqlError>().Select(e => new ProbeError {
                    Type = "SqlException", Number = e.Number, State = e.State,
                    Severity = e.Class, Category = ClassifySql(e.Number, opening) }).ToArray();
                return;
            }
            var socket = exception as SocketException;
            result.Errors = new[] { new ProbeError { Type = exception.GetType().Name,
                Number = socket == null ? (int?)null : socket.NativeErrorCode,
                Category = exception is TimeoutException || exception is OperationCanceledException ? "DELAI_SONDE" : "RESEAU_OU_CLIENT" } };
            // Aucun Message/StackTrace : un fournisseur peut y inclure un secret.
        }
        private static void End(ProbeStage result, Stopwatch watch)
        {
            result.EndedUtc = DateTime.UtcNow; result.DurationMs = watch.Elapsed.TotalMilliseconds;
        }
        private static async Task<T> Bounded<T>(Task<T> task, int timeoutMs)
        {
            if (await Task.WhenAny(task, Task.Delay(timeoutMs)).ConfigureAwait(false) != task)
            {
                Observe(task);
                throw new TimeoutException();
            }
            return await task.ConfigureAwait(false);
        }
        private static void Observe(Task task)
        {
            task.ContinueWith(t => { var ignored = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        }
        private static async Task<ProbeStage> DnsAsync(string host, int timeoutMs)
        {
            var result = new ProbeStage("DNS", host); var watch = Stopwatch.StartNew();
            try
            {
                IPAddress literal;
                var addresses = IPAddress.TryParse(host, out literal) ? new[] { literal } :
                    await Bounded(Dns.GetHostAddressesAsync(host), timeoutMs).ConfigureAwait(false);
                result.Addresses = addresses.Select(a => a.ToString()).Distinct().ToArray();
                if (result.Addresses.Length == 0) throw new SocketException((int)SocketError.HostNotFound);
            }
            catch (Exception ex) { Fail(result, ex, true); }
            finally { End(result, watch); }
            return result;
        }
        public static async Task<ProbeStage> TcpAsync(string address, int port, int timeoutMs)
        {
            var result = new ProbeStage("TCP", address); var watch = Stopwatch.StartNew();
            try
            {
                IPAddress ip = IPAddress.Parse(address);
                using (var tcp = new TcpClient(ip.AddressFamily))
                {
                    var task = tcp.ConnectAsync(ip, port);
                    if (await Task.WhenAny(task, Task.Delay(timeoutMs)).ConfigureAwait(false) != task)
                    { Observe(task); throw new TimeoutException(); }
                    await task.ConfigureAwait(false);
                }
            }
            catch (Exception ex) { Fail(result, ex, true); }
            finally { End(result, watch); }
            return result;
        }
        private static async Task<ProbeStage> PingAsync(string address, int timeoutMs)
        {
            var result = new ProbeStage("ICMP", address); var watch = Stopwatch.StartNew();
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await Bounded(ping.SendPingAsync(IPAddress.Parse(address), timeoutMs), timeoutMs + 100).ConfigureAwait(false);
                    if (reply.Status != IPStatus.Success)
                    {
                        result.Status = "ECHEC";
                        result.Errors = new[] { new ProbeError { Type = "IPStatus", Number = (int)reply.Status, Category = "ICMP_" + reply.Status.ToString() } };
                    }
                }
            }
            catch (Exception ex) { Fail(result, ex, true); }
            finally { End(result, watch); }
            return result;
        }
        private static async Task<ProbeStage[]> SqlAsync(string connectionString, int seconds, bool pooling)
        {
            var opening = new ProbeStage("SQL_OUVERTURE", "configuration_active");
            ProbeStage query = null;
            var watch = Stopwatch.StartNew();
            try
            {
                var options = new SqlConnectionStringBuilder(connectionString);
                if (options.UserInstance || !string.IsNullOrWhiteSpace(options.AttachDBFilename))
                    throw new ArgumentException("Rattachement de fichier interdit dans le diagnostic.");
                options.Pooling = pooling; options.MinPoolSize = 0; options.MaxPoolSize = 2;
                options.ApplicationName = "Commercial Pro - Diagnostic reseau";
                options.ConnectTimeout = seconds; options.ConnectRetryCount = 0;
                // TLS, identite SQL/Windows et cible restent ceux du fichier lu.
                using (var cn = new SqlConnection(options.ConnectionString))
                {
                    using (var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(seconds)))
                        await cn.OpenAsync(cancel.Token).ConfigureAwait(false);
                    opening.ConnectionId = cn.ClientConnectionId.ToString("D");
                    End(opening, watch);
                    query = new ProbeStage("SQL_REQUETE", "SELECT_1_ET_METADONNEES_SESSION");
                    watch.Restart();
                    try
                    {
                        using (var cmd = new SqlCommand("SELECT CAST(1 AS int),CONVERT(nvarchar(40),CONNECTIONPROPERTY('net_transport')),CONVERT(nvarchar(48),CONNECTIONPROPERTY('local_net_address')),CONVERT(int,CONNECTIONPROPERTY('local_tcp_port'))", cn))
                        using (var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(seconds)))
                        {
                            cmd.CommandTimeout = seconds;
                            using (var reader = await cmd.ExecuteReaderAsync(cancel.Token).ConfigureAwait(false))
                            {
                                if (!await reader.ReadAsync(cancel.Token).ConfigureAwait(false) || reader.GetInt32(0) != 1) throw new InvalidOperationException();
                                query.Transport = reader.IsDBNull(1) ? null : reader.GetString(1);
                                query.Addresses = reader.IsDBNull(2) ? new string[0] : new[] { reader.GetString(2) };
                                query.ObservedSqlPort = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
                                query.ConnectionId = cn.ClientConnectionId.ToString("D");
                            }
                        }
                    }
                    catch (Exception ex) { Fail(query, ex, false); }
                    finally { End(query, watch); }
                }
            }
            catch (Exception ex)
            {
                if (query == null) { Fail(opening, ex, true); End(opening, watch); }
                else { Fail(query, ex, false); End(query, watch); }
            }
            if (query == null) { query = new ProbeStage("SQL_REQUETE", "NON_EXECUTEE"); query.Status = "IGNORE"; End(query, Stopwatch.StartNew()); }
            return new[] { opening, query };
        }
        public static async Task<ProbeStage[]> MeasureAsync(string connectionString, string host, int port, int networkMs, int sqlSeconds, bool pooling, bool sqlEnabled = true)
        {
            // SQL est lance sans attendre ICMP/TCP. Un ping refuse ne bloque pas l'authentification.
            var dnsTask = DnsAsync(host, networkMs);
            var sqlTask = sqlEnabled ? SqlAsync(connectionString, sqlSeconds, pooling) : Task.FromResult(new[] {
                new ProbeStage("SQL_OUVERTURE", "SUSPENDU_AUTHENTIFICATION") { Status = "IGNORE", EndedUtc = DateTime.UtcNow },
                new ProbeStage("SQL_REQUETE", "SUSPENDU_AUTHENTIFICATION") { Status = "IGNORE", EndedUtc = DateTime.UtcNow } });
            var dns = await dnsTask.ConfigureAwait(false);
            var results = new List<ProbeStage> { dns };
            if (dns.Addresses.Length == 0)
            {
                foreach (var name in new[] { "TCP", "ICMP" })
                {
                    var skipped = new ProbeStage(name, host); skipped.Status = "IGNORE";
                    End(skipped, Stopwatch.StartNew()); results.Add(skipped);
                }
            }
            var networks = new List<Task<ProbeStage>>();
            foreach (string address in dns.Addresses)
            {
                networks.Add(TcpAsync(address, port, networkMs));
                networks.Add(PingAsync(address, networkMs));
            }
            results.AddRange(await Task.WhenAll(networks).ConfigureAwait(false));
            results.AddRange(await sqlTask.ConfigureAwait(false));
            return results.ToArray();
        }
    }
}
