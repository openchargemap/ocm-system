using DotSpatial;
using DotSpatial.Data;
using DotSpatial.Projections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.Import.Analysis
{
    public class PointClassification
    {
        public string CountryCode { get; set; }

        public string CountryName { get; set; }

        public string CountrySubdivision { get; set; }
    }

    public class CountryCodeMapping
    {
        public string ISOCode2 { get; set; }

        public string ISOCode3 { get; set; }

        public int NumericCode { get; set; }
    }

    public class SpatialAnalysis : IDisposable
    {
        private IFeatureSet fsWorldCountries;
        private List<CountryCodeMapping> countryCodes;

        public SpatialAnalysis(string globalShapeFileDataPath)
        {
            string dataPath = globalShapeFileDataPath;
            fsWorldCountries = FeatureSet.Open(dataPath);
            fsWorldCountries.Reproject(DotSpatial.Projections.KnownCoordinateSystems.Geographic.World.WGS1984);
            countryCodes = GetCountryCodeMappings();
        }

        public List<CountryCodeMapping> GetCountryCodeMappings()
        {
            //TODO: read from data source instead
            //http://en.wikipedia.org/wiki/ISO_3166-1
            var countryCodes = new List<CountryCodeMapping>();
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "AF", ISOCode3 = "AFG", NumericCode = 4 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "AX", ISOCode3 = "ALA", NumericCode = 248 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "AL", ISOCode3 = "ALB", NumericCode = 8 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "DZ", ISOCode3 = "DZA", NumericCode = 12 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "AS", ISOCode3 = "ASM", NumericCode = 16 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "AD", ISOCode3 = "AND", NumericCode = 20 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "AO", ISOCode3 = "AGO", NumericCode = 24 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "AI", ISOCode3 = "AIA", NumericCode = 660 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "AQ", ISOCode3 = "ATA", NumericCode = 10 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "AG", ISOCode3 = "ATG", NumericCode = 28 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "AR", ISOCode3 = "ARG", NumericCode = 32 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "AM", ISOCode3 = "ARM", NumericCode = 51 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "AW", ISOCode3 = "ABW", NumericCode = 533 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "AU", ISOCode3 = "AUS", NumericCode = 36 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "AT", ISOCode3 = "AUT", NumericCode = 40 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "AZ", ISOCode3 = "AZE", NumericCode = 31 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BS", ISOCode3 = "BHS", NumericCode = 44 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BH", ISOCode3 = "BHR", NumericCode = 48 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BD", ISOCode3 = "BGD", NumericCode = 50 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BB", ISOCode3 = "BRB", NumericCode = 52 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BY", ISOCode3 = "BLR", NumericCode = 112 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BE", ISOCode3 = "BEL", NumericCode = 56 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BZ", ISOCode3 = "BLZ", NumericCode = 84 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BJ", ISOCode3 = "BEN", NumericCode = 204 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BM", ISOCode3 = "BMU", NumericCode = 60 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BT", ISOCode3 = "BTN", NumericCode = 64 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BO", ISOCode3 = "BOL", NumericCode = 68 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BQ", ISOCode3 = "BES", NumericCode = 535 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BA", ISOCode3 = "BIH", NumericCode = 70 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BW", ISOCode3 = "BWA", NumericCode = 72 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BV", ISOCode3 = "BVT", NumericCode = 74 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BR", ISOCode3 = "BRA", NumericCode = 76 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "IO", ISOCode3 = "IOT", NumericCode = 86 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BN", ISOCode3 = "BRN", NumericCode = 96 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BG", ISOCode3 = "BGR", NumericCode = 100 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BF", ISOCode3 = "BFA", NumericCode = 854 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BI", ISOCode3 = "BDI", NumericCode = 108 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "KH", ISOCode3 = "KHM", NumericCode = 116 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "CM", ISOCode3 = "CMR", NumericCode = 120 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "CA", ISOCode3 = "CAN", NumericCode = 124 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "CV", ISOCode3 = "CPV", NumericCode = 132 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "KY", ISOCode3 = "CYM", NumericCode = 136 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "CF", ISOCode3 = "CAF", NumericCode = 140 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "TD", ISOCode3 = "TCD", NumericCode = 148 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "CL", ISOCode3 = "CHL", NumericCode = 152 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "CN", ISOCode3 = "CHN", NumericCode = 156 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "CX", ISOCode3 = "CXR", NumericCode = 162 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "CC", ISOCode3 = "CCK", NumericCode = 166 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "CO", ISOCode3 = "COL", NumericCode = 170 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "KM", ISOCode3 = "COM", NumericCode = 174 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "CG", ISOCode3 = "COG", NumericCode = 178 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "CD", ISOCode3 = "COD", NumericCode = 180 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "CK", ISOCode3 = "COK", NumericCode = 184 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "CR", ISOCode3 = "CRI", NumericCode = 188 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "CI", ISOCode3 = "CIV", NumericCode = 384 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "HR", ISOCode3 = "HRV", NumericCode = 191 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "CU", ISOCode3 = "CUB", NumericCode = 192 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "CW", ISOCode3 = "CUW", NumericCode = 531 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "CY", ISOCode3 = "CYP", NumericCode = 196 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "CZ", ISOCode3 = "CZE", NumericCode = 203 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "DK", ISOCode3 = "DNK", NumericCode = 208 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "DJ", ISOCode3 = "DJI", NumericCode = 262 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "DM", ISOCode3 = "DMA", NumericCode = 212 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "DO", ISOCode3 = "DOM", NumericCode = 214 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "EC", ISOCode3 = "ECU", NumericCode = 218 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "EG", ISOCode3 = "EGY", NumericCode = 818 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SV", ISOCode3 = "SLV", NumericCode = 222 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "GQ", ISOCode3 = "GNQ", NumericCode = 226 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "ER", ISOCode3 = "ERI", NumericCode = 232 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "EE", ISOCode3 = "EST", NumericCode = 233 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "ET", ISOCode3 = "ETH", NumericCode = 231 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "FK", ISOCode3 = "FLK", NumericCode = 238 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "FO", ISOCode3 = "FRO", NumericCode = 234 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "FJ", ISOCode3 = "FJI", NumericCode = 242 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "FI", ISOCode3 = "FIN", NumericCode = 246 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "FR", ISOCode3 = "FRA", NumericCode = 250 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "GF", ISOCode3 = "GUF", NumericCode = 254 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "PF", ISOCode3 = "PYF", NumericCode = 258 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "TF", ISOCode3 = "ATF", NumericCode = 260 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "GA", ISOCode3 = "GAB", NumericCode = 266 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "GM", ISOCode3 = "GMB", NumericCode = 270 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "GE", ISOCode3 = "GEO", NumericCode = 268 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "DE", ISOCode3 = "DEU", NumericCode = 276 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "GH", ISOCode3 = "GHA", NumericCode = 288 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "GI", ISOCode3 = "GIB", NumericCode = 292 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "GR", ISOCode3 = "GRC", NumericCode = 300 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "GL", ISOCode3 = "GRL", NumericCode = 304 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "GD", ISOCode3 = "GRD", NumericCode = 308 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "GP", ISOCode3 = "GLP", NumericCode = 312 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "GU", ISOCode3 = "GUM", NumericCode = 316 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "GT", ISOCode3 = "GTM", NumericCode = 320 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "GG", ISOCode3 = "GGY", NumericCode = 831 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "GN", ISOCode3 = "GIN", NumericCode = 324 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "GW", ISOCode3 = "GNB", NumericCode = 624 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "GY", ISOCode3 = "GUY", NumericCode = 328 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "HT", ISOCode3 = "HTI", NumericCode = 332 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "HM", ISOCode3 = "HMD", NumericCode = 334 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "VA", ISOCode3 = "VAT", NumericCode = 336 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "HN", ISOCode3 = "HND", NumericCode = 340 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "HK", ISOCode3 = "HKG", NumericCode = 344 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "HU", ISOCode3 = "HUN", NumericCode = 348 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "IS", ISOCode3 = "ISL", NumericCode = 352 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "IN", ISOCode3 = "IND", NumericCode = 356 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "ID", ISOCode3 = "IDN", NumericCode = 360 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "IR", ISOCode3 = "IRN", NumericCode = 364 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "IQ", ISOCode3 = "IRQ", NumericCode = 368 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "IE", ISOCode3 = "IRL", NumericCode = 372 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "IM", ISOCode3 = "IMN", NumericCode = 833 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "IL", ISOCode3 = "ISR", NumericCode = 376 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "IT", ISOCode3 = "ITA", NumericCode = 380 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "JM", ISOCode3 = "JAM", NumericCode = 388 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "JP", ISOCode3 = "JPN", NumericCode = 392 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "JE", ISOCode3 = "JEY", NumericCode = 832 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "JO", ISOCode3 = "JOR", NumericCode = 400 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "KZ", ISOCode3 = "KAZ", NumericCode = 398 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "KE", ISOCode3 = "KEN", NumericCode = 404 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "KI", ISOCode3 = "KIR", NumericCode = 296 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "KP", ISOCode3 = "PRK", NumericCode = 408 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "KR", ISOCode3 = "KOR", NumericCode = 410 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "KW", ISOCode3 = "KWT", NumericCode = 414 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "KG", ISOCode3 = "KGZ", NumericCode = 417 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "LA", ISOCode3 = "LAO", NumericCode = 418 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "LV", ISOCode3 = "LVA", NumericCode = 428 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "LB", ISOCode3 = "LBN", NumericCode = 422 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "LS", ISOCode3 = "LSO", NumericCode = 426 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "LR", ISOCode3 = "LBR", NumericCode = 430 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "LY", ISOCode3 = "LBY", NumericCode = 434 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "LI", ISOCode3 = "LIE", NumericCode = 438 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "LT", ISOCode3 = "LTU", NumericCode = 440 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "LU", ISOCode3 = "LUX", NumericCode = 442 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MO", ISOCode3 = "MAC", NumericCode = 446 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MK", ISOCode3 = "MKD", NumericCode = 807 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MG", ISOCode3 = "MDG", NumericCode = 450 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MW", ISOCode3 = "MWI", NumericCode = 454 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MY", ISOCode3 = "MYS", NumericCode = 458 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MV", ISOCode3 = "MDV", NumericCode = 462 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "ML", ISOCode3 = "MLI", NumericCode = 466 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MT", ISOCode3 = "MLT", NumericCode = 470 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MH", ISOCode3 = "MHL", NumericCode = 584 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MQ", ISOCode3 = "MTQ", NumericCode = 474 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MR", ISOCode3 = "MRT", NumericCode = 478 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MU", ISOCode3 = "MUS", NumericCode = 480 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "YT", ISOCode3 = "MYT", NumericCode = 175 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MX", ISOCode3 = "MEX", NumericCode = 484 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "FM", ISOCode3 = "FSM", NumericCode = 583 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MD", ISOCode3 = "MDA", NumericCode = 498 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MC", ISOCode3 = "MCO", NumericCode = 492 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MN", ISOCode3 = "MNG", NumericCode = 496 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "ME", ISOCode3 = "MNE", NumericCode = 499 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MS", ISOCode3 = "MSR", NumericCode = 500 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MA", ISOCode3 = "MAR", NumericCode = 504 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MZ", ISOCode3 = "MOZ", NumericCode = 508 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MM", ISOCode3 = "MMR", NumericCode = 104 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "NA", ISOCode3 = "NAM", NumericCode = 516 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "NR", ISOCode3 = "NRU", NumericCode = 520 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "NP", ISOCode3 = "NPL", NumericCode = 524 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "NL", ISOCode3 = "NLD", NumericCode = 528 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "NC", ISOCode3 = "NCL", NumericCode = 540 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "NZ", ISOCode3 = "NZL", NumericCode = 554 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "NI", ISOCode3 = "NIC", NumericCode = 558 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "NE", ISOCode3 = "NER", NumericCode = 562 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "NG", ISOCode3 = "NGA", NumericCode = 566 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "NU", ISOCode3 = "NIU", NumericCode = 570 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "NF", ISOCode3 = "NFK", NumericCode = 574 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MP", ISOCode3 = "MNP", NumericCode = 580 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "NO", ISOCode3 = "NOR", NumericCode = 578 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "OM", ISOCode3 = "OMN", NumericCode = 512 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "PK", ISOCode3 = "PAK", NumericCode = 586 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "PW", ISOCode3 = "PLW", NumericCode = 585 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "PS", ISOCode3 = "PSE", NumericCode = 275 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "PA", ISOCode3 = "PAN", NumericCode = 591 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "PG", ISOCode3 = "PNG", NumericCode = 598 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "PY", ISOCode3 = "PRY", NumericCode = 600 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "PE", ISOCode3 = "PER", NumericCode = 604 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "PH", ISOCode3 = "PHL", NumericCode = 608 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "PN", ISOCode3 = "PCN", NumericCode = 612 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "PL", ISOCode3 = "POL", NumericCode = 616 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "PT", ISOCode3 = "PRT", NumericCode = 620 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "PR", ISOCode3 = "PRI", NumericCode = 630 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "QA", ISOCode3 = "QAT", NumericCode = 634 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "RE", ISOCode3 = "REU", NumericCode = 638 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "RO", ISOCode3 = "ROU", NumericCode = 642 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "RU", ISOCode3 = "RUS", NumericCode = 643 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "RW", ISOCode3 = "RWA", NumericCode = 646 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "BL", ISOCode3 = "BLM", NumericCode = 652 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SH", ISOCode3 = "SHN", NumericCode = 654 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "KN", ISOCode3 = "KNA", NumericCode = 659 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "LC", ISOCode3 = "LCA", NumericCode = 662 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "MF", ISOCode3 = "MAF", NumericCode = 663 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "PM", ISOCode3 = "SPM", NumericCode = 666 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "VC", ISOCode3 = "VCT", NumericCode = 670 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "WS", ISOCode3 = "WSM", NumericCode = 882 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SM", ISOCode3 = "SMR", NumericCode = 674 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "ST", ISOCode3 = "STP", NumericCode = 678 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SA", ISOCode3 = "SAU", NumericCode = 682 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SN", ISOCode3 = "SEN", NumericCode = 686 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "RS", ISOCode3 = "SRB", NumericCode = 688 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SC", ISOCode3 = "SYC", NumericCode = 690 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SL", ISOCode3 = "SLE", NumericCode = 694 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SG", ISOCode3 = "SGP", NumericCode = 702 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SX", ISOCode3 = "SXM", NumericCode = 534 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SK", ISOCode3 = "SVK", NumericCode = 703 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SI", ISOCode3 = "SVN", NumericCode = 705 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SB", ISOCode3 = "SLB", NumericCode = 90 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SO", ISOCode3 = "SOM", NumericCode = 706 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "ZA", ISOCode3 = "ZAF", NumericCode = 710 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "GS", ISOCode3 = "SGS", NumericCode = 239 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SS", ISOCode3 = "SSD", NumericCode = 728 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "ES", ISOCode3 = "ESP", NumericCode = 724 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "LK", ISOCode3 = "LKA", NumericCode = 144 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SD", ISOCode3 = "SDN", NumericCode = 729 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SR", ISOCode3 = "SUR", NumericCode = 740 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SJ", ISOCode3 = "SJM", NumericCode = 744 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SZ", ISOCode3 = "SWZ", NumericCode = 748 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SE", ISOCode3 = "SWE", NumericCode = 752 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "CH", ISOCode3 = "CHE", NumericCode = 756 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "SY", ISOCode3 = "SYR", NumericCode = 760 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "TW", ISOCode3 = "TWN", NumericCode = 158 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "TJ", ISOCode3 = "TJK", NumericCode = 762 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "TZ", ISOCode3 = "TZA", NumericCode = 834 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "TH", ISOCode3 = "THA", NumericCode = 764 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "TL", ISOCode3 = "TLS", NumericCode = 626 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "TG", ISOCode3 = "TGO", NumericCode = 768 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "TK", ISOCode3 = "TKL", NumericCode = 772 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "TO", ISOCode3 = "TON", NumericCode = 776 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "TT", ISOCode3 = "TTO", NumericCode = 780 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "TN", ISOCode3 = "TUN", NumericCode = 788 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "TR", ISOCode3 = "TUR", NumericCode = 792 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "TM", ISOCode3 = "TKM", NumericCode = 795 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "TC", ISOCode3 = "TCA", NumericCode = 796 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "TV", ISOCode3 = "TUV", NumericCode = 798 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "UG", ISOCode3 = "UGA", NumericCode = 800 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "UA", ISOCode3 = "UKR", NumericCode = 804 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "AE", ISOCode3 = "ARE", NumericCode = 784 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "GB", ISOCode3 = "GBR", NumericCode = 826 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "US", ISOCode3 = "USA", NumericCode = 840 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "UM", ISOCode3 = "UMI", NumericCode = 581 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "UY", ISOCode3 = "URY", NumericCode = 858 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "UZ", ISOCode3 = "UZB", NumericCode = 860 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "VU", ISOCode3 = "VUT", NumericCode = 548 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "VE", ISOCode3 = "VEN", NumericCode = 862 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "VN", ISOCode3 = "VNM", NumericCode = 704 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "VG", ISOCode3 = "VGB", NumericCode = 92 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "VI", ISOCode3 = "VIR", NumericCode = 850 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "WF", ISOCode3 = "WLF", NumericCode = 876 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "EH", ISOCode3 = "ESH", NumericCode = 732 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "YE", ISOCode3 = "YEM", NumericCode = 887 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "ZM", ISOCode3 = "ZMB", NumericCode = 894 });
            countryCodes.Add(new CountryCodeMapping { ISOCode2 = "ZW", ISOCode3 = "ZWE", NumericCode = 716 });

            return countryCodes;
        }

        public string ConvertISOCountryCode(string code)
        {
            if (code.Length == 2)
            {
                var codeMapping = countryCodes.FirstOrDefault(c => c.ISOCode2.ToLower() == code.ToLower().Trim());
                if (codeMapping != null) return codeMapping.ISOCode3;
            }

            if (code.Length == 3)
            {
                var codeMapping = countryCodes.FirstOrDefault(c => c.ISOCode3.ToLower() == code.ToLower().Trim());
                if (codeMapping != null) return codeMapping.ISOCode2;
            }

            System.Diagnostics.Debug.WriteLine("Could not convert ISOcode " + code);
            return null;
        }

        public PointClassification ClassifyPoint(double latitude, double longitude)
        {
            FeatureSet pFeatureSet = new FeatureSet();
            pFeatureSet.Projection = KnownCoordinateSystems.Geographic.World.WGS1984;

            DotSpatial.Topology.Point pPoint = new DotSpatial.Topology.Point(longitude, latitude);
            FeatureSet pPointFeatureSet = new FeatureSet(DotSpatial.Topology.FeatureType.Point);
            pPointFeatureSet.Projection = KnownCoordinateSystems.Geographic.World.WGS1984;
            pPointFeatureSet.AddFeature(pPoint);

            Extent pAffectedExtent = null;
            var result = fsWorldCountries.Select(pPointFeatureSet.Extent, out pAffectedExtent);

            foreach (IFeature feature in result)
            {
                PointClassification classification = new PointClassification();
                classification.CountryCode = feature.DataRow["ADM0_A3"].ToString();
                if (classification.CountryCode.Length == 3) classification.CountryCode = ConvertISOCountryCode(classification.CountryCode);
                classification.CountrySubdivision = feature.DataRow["NAME"].ToString();
                classification.CountryName = feature.DataRow["ADMIN"].ToString();
                return classification;
            }

            return null;
            // System.Diagnostics.Debug.WriteLine(featureL);
        }

        public void Dispose()
        {
            if (!fsWorldCountries.IsDisposed)
            {
                fsWorldCountries.Close();
                fsWorldCountries.Dispose();
            }
        }
    }
}