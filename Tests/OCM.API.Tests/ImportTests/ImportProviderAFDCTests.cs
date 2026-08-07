using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OCM.API.Common.Model;
using OCM.Import.Providers;
using Xunit;

namespace OCM.API.Tests.ImportTests
{
    public class ImportProviderAfdcTests
    {
        [Fact]
        public void Constructor_SetsExpectedDefaults()
        {
            var provider = new ImportProvider_AFDC("test-api-key");

            Assert.Equal("afdc.energy.gov", provider.GetProviderName());
            Assert.Equal("afdc", provider.OutputNamePrefix);
            Assert.Equal(2, provider.GetProviderID());
            Assert.True(provider.IsAutoRefreshed);
            Assert.True(provider.IsProductionReady);
            Assert.Contains("api_key=test-api-key", provider.AutoRefreshURL);
        }
        
        [Fact]
        public void Process_MapsOperationalPublicCanadianStationWithLevel2Connections()
        {
            // Given
            var provider = new ImportProvider_AFDC("test-api-key");
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            provider.InputData = File.ReadAllText(Path.Combine(path, "Assets", "ImportProviderAFDCTests-InputData-Level2.json"));

            // When
            var results = provider.Process(CreateCoreReferenceData());

            // Then
            var station = Assert.Single(results);
            Assert.Equal("384109", station.DataProvidersReference);
            Assert.Equal(2, station.DataProviderID);
            Assert.Null(station.OperatorID); // FIXME: Should be 90
            Assert.Equal((int)StandardStatusTypes.Operational, station.StatusTypeID);
            Assert.Equal(1, station.UsageTypeID); // public
            Assert.Equal(44, station.AddressInfo.CountryID); // CA
            Assert.Equal("Québec - Piscine Lebourgneuf", station.AddressInfo.Title);
            Assert.Equal("1640, Boulevard la Morille", station.AddressInfo.AddressLine1);
            Assert.All(station.Connections, c => Assert.Equal(2, c.LevelID));
            Assert.All(station.Connections, c => Assert.Equal(3.7, c.PowerKW)); // FIXME: Should be 6.2
            Assert.Equal(1, station.Connections.Count(c => c.ConnectionTypeID == (int)StandardConnectionTypes.J1772)); // FIXME: Should be 4
            Assert.Equal(100, station.SubmissionStatus.ID);
        }

        [Fact]
        public void Process_MapsOperationalPublicCanadianStationWithDCFastConnections()
        {
            // Given
            var provider = new ImportProvider_AFDC("test-api-key");
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            provider.InputData = File.ReadAllText(Path.Combine(path, "Assets", "ImportProviderAFDCTests-InputData-Level3.json"));

            // When
            var results = provider.Process(CreateCoreReferenceData());

            // Then
            var station = Assert.Single(results);
            Assert.Equal("434008", station.DataProvidersReference);
            Assert.Equal(2, station.DataProviderID);
            Assert.Null(station.OperatorID); // FIXME: FLO network is not in the switch cases
            Assert.Equal((int)StandardStatusTypes.Operational, station.StatusTypeID);
            Assert.Equal(1, station.UsageTypeID); // public
            Assert.Equal(44, station.AddressInfo.CountryID); // CA
            Assert.Equal("Canadian Tire - Lebourgneuf", station.AddressInfo.Title);
            Assert.Equal("5500 Boulevard des Gradins", station.AddressInfo.AddressLine1);
            Assert.All(station.Connections, c => Assert.Equal(3, c.LevelID));
            Assert.All(station.Connections, c => Assert.Equal(50.0, c.PowerKW));
            Assert.Equal(2, station.Connections.Count);
            Assert.Contains(station.Connections, c => c.ConnectionTypeID == (int)StandardConnectionTypes.CHAdeMO && c.Quantity == 1);
            Assert.Contains(station.Connections, c => c.ConnectionTypeID == (int)StandardConnectionTypes.CCSComboType1 && c.Quantity == 1); // FIXME: Should be 4
            Assert.Equal(100, station.SubmissionStatus.ID);
        }

        private static CoreReferenceData CreateCoreReferenceData()
        {
            return new CoreReferenceData
            {
                SubmissionStatusTypes =
                [
                    new SubmissionStatusType { ID = 100, Title = "Imported and Published" },
                    new SubmissionStatusType { ID = 1001, Title = "Delisted Duplicate" }
                ],
                StatusTypes =
                [
                    new StatusType { ID = 0, Title = "Unknown" },
                    new StatusType { ID = 50, Title = "Operational" },
                    new StatusType { ID = 100, Title = "Not Operational" }
                ],
                UsageTypes =
                [
                    new UsageType { ID = 1, Title = "Public" },
                    new UsageType { ID = 2, Title = "Private" },
                    new UsageType { ID = 4, Title = "Public Membership Required" },
                    new UsageType { ID = 5, Title = "Public Pay At Location" },
                    new UsageType { ID = 7, Title = "Public Notice Required" }
                ],
                Operators =
                [
                    new OperatorInfo { ID = 1, Title = "Unknown" }
                ],
                ChargerTypes =
                [
                    new ChargerType { ID = 1, Title = "Level 1" },
                    new ChargerType { ID = 2, Title = "Level 2" },
                    new ChargerType { ID = 3, Title = "Level 3" }
                ]
            };
        }
    }
}
