﻿using AutoMapper;

namespace apcurium.MK.Booking.IBS
{
    public class IBSAutoMapperProfile : Profile
    {
        protected override void Configure()
        {
            this.CreateMap<TVehiclePosition, IBSVehiclePosition>()
                .ForMember(x => x.VehicleNumber, opt => opt.MapFrom(x => x.VehicleNumber.Trim()))
                .ForMember(x => x.PositionDate, opt => opt
                    .MapFrom(x => x.GPSLastUpdated.ToDateTime().GetValueOrDefault()));
        }
    }
}
