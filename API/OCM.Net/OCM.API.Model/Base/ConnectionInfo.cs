using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OCM.API.Common.Model
{
    public class ConnectionInfo
    {
        public int ID { get; set; }


        public int? ConnectionTypeID { get; set; }
        [DisplayName("Connection Type")]
        public ConnectionType ConnectionType { get; set; }

        [DisplayName("Operators Own Ref"), StringLength(100)]
        public string Reference { get; set; }

        public int? StatusTypeID { get; set; }
        [DisplayName("Operational Status")]
        public StatusType StatusType { get; set; }

        public int? LevelID { get; set; }
        [DisplayName("Charging Level")]
        public ChargerType Level { get; set; }

        [DisplayName("Max. Current (Amps)"), Range(0, 1000)]
        public int? Amps { get; set; }

        [DisplayName("Max. Voltage")]
        public int? Voltage { get; set; }

        [DisplayName("Max. Power (kW)"), Range(0, 1000)]
        public double? PowerKW { get; set; }

        public int? CurrentTypeID { get; set; }
        [DisplayName("Supply Type")]
        public CurrentType CurrentType { get; set; }

        [DisplayName("Quantity Available"), Range(0, 100)]
        public int? Quantity { get; set; }

        [DisplayName("Additional Comments")]
        public string Comments { get; set; }

        public override string ToString()
        {
            string output = "";

            if (this.ConnectionType != null) output += "Connection Type: " + ConnectionType.Title + ", ";
            if (this.Amps != null) output += Amps + "A Max. Current, ";
            if (this.Voltage != null) output += Voltage + "V, ";
            if (this.PowerKW != null) output += PowerKW + "kW, ";
            if (this.CurrentType != null) output += this.CurrentType.Title + ", ";
            if (this.Quantity != null) output += this.Quantity + " Present, ";
            if (this.StatusType != null) output += "Status: " + this.StatusType.Title + ", ";
            if (this.Comments != null) output += "Comment: " + this.Comments + ", ";

            if (output.EndsWith(", ")) output = output.Substring(0, output.Length - 2);
            return output;
        }
    }
}