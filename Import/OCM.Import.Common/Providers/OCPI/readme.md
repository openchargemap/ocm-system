# Adding an OCPI Provider

To add a new OCPI Provider:
- download and verify the OCPI feed manually to check credentials and data format
- add a test with a copy of the json feed in OCM.API.Tests -ImportTests. Review test for data consistency and completeness.
- Implement an OCPI provider with required settings in OCM.Import.Common.Providers.OCPI
    - Add a new data provider ID in DB and in the import provider.
    - Review the network operator mapings in the new import provider. Provider will at least map to their own network operator
    - if providing services for other operators there may be may operators to map to.
    - Store Import credentials (Authorization header value etc) in Azure keyvault
- Add import provider to OCM.Import.Worker
    - appsettings.json EnabledImports
    - add provider to OCM.Import.Common.ImportManager - 
    