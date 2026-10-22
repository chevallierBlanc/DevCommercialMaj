-- LECTURE SEULE, a executer par le DBA. Les DMV serveur demandent des droits
-- de diagnostic (VIEW SERVER STATE ou VIEW SERVER PERFORMANCE STATE selon version).
-- Ne pas attribuer ces droits au compte applicatif ordinaire pour ce diagnostic.
SET NOCOUNT ON;
SELECT @@SERVERNAME AS Serveur,DB_NAME() AS BaseInspectee,SYSUTCDATETIME() AS DateUtc,
 SERVERPROPERTY('ProductVersion') AS VersionSql,
 CONNECTIONPROPERTY('net_transport') AS Transport,
 CONNECTIONPROPERTY('local_net_address') AS AdresseServeur,
 CONNECTIONPROPERTY('local_tcp_port') AS PortServeur,
 CONNECTIONPROPERTY('client_net_address') AS AdresseClient;
SELECT TOP(100) r.session_id,r.blocking_session_id,r.status,r.command,
 r.wait_type,r.wait_time,r.total_elapsed_time,r.open_transaction_count,
 s.host_name,s.program_name,s.login_time
FROM sys.dm_exec_requests r JOIN sys.dm_exec_sessions s ON s.session_id=r.session_id
WHERE s.is_user_process=1 ORDER BY r.blocking_session_id DESC,r.total_elapsed_time DESC;
SELECT TOP(100) s.session_id,s.host_name,s.program_name,s.status,
 s.open_transaction_count,s.last_request_start_time,s.last_request_end_time,
 c.net_transport,c.encrypt_option,c.auth_scheme,c.client_net_address,c.local_tcp_port
FROM sys.dm_exec_sessions s LEFT JOIN sys.dm_exec_connections c ON c.session_id=s.session_id
WHERE s.is_user_process=1 ORDER BY s.host_name,s.session_id;
-- Aucune terminaison de session, aucun KILL, aucune configuration modifiee.
