﻿using DddEfteling.Shared.Entities;
using DddEfteling.Rides.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DddEfteling.Shared.Boundary;
using DddEfteling.Rides.Boundary;

namespace DddEfteling.Rides.Controls
{
    public class RideControl : IRideControl
    {
        private readonly ConcurrentBag<Ride> rides;
        private readonly ILogger logger;
        private readonly IEventProducer eventProducer;
        private readonly IEmployeeClient employeeClient;
        private readonly IVisitorClient visitorClient;

        public RideControl() { }

        public RideControl(ILogger<RideControl> logger, IEventProducer eventProducer, IEmployeeClient employeeClient, IVisitorClient visitorClient)
        {
            this.rides = LoadRides();
            this.logger = logger;
            this.eventProducer = eventProducer;
            this.employeeClient = employeeClient;
            this.visitorClient = visitorClient;

            this.logger.LogInformation($"Loaded ride count: {rides.Count}");
            this.CalculateRideDistances();
        }

        private ConcurrentBag<Ride> LoadRides()
        {
            using StreamReader r = new StreamReader("resources/rides.json");
            string json = r.ReadToEnd();
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(new RideConverter());
            return new ConcurrentBag<Ride>(JsonConvert.DeserializeObject<List<Ride>>(json, settings));
        }

        public void ToMaintenance(Ride ride)
        {
            ride.ToMaintenance();
            logger.LogInformation("Ride in maintenance");
        }

        public Ride FindRideByName(string name)
        {
            return rides.FirstOrDefault(ride => ride.Name.Equals(name));
        }

        public List<Ride> All()
        {
            return rides.ToList();
        }

        public void OpenRides()
        {
            foreach (Ride ride in rides.Where(ride => ride.Status.Equals(RideStatus.Closed)))
            {
                ride.ToOpen();
                logger.LogInformation($"Ride {ride.Name} opened");
                this.CheckRequiredEmployees(ride);
            }
        }

        public void StartService()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    foreach (Ride ride in rides)
                    {

                        if (ride.EndTime > DateTime.Now)
                        {
                            continue;
                        }

                        List<VisitorDto> unboardedVisitors = ride.UnboardVisitors();

                        if (ride.Status.Equals(RideStatus.Open))
                        {
                            ride.Start();
                        }

                        if (unboardedVisitors.Count > 0)
                        {
                            var payload = new Dictionary<string, string>
                            {
                                { "visitors", unboardedVisitors.ConvertAll(visitor => visitor.Guid).ToString() }
                            };

                            Event unboardedEvent = new Event(EventType.VisitorsUnboarded, EventSource.Ride, payload);
                            eventProducer.Produce(unboardedEvent);
                        }

                        Task.Delay(100).Wait();
                    }
                }
            });
        }

        public async void CloseRides()
        {
            await Task.Run(() =>
            {
                foreach (Ride ride in rides.Where(ride => ride.Status.Equals(RideStatus.Open)))
                {
                    logger.LogInformation($"Ride {ride.Name} closed");
                    ride.ToClosed();
                    // Send employees home
                }
            });
        }

        public Ride NearestRide(Guid rideGuid, List<Guid> exclusionList)
        {
            Ride ride = this.rides.Where(ride => ride.Guid.Equals(rideGuid)).First();
            Guid nextRide = ride.DistanceToOthers.Where(keyVal => !exclusionList.Contains(keyVal.Value))
                .First().Value;

            return this.rides.Where(tale => tale.Guid.Equals(nextRide)).First();
        }

        private void CheckRequiredEmployees(Ride ride)
        {
            // Todo: Fix this
        }

        private void CalculateRideDistances()
        {
            foreach (Ride ride in rides)
            {
                foreach (Ride toRide in rides)
                {
                    if (ride.Equals(toRide))
                    {
                        continue;
                    }

                    ride.AddDistanceToOthers(ride.GetDistanceTo(toRide), toRide.Guid);
                    logger.LogDebug($"Calculated distance from {ride.Name} to {toRide.Name}");
                }
            }
            logger.LogDebug($"Ride distances calculated");
        }

        public Ride GetRandom()
        {
            return this.rides.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
        }

        public void HandleEmployeeChangedWorkplace(WorkplaceDto workplace, Guid employee, WorkplaceSkill skill)
        {
            if(this.rides.Where(ride => ride.Guid.Equals(workplace.Guid)).Any())
            {
                Ride ride = rides.Where(ride => ride.Guid.Equals(workplace.Guid)).First();

                ride.AddEmployee(employee, skill);
            }
        }

        public void HandleVisitorSteppingInRideLine(Guid visitorGuid, Guid rideGuid)
        {
            Ride ride = rides.Where(ride => ride.Guid.Equals(rideGuid)).First();
            if (ride.Status.Equals(RideStatus.Open))
            {
                VisitorDto visitor = visitorClient.GetVisitor(visitorGuid).Result;
                ride.AddVisitorToLine(visitor);
            }
            else
            {
                var payload = new Dictionary<string, string>
                            {
                                { "visitors", visitorGuid.ToString() }
                            };

                Event unboardedEvent = new Event(EventType.VisitorsUnboarded, EventSource.Ride, payload);
                eventProducer.Produce(unboardedEvent);
            }

        }
    }

    public interface IRideControl
    {
        public Ride FindRideByName(string name);

        public List<Ride> All();

        public void OpenRides();

        public void CloseRides();

        public Ride GetRandom();

        public void StartService();

        public Ride NearestRide(Guid rideGuid, List<Guid> exclusionList);
        public void HandleVisitorSteppingInRideLine(Guid visitorGuid, Guid rideGuid);

        public void HandleEmployeeChangedWorkplace(WorkplaceDto workplace, Guid employee, WorkplaceSkill skill);
    }
}
