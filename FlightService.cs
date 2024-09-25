using CsvHelper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightDataScraper
{
    public class FlightService
    {
        // Method to fetch data from API
        public static async Task<List<Flight>> FetchFlightData(string origin, string destination, string outboundDate, string inboundDate)
        {
            // Constructing the API URL based on user input
            string apiUrl = $"http://homeworktask.infare.lt/search.php?from={origin}&to={destination}&depart={outboundDate}&return={inboundDate}";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    Console.WriteLine($"Fetching data from: {apiUrl}");
                    // Send HTTP GET request to the API
                    HttpResponseMessage response = await client.GetAsync(apiUrl);

                    // Check if the request was successful
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Error: Received status code {response.StatusCode}");
                        return null;
                    }

                    string responseBody = await response.Content.ReadAsStringAsync();

                    // Check if the response is HTML (indicating a route isn't available)
                    if (responseBody.StartsWith("<"))
                    {
                        Console.WriteLine($"Route Not Available. The server returned an HTML response instead of flight data.");
                        return null;
                    }

                    // Deserialize JSON response into ApiResponse object
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseBody);
                    if (apiResponse == null || apiResponse.Body == null || apiResponse.Body.Data == null)
                    {
                        Console.WriteLine("Error: Invalid response data. Please check your input and try again.");
                        return null;
                    }

                    List<Flight> flightData = new List<Flight>();
                    bool hasDirectFlights = false; // Track if direct flights exist

                    if (apiResponse.Body?.Data?.Journeys != null)
                    {
                        // Process each journey in the response
                        foreach (var journey in apiResponse.Body.Data.Journeys)
                        {
                            // Check if there are direct or one-stop flights
                            if (journey.NumberOfConnections <= 1)
                            {
                                hasDirectFlights = true; 
                            }

                            // Skip journeys with more than one connection
                            if (journey.NumberOfConnections > 1) 
                            {
                                continue;
                            }

                            // Create a Flight object based on the journey
                            var flight = new Flight
                            {
                                RecommendationId = journey.RecommendationId,
                                Price = apiResponse.Body.Data.TotalAvailabilities
                                           .FirstOrDefault(t => t.RecommendationId == journey.RecommendationId)?.Total ?? 0m,
                                Taxes = journey.ImportTaxAdl
                            };

                            // Add flight legs to the Flight object
                            foreach (var flightInfo in journey.Flights)
                            {
                                var leg = new FlightLeg
                                {
                                    Origin = flightInfo.AirportDeparture.Code,
                                    Destination = flightInfo.AirportArrival.Code,
                                    DepartureTime = DateTime.ParseExact(flightInfo.DateDeparture, "yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture),
                                    ArrivalTime = DateTime.ParseExact(flightInfo.DateArrival, "yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture),
                                    FlightNumber = flightInfo.companyCode + flightInfo.number, // Updated to match the JSON structure
                                };

                                // Add to outbound or inbound legs based on journey direction
                                if (journey.Direction == "I")  // "I" for outbound
                                {
                                    flight.OutboundLegs.Add(leg);
                                }
                                else if (journey.Direction == "V")  // "V" for inbound
                                {
                                    flight.InboundLegs.Add(leg);
                                }
                            }

                            flightData.Add(flight); // Add the processed flight to the list
                        }
                    }
                    // Message if there are no direct or one-stop flights
                    if (!hasDirectFlights)
                    {
                        Console.WriteLine("Skipped journeys with more than 1 connection");
                    }

                    return flightData;
                }
                catch (Exception ex)
                {
                    // Catch any exceptions and print the error message
                    Console.WriteLine($"Error fetching flight data: {ex.Message}");
                    return null;
                }
            }
        }

        // Method to create roundtrip flight combinations from outbound and inbound flights
        public static List<Flight> CreateRoundtripCombinations(List<Flight> flights)
        {
            List<Flight> roundtripCombinations = new List<Flight>();

            // Separate outbound and inbound flights
            var outboundFlights = flights.Where(f => f.OutboundLegs.Count > 0).ToList();
            var inboundFlights = flights.Where(f => f.InboundLegs.Count > 0).ToList();

            // Match outbound and inbound flights by RecommendationId
            foreach (var outbound in outboundFlights)
            {
                foreach (var inbound in inboundFlights)
                {
                    // Check if both outbound and inbound flights share the same RecommendationId
                    if (outbound.RecommendationId == inbound.RecommendationId)
                    {
                        var totalTaxes = outbound.Taxes + inbound.Taxes; 

                        // Create a roundtrip combination for each matching pair
                        var roundtripFlight = new Flight
                        {
                            OutboundLegs = new List<FlightLeg>(outbound.OutboundLegs),
                            InboundLegs = new List<FlightLeg>(inbound.InboundLegs),
                            Price = outbound.Price, 
                            Taxes = totalTaxes, 
                            RecommendationId = outbound.RecommendationId
                        };

                        roundtripCombinations.Add(roundtripFlight); // Add to roundtrip combinations
                    } 
                }
            }

            return roundtripCombinations; // Return the list of roundtrip combinations
        }

        // Method to save flight data to a CSV file
        public static void SaveFlightsToCsv(List<Flight> roundtripFlights, string filePath)
        {
            // Define the output directory
            string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Output");

            // Create the Output directory if it does not exist
            Directory.CreateDirectory(outputDirectory);

            // Find the minimum price among the flights
            decimal minPrice = roundtripFlights.Min(f => f.Price + f.Taxes);

            // Construct the file path for the CSV file
            string csvFilePath = Path.Combine(outputDirectory, $"{filePath}).csv");

            // Create and write to a CSV file
            using (var writer = new StreamWriter(csvFilePath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteField("Cheapest");  
                csv.WriteField("Price");
                csv.WriteField("Taxes");

                // Write fields for outbound legs (up to 2)
                for (int i = 1; i <= 2; i++) 
                {
                    csv.WriteField($"outbound {i} airport departure");
                    csv.WriteField($"outbound {i} airport arrival");
                    csv.WriteField($"outbound {i} time departure");
                    csv.WriteField($"outbound {i} time arrival");
                    csv.WriteField($"outbound {i} flight number");
                }

                // Write fields for inbound legs (up to 2)
                for (int i = 1; i <= 2; i++) 
                {
                    csv.WriteField($"inbound {i} airport departure");
                    csv.WriteField($"inbound {i} airport arrival");
                    csv.WriteField($"inbound {i} time departure");
                    csv.WriteField($"inbound {i} time arrival");
                    csv.WriteField($"inbound {i} flight number");
                }

                csv.NextRecord();

                // Write each flight data
                foreach (var roundtrip in roundtripFlights)
                {
                    // Mark as true if it's the cheapest flight, else false
                    bool isCheapest = (roundtrip.Price + roundtrip.Taxes) == minPrice;
                    csv.WriteField(isCheapest);

                    csv.WriteField(roundtrip.Price);
                    csv.WriteField(roundtrip.Taxes);

                    // Write outbound flight details
                    for (int i = 0; i < 2; i++) 
                    {
                        if (i < roundtrip.OutboundLegs.Count)
                        {
                            var leg = roundtrip.OutboundLegs[i];
                            csv.WriteField(leg.Origin);
                            csv.WriteField(leg.Destination);
                            csv.WriteField(leg.DepartureTime.ToString("yyyy-MM-dd HH:mm"));
                            csv.WriteField(leg.ArrivalTime.ToString("yyyy-MM-dd HH:mm"));
                            csv.WriteField(leg.FlightNumber);
                        }
                        else
                        {
                            csv.WriteField(string.Empty); // Empty fields for missing legs
                        }
                    }

                    // Write inbound flight details
                    for (int i = 0; i < 2; i++) 
                    {
                        if (i < roundtrip.InboundLegs.Count)
                        {
                            var leg = roundtrip.InboundLegs[i];
                            csv.WriteField(leg.Origin);
                            csv.WriteField(leg.Destination);
                            csv.WriteField(leg.DepartureTime.ToString("yyyy-MM-dd HH:mm"));
                            csv.WriteField(leg.ArrivalTime.ToString("yyyy-MM-dd HH:mm"));
                            csv.WriteField(leg.FlightNumber);
                        }
                        else
                        {
                            csv.WriteField(string.Empty); // Empty fields for missing legs
                        }
                    }

                    csv.NextRecord(); // Move to the next row
                }
            }
        }
    }
}
