using System;

namespace FlightDataScraper
{
    // Represents a single leg of a flight, containing details such as origin, destination, 
    // departure and arrival times, and the flight number.
    public class FlightLeg
    {
        public string Origin { get; set; }
        public string Destination { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string FlightNumber { get; set; }
    }

    // Represents a complete flight with outbound and inbound legs, along with pricing and a recommendation ID.
    public class Flight
    {
        public List<FlightLeg> OutboundLegs { get; set; } = new List<FlightLeg>();
        public List<FlightLeg> InboundLegs { get; set; } = new List<FlightLeg>();
        public decimal Price { get; set; }  
        public decimal Taxes { get; set; } 
        public int RecommendationId { get; set; }  
    }

}
