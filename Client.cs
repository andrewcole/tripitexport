using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace TripItExport
{
    public record Airport(string Code, double Latitude, double Longitude);
    public record Flight(Airport Origin, Airport Destination);


    public class Client : IDisposable
    {

        readonly RestClient _client;

        public Client(
            string clientToken,
            string clientTokenSecret,
            string accessToken,
            string accessTokenSecret)
        {
            var options = new RestClientOptions("https://api.tripit.com");

            _client = new RestClient(options)
            {
                Authenticator = OAuth1Authenticator.ForProtectedResource(
                    clientToken,
                    clientTokenSecret,
                    accessToken,
                    accessTokenSecret),
            };
        }
        public async Task<Guid?> GetUserUUID()
        {
            var url = $"/v1/get/profile?format=json".ToLower();

            RestResponse? response = await _client.GetAsync(new RestRequest(url));
            if (response is null || response.Content is null) return null;

            JsonDocument? jsonDocument = JsonDocument.Parse(response.Content);
            if (jsonDocument is null) return null;

            JsonElement rootElement = jsonDocument.RootElement;

            if (!rootElement.TryGetProperty("Profile", out JsonElement profile) ||
                profile.ValueKind != JsonValueKind.Object ||
                !profile.TryGetPropertyAsString("screen_name", out string screenName) ||
                !profile.TryGetPropertyAsGuid("uuid", out Guid uuid)) return null;


            return uuid;
        }

        public async Task<Flight[]> GetFlights()
        {
            var objects = true;
            var page_size = 10;
            var format = "json";
            var traveler = true;
            var past = true;


            var page = 1;
            var result = new HashSet<Flight>();

            while (true)
            {
                var url = $"/v1/list/trip/traveler/{traveler}/past/{past}/include_objects/{objects}?format={format}&page_size={page_size}&page_num={page}".ToLower();

                RestResponse? response = await _client.GetAsync(new RestRequest(url));
                if (response is null || response.Content is null) break;

                JsonDocument? jsonDocument = JsonDocument.Parse(response.Content);
                if (jsonDocument is null) break;

                JsonElement rootElement = jsonDocument.RootElement;

                if (rootElement.TryGetPropertyAsArray("AirObject", out JsonElement[] airs))
                {
                    foreach (var air in airs)
                    {
                        if (air.TryGetPropertyAsArray("Segment", out JsonElement[] segments))
                        {
                            foreach (var segment in segments)
                            {
                                if (segment.TryGetPropertyAsString("start_airport_code", out string startAirportCode) &&
                                    segment.TryGetPropertyAsDouble("start_airport_latitude", out double startAirportLatitude) &&
                                    segment.TryGetPropertyAsDouble("start_airport_longitude", out double startAirportLongitude) &&
                                    segment.TryGetPropertyAsString("end_airport_code", out string endAirportCode) &&
                                    segment.TryGetPropertyAsDouble("end_airport_latitude", out double endAirportLatitude) &&
                                    segment.TryGetPropertyAsDouble("end_airport_longitude", out double endAirportLongitude))
                                {
                                    result.Add(new Flight(
                                        new Airport(
                                            startAirportCode,
                                            startAirportLatitude,
                                            startAirportLongitude),
                                        new Airport(
                                            endAirportCode,
                                            endAirportLatitude,
                                            endAirportLongitude)
                                    ));
                                }
                            }
                        }
                    }
                }

                if (!rootElement.TryGetPropertyAsInt("max_page", out int maxPage)) break;
                if (page >= maxPage) break;
                page++;
            }

            return result.ToArray();
        }

        public void Dispose()
        {
            _client?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}