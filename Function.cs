using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Configuration;
using Azure.Storage.Blobs;
using System.Text;

namespace TripItExport
{
    public class Function
    {
        [FunctionName("TripItExport")]
        public async Task Run([TimerTrigger("58 3 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"TripItExport started at: {DateTime.Now}");

            string? access_token = ConfigurationManager.AppSettings["access_token"];
            if (access_token == null)
            {
                log.LogError("access_token settings not set!");
                return;
            }
            string? access_secret = ConfigurationManager.AppSettings["access_secret"];
            if (access_secret == null)
            {
                log.LogError("access_secret settings not set!");
                return;
            }
            string? client_token = ConfigurationManager.AppSettings["client_token"];
            if (client_token == null)
            {
                log.LogError("client_token settings not set!");
                return;
            }
            string? client_secret = ConfigurationManager.AppSettings["client_secret"];
            if (client_secret == null)
            {
                log.LogError("client_secret settings not set!");
                return;
            }
            string? storage_connection_string = ConfigurationManager.AppSettings["storage_connection_string"];
            if (storage_connection_string == null)
            {
                log.LogError("storage_connection_string not set!");
                return;
            }

            var client = new Client(
                client_token,
                client_secret,
                access_token,
                access_secret);

            var userUUID = await client.GetUserUUID();
            if (userUUID == null)
            {
                log.LogError("User UUID not returned!");
                return;
            }
            log.LogInformation($"User UUID {userUUID} receieved.");

            var flights = await client.GetFlights();
            if (flights is null)
            {
                log.LogError("Flights not returned!");
                return;
            }
            log.LogInformation($"{flights.Length} flights received.");

            var geojson = new
            {
                type = "FeatureCollection",
                features = flights.Select(flight => new
                {
                    geometry = new
                    {
                        coordinates = new[]
                        {
                            new[]
                            {
                                new[]
                                {
                                    flight.Origin.Longitude,
                                    flight.Origin.Latitude,
                                },
                                new[]
                                {
                                    flight.Destination.Longitude,
                                    flight.Destination.Latitude,
                                },
                            },
                        },
                        type = "MultiLineString",
                    },
                    properties = new
                    {
                        name = $"{flight.Origin.Code} - {flight.Destination.Code}"
                    },
                    type = "Feature",
                })
            };

            BlobContainerClient container = new BlobContainerClient(storage_connection_string, "geojson");
            var blob = container.GetBlobClient($"{userUUID}.geojson");
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(geojson, new JsonSerializerOptions { WriteIndented = true }))))
            {
                await blob.UploadAsync(ms);
            }

            log.LogInformation($"TripItExport finished at: {DateTime.Now}");
        }
    }
}
