using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace Roma.Services
{
    /// <summary>
    /// Service for measuring server ping/latency.
    /// Note: Many game servers block ICMP ping for security/DDoS protection,
    /// so timeouts and failures are expected and normal behavior.
    /// </summary>
    public class PingService
    {
        private static bool _verboseLogging = false; // Set to true for detailed debugging

        /// <summary>
        /// Attempts to ping a server and returns the round-trip time in milliseconds.
        /// Returns -1 if the ping fails (timeout, blocked, or unreachable).
        /// </summary>
        public async Task<int> GetPingAsync(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return -1;

            try
            {
                using var ping = new Ping();

                // Use a 2 second timeout - game servers often block ping anyway
                var reply = await ping.SendPingAsync(host, 2000);

                if (reply?.Status == IPStatus.Success)
                {
                    return (int)reply.RoundtripTime;
                }

                // Only log non-success status if verbose logging is enabled
                // TimeOut is very common and expected for game servers
                if (_verboseLogging && reply != null && reply.Status != IPStatus.TimedOut)
                {
                    Debug.WriteLine($"Ping to {host} returned status: {reply.Status}");
                }
            }
            catch (PingException ex)
            {
                // Common for unreachable servers or blocked ICMP - only log in verbose mode
                if (_verboseLogging)
                {
                    Debug.WriteLine($"Ping failed for {host}: {ex.Message}");
                }
            }
            catch (System.Net.Sockets.SocketException)
            {
                // DNS or network errors - silently fail (very common)
            }
            catch (InvalidOperationException)
            {
                // Ping object disposed or invalid state - silently fail
            }
            catch (Exception ex)
            {
                // Catch-all for any other exceptions - only log unexpected errors
                if (_verboseLogging)
                {
                    Debug.WriteLine($"Unexpected ping error for {host}: {ex.GetType().Name} - {ex.Message}");
                }
            }

            return -1; // Indicates ping failure
        }
    }
}
