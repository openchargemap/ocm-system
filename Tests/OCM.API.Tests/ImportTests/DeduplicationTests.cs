using System.Collections.Generic;
using System.Linq;
using OCM.API.Common.Model;
using OCM.Import;
using Xunit;

namespace OCM.API.Tests.ImportTests
{
    /// <summary>
    /// Deduplication rules applied to POIs within a single import batch.
    /// </summary>
    public class DeduplicationTests
    {
        private static ImportManager CreateImportManager()
        {
            return new ImportManager(new ImportSettings
            {
                TempFolderPath = System.IO.Path.GetTempPath(),
                MasterAPIBaseUrl = "https://api.openchargemap.io/v3"
            });
        }

        private static ChargePoint CreatePOI(string title, double latitude, double longitude, string addressLine1 = null, string reference = null)
        {
            return new ChargePoint
            {
                DataProviderID = 43,
                DataProvidersReference = reference,
                AddressInfo = new AddressInfo
                {
                    Title = title,
                    AddressLine1 = addressLine1 ?? title,
                    Latitude = latitude,
                    Longitude = longitude
                }
            };
        }

        [Fact]
        public void IdenticalTitlesAtDifferentLocationsAreNotDuplicates()
        {
            var manager = CreateImportManager();

            // roughly 550m apart, well beyond the title threshold
            var first = CreatePOI("Acme Charging Station", 52.2300, 21.0100, "Piękna 3", "REF-1");
            var second = CreatePOI("Acme Charging Station", 52.2350, 21.0150, "Radzyńska 7", "REF-2");

            Assert.False(manager.IsDuplicateLocation(second, first, compareTitle: true));
        }

        [Fact]
        public void IdenticalTitlesAtTheSameLocationAreDuplicates()
        {
            var manager = CreateImportManager();

            // roughly 22m apart, the same physical site listed twice
            var first = CreatePOI("Acme Charging Station", 52.2300, 21.0100, "Piękna 3", "REF-1");
            var second = CreatePOI("Acme Charging Station", 52.2302, 21.0100, "Piękna 3a", "REF-2");

            Assert.True(manager.IsDuplicateLocation(second, first, compareTitle: true));
        }

        [Fact]
        public void DifferentTitlesInCloseProximityAreStillDuplicates()
        {
            var manager = CreateImportManager();

            // proximity alone remains sufficient, regardless of title
            var first = CreatePOI("Acme Charging Station", 52.2300, 21.0100, "Piękna 3", "REF-1");
            var second = CreatePOI("Beta Charging Point", 52.2300, 21.0101, "Piękna 5", "REF-2");

            Assert.True(manager.IsDuplicateLocation(second, first, compareTitle: true));
        }

        /// <summary>
        /// A network naming every site identically must not collapse to a single POI. This is the
        /// failure that hid the EV24 Poland locations from import.
        /// </summary>
        [Fact]
        public void BatchOfCommonlyNamedDistinctLocationsSurvivesDeduplication()
        {
            var manager = CreateImportManager();

            // 12 distinct sites spread ~1km apart, all sharing one placeholder name
            var batch = Enumerable.Range(0, 12)
                .Select(i => CreatePOI("EV24 Charging Station", 52.0 + (i * 0.01), 21.0 + (i * 0.01), $"Street {i}", $"REF-{i}"))
                .ToList();

            var survivors = RunInBatchDeduplication(manager, batch);

            Assert.Equal(batch.Count, survivors.Count);
        }

        /// <summary>
        /// Mirrors the consecutive-pair pass ImportManager.DeDuplicateList runs over a sorted batch.
        /// </summary>
        private static List<ChargePoint> RunInBatchDeduplication(ImportManager manager, List<ChargePoint> batch)
        {
            var sorted = batch
                .OrderBy(c => c.AddressInfo.Latitude)
                .ThenBy(c => c.AddressInfo.Longitude)
                .ToList();

            var survivors = new List<ChargePoint>();
            ChargePoint previous = null;

            foreach (var item in sorted)
            {
                if (previous == null || !manager.IsDuplicateLocation(item, previous, compareTitle: true))
                {
                    survivors.Add(item);
                }

                previous = item;
            }

            return survivors;
        }
    }
}
