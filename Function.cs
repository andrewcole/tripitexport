using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using Azure.Storage.Blobs;
using System.Text;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Blobs.Models;

namespace TripItExport
{
    public class Function
    {
        [FunctionName("TripItExport")]
        public async Task Run(
            [TimerTrigger("58 3 * * *")]TimerInfo myTimer,
            ILogger log,
            [Blob("tripitexport//flights.json", FileAccess.ReadWrite)] BlockBlobClient flightsJson,
            [Blob("tripitexport//flights.geojson", FileAccess.ReadWrite)] BlockBlobClient flightsGeoJson
        )
        {
            log.LogInformation($"TripItExport started at: {DateTime.Now}");

            string? access_token = Environment.GetEnvironmentVariable("access_token");
            if (access_token == null)
            {
                log.LogError("access_token settings not set!");
                return;
            }
            string? access_secret = Environment.GetEnvironmentVariable("access_secret");
            if (access_secret == null)
            {
                log.LogError("access_secret settings not set!");
                return;
            }
            string? client_token = Environment.GetEnvironmentVariable("client_token");
            if (client_token == null)
            {
                log.LogError("client_token settings not set!");
                return;
            }
            string? client_secret = Environment.GetEnvironmentVariable("client_secret");
            if (client_secret == null)
            {
                log.LogError("client_secret settings not set!");
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

            Stream outputStream = new MemoryStream();

            JsonSerializer.Serialize(outputStream, geojson, new JsonSerializerOptions() { WriteIndented = true });
            outputStream.Position = 0;
            await flightsGeoJson.UploadAsync(outputStream, new BlobHttpHeaders { ContentType = "application/json" });
            outputStream.Position = 0;

            JsonSerializer.Serialize(outputStream, flights, new JsonSerializerOptions() { WriteIndented = true });
            outputStream.Position = 0;
            await flightsJson.UploadAsync(outputStream, new BlobHttpHeaders { ContentType = "application/json" });
            outputStream.Position = 0;

            log.LogInformation($"TripItExport finished at: {DateTime.Now}");
        }
    }
}
