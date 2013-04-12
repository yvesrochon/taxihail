using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace apcurium.MK.Booking.Mobile.Infrastructure
{
    public interface ILocationService
    {
        void Initialize();

        bool IsServiceEnabled{get;}

        Task<Position> GetPositionAsync(int timeout, float accuracy, int fallbackTimeout, float fallbackAccuracy, CancellationToken cancelToken);

        Position LastKnownPosition {get;}

    }

    public class Position
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }

    }
}