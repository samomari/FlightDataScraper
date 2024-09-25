using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using CsvHelper;
using System.Globalization;
using System.Linq;

namespace FlightDataScraper
{
    class Program
    {

        static async Task Main(string[] args)
        {
            // List of acceptable outbound and inbound airports
            List<string> acceptableOutboundAirports = new List<string> { "MAD", "JFK", "CPH" };
            List<string> acceptableInboundAirports = new List<string> { "AUH", "FUE", "MAD" };

            // Get valid origin and destination airports from user input
            string origin = GetValidAirport("origin", acceptableOutboundAirports);
            string destination = GetValidAirport("destination", acceptableInboundAirports);

            // Ensure origin and destination are different
            if (origin == destination)
            {
                Console.WriteLine("Origin and destination cannot be the same. Please enter valid airports.");
                destination = GetValidAirport("destination", acceptableInboundAirports);
            }

            DateTime outboundDateTime;
            // Get valid outbound date
            string outboundDate = GetValidDate("outbound date", out outboundDateTime);
            // Get valid inbound date based on the outbound date
            string inboundDate = GetValidInboundDate(outboundDateTime);


            var flights = await FlightService.FetchFlightData(origin, destination, outboundDate, inboundDate);

            // Check if flights were fetched successfully
            if (flights == null || flights.Count == 0)
            {
                Console.WriteLine("No flights fetched. Please check your input and try again.");
                return;
            }

            Console.WriteLine("Flight data fetched successfully.");


            // Create roundtrip combinations
            var roundtripFlights = FlightService.CreateRoundtripCombinations(flights);

            // Handle roundtrip flight combinations
            HandleRoundtripCombinations(flights, origin, destination, outboundDate, inboundDate);
        }

        // Method to get valid airport input from the user
        public static string GetValidAirport(string airportType, List<string> acceptableAirports)
        {
            string airport = string.Empty;

            while (true)
            {
                Console.WriteLine($"Enter {airportType} airport, acceptable airports: {string.Join(", ", acceptableAirports)}.");
                airport = Console.ReadLine().ToUpper();

                if (IsValidAirport(airport, acceptableAirports))
                    break; // Valid input received

                Console.WriteLine($"Invalid {airportType} airport: {airport}. Acceptable {airportType} airports are: {string.Join(", ", acceptableAirports)}.");
            }

            return airport; // Return valid airport code
        }

        // Method to validate if the airport is in the list of acceptable airports
        public static bool IsValidAirport(string airport, List<string> acceptableAirports)
        {
            return acceptableAirports.Contains(airport);
        }

        // General date validation method
        public static string GetValidDate(string dateType, out DateTime validDate, DateTime? referenceDate = null, int minimumDaysApart = 0)
        {
            string date = string.Empty;

            while (true)
            {
                Console.WriteLine($"Enter {dateType} (yyyy-mm-dd):");
                date = Console.ReadLine();

                // Attempt to parse the date input
                if (DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out validDate))
                {
                    if (validDate.Date < DateTime.Today)
                    {
                        Console.WriteLine($"{dateType} cannot be in the past. Please enter a valid date.");
                        continue; // Re-prompt for valid date
                    }

                    // Check if there's a reference date and if validDate meets the minimum days apart requirement
                    if (referenceDate.HasValue && validDate <= referenceDate.Value.AddDays(minimumDaysApart))
                    {
                        Console.WriteLine($"{dateType} must be at least {minimumDaysApart} days after {referenceDate.Value.ToShortDateString()}.");
                        continue; // Re-prompt for valid date
                    }
                    break; // Valid date received
                }

                Console.WriteLine($"Invalid {dateType} format. Please use yyyy-mm-dd.");
            }

            return date; // Return valid date string
        }

        // Validate inbound date using the outbound date as a reference
        public static string GetValidInboundDate(DateTime outboundDateTime)
        {
            DateTime inboundDateTime;
            // Call GetValidDate with outboundDateTime as reference and 2 days minimum
            return GetValidDate("inbound date", out inboundDateTime, outboundDateTime, 2);
        }

        // Method to display flight legs
        static void DisplayFlightLegs(List<FlightLeg> legs, string label)
        {
            foreach (var leg in legs)
            {
                Console.WriteLine($"{label}: {leg.FlightNumber}, From: {leg.Origin} To: {leg.Destination}, Departure: {leg.DepartureTime}, Arrival: {leg.ArrivalTime}");
            }
        }

        // Method to handle roundtrip combinations
        static void HandleRoundtripCombinations(List<Flight> flights, string origin, string destination, string outboundDate, string inboundDate)
        {
            var roundtripFlights = FlightService.CreateRoundtripCombinations(flights);

            if (roundtripFlights.Count == 0)
            {
                Console.WriteLine("No roundtrip combinations found.");
                return; // Exit if no roundtrip flights are available
            }

            decimal minPrice = roundtripFlights.Min(f => f.Price + f.Taxes);
            var cheapestFlights = roundtripFlights.Where(f => f.Price + f.Taxes == minPrice).ToList();

            Console.WriteLine("\nAll Roundtrip Flights:");
            DisplayFlights(roundtripFlights);

            Console.WriteLine("\nCheapest Flight Options:");
            DisplayFlights(cheapestFlights);

            SaveFlightsToCsv(roundtripFlights, origin, destination, outboundDate, inboundDate);
        }

        // Method to display flights
        static void DisplayFlights(List<Flight> flights)
        {
            foreach (var flight in flights)
            {
                Console.WriteLine("\n");

                string allFlightNumbers = string.Join("-", flight.OutboundLegs.Select(l => l.FlightNumber).Concat(flight.InboundLegs.Select(l => l.FlightNumber)));
                Console.WriteLine($"Flights: {allFlightNumbers}");

                DisplayFlightLegs(flight.OutboundLegs, "Outbound Flight");
                DisplayFlightLegs(flight.InboundLegs, "Inbound Flight");

                decimal totalPriceWithTaxes = flight.Price + flight.Taxes;
                Console.WriteLine($"Price: {flight.Price}, Taxes: {flight.Taxes}, Total Price (with Taxes): {totalPriceWithTaxes}");
            }
        }

        // Save flights to CSV
        static void SaveFlightsToCsv(List<Flight> flights, string origin, string destination, string outboundDate, string inboundDate)
        {
            if (flights.Count > 0)
            {
                string csvFilePath = $"{origin}-{destination}_({outboundDate})-({inboundDate}).csv";
                FlightService.SaveFlightsToCsv(flights, csvFilePath);
                Console.WriteLine($"\nFlights data saved to {csvFilePath}");
            }
            else
            {
                Console.WriteLine("No roundtrip flight combinations available to save.");
            }
        }

    }
}
