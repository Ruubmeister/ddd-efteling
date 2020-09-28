﻿using DddEfteling.Shared.Entities;
using Geolocation;
using System;
using System.Collections.Generic;
using System.Text;

namespace DddEfteling.Shared.Boundary
{
    public class FairyTaleDto: ILocationDto
    {
        public Guid Guid { get; set; }

        public string Name { get; set; }
        
        public Coordinate Coordinates { get; set; }

        public LocationType LocationType { get; set; }

        public FairyTaleDto(Guid guid, string name, Coordinate coordinate, LocationType locationType) {
            this.Guid = guid;
            this.Name = name;
            this.Coordinates = coordinate;
            this.LocationType = locationType;
        }
    }
}
