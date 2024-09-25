using System;
using System.Collections.Generic;

namespace FlightDataScraper
{
    // Represents API response.
    public class ApiResponse
    {
        public Body Body { get; set; }
    }

    // Represents the body of the API response, containing flight data.
    public class Body
    {
        public Data Data { get; set; }
    }

    // Represents the main flight data, including journeys and availabilities info.
    public class Data
    {
        public List<Journey> Journeys { get; set; }
        public List<TotalAvailability> TotalAvailabilities { get; set; }
    }

    // Represents total availability, including price.
    public class TotalAvailability
    {
        public int RecommendationId { get; set; }
        public decimal Total { get; set; }
    }

    // Represents a flight journey and its connections.
    public class Journey
    {
        public int RecommendationId { get; set; }
        public string Direction { get; set; }
        public List<FlightInfo> Flights { get; set; }
        public decimal ImportTaxAdl { get; set; }
        // Number of flight connections (derived from the number of flights).
        public int NumberOfConnections => Flights.Count - 1;
    }

    // Represents individual flight information.
    public class FlightInfo
    {
        public string companyCode { get; set; } 
        public string number { get; set; }
        public AirportInfo AirportDeparture { get; set; }
        public AirportInfo AirportArrival { get; set; }
        public string DateDeparture { get; set; }
        public string DateArrival { get; set; }
    }

    // Represents airport information.
    public class AirportInfo
    {
        public string Code { get; set; }
    }

}
