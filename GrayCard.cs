using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrayCard581
{
    public class GrayCard
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public int VehicleId { get; set; }
        public DateTime Date { get; set; }

        public bool IsExpired()
        {
            return Date.AddDays(30) < DateTime.Now;
        }
    }
}
