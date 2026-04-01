# Adding an OCPI Provider

There are two ways to add a new OCPI Provider:

## Option 1: Configuration-Based Provider (Recommended for new providers)

Use the configurable OCPI provider system to add new providers via JSON configuration without writing any code.

### Steps:

1. **Add DataProvider definition in database** - Create the OCM data provider record
2. **Add/get operator ID(s)** for the target network(s)
3. **Store credential in keyvault** (if API requires authentication)
4. **Add provider to the OCPI configuration file** (e.g., `ocpi-providers.json`)

### Configuration File Format:

Create or update the OCPI providers configuration file (path specified in `ImportSettings.OCPIProvidersConfigPath`):

```json
{
  "Providers": [
    {
      "ProviderName": "my-new-provider",
      "OutputNamePrefix": "mynewprovider",
      "Description": "My New OCPI Provider",
      "DataProviderId": 99,
      "LocationsEndpointUrl": "https://api.example.com/ocpi/2.2/locations",
      "AuthHeaderKey": "Authorization",
      "CredentialKey": "OCPI-MYPROVIDER",
      "DefaultOperatorId": 1234,
      "IsAutoRefreshed": true,
      "IsProductionReady": true,
      "IsEnabled": true,
      "AllowDuplicatePOIWithDifferentOperator": true,
      "OperatorMappings": {
        "Operator Name From OCPI": 1234,
        "Another Operator": 5678
      },
      "ExcludedLocationIds": []
    }
  ]
}
```

### Configuration Properties:

| Property | Required | Description |
|----------|----------|-------------|
| `ProviderName` | Yes | Unique identifier for the provider (e.g., "electricera") |
| `OutputNamePrefix` | No | Prefix for output files (defaults to ProviderName) |
| `Description` | No | Human-readable description |
| `DataProviderId` | Yes | OCM Data Provider ID from database |
| `LocationsEndpointUrl` | Yes | OCPI locations endpoint URL |
| `AuthHeaderKey` | No | Custom auth header name (defaults to "Authorization") |
| `CredentialKey` | No | Key to lookup auth value from credentials vault |
| `DefaultOperatorId` | No | Default OCM Operator ID when not mapped |
| `IsAutoRefreshed` | No | Enable auto-refresh (default: true) |
| `IsProductionReady` | No | Enable for production imports (default: true) |
| `IsEnabled` | No | Enable this provider (default: true) |
| `AllowDuplicatePOIWithDifferentOperator` | No | Allow same location with different operators (default: true) |
| `OperatorMappings` | No | Map OCPI operator names to OCM Operator IDs |
| `ExcludedLocationIds` | No | List of OCPI location IDs to skip |

See `ocpi-providers.example.json` for complete examples.

---

## Option 2: Code-Based Provider (Legacy approach)

For providers requiring custom processing logic, create a new class inheriting from `ImportProvider_OCPI`.

### Steps:

- Add new DataProvider definition in database
- Add/get operator id for the target network
- Store credential in keyvault
- Implement the OCPI import provider in OCM.Import.Common.Providers.OCPI

### Detail:

- Download and verify the OCPI feed manually to check credentials and data format
- Add a test with a copy of the json feed in OCM.API.Tests - ImportTests. Review test for data consistency and completeness.
- Implement an OCPI provider with required settings in OCM.Import.Common.Providers.OCPI
    - Add a new data provider ID in DB and in the import provider.
    - Review the network operator mappings in the new import provider. Provider will at least map to their own network operator
    - If providing services for other operators there may be many operators to map to.
    - Store Import credentials (Authorization header value etc) in Azure keyvault
- Add import provider to OCM.Import.Worker
    - appsettings.json EnabledImports
    - Add provider to OCM.Import.Common.ImportManager

### Example Code-Based Provider:

```csharp
using System.Collections.Generic;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_MyProvider : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_MyProvider() : base()
        {
            ProviderName = "myprovider";
            OutputNamePrefix = "myprovider";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            CredentialKey = "OCPI-MYPROVIDER"; // or null if no auth needed

            DefaultOperatorID = 1234;

            Init(dataProviderId: 99, "https://api.example.com/ocpi/2.2/locations");
        }

        public override Dictionary<string, int> GetOperatorMappings()
        {
            return new Dictionary<string, int>()
            {
                { "My Operator", 1234 }
            };
        }
    }
}
```

---

## Migrating from Code-Based to Configuration-Based

Existing code-based providers can be gradually migrated to configuration-based providers:

1. Create the JSON configuration entry for the provider
2. Set `IsEnabled = true` in the configuration
3. The ImportManager will skip configured providers that have the same name as existing hardcoded providers
4. Once verified, remove the hardcoded provider class and it will use the configured version

    

    