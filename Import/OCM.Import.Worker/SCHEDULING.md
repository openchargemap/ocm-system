# OCM Import Worker - Intelligent Scheduling

## Overview

## How It Works

### 1. **API-Driven Scheduling**

Every cycle (configured via `ImportRunFrequencyMinutes`), the worker:

1. Fetches the current `DataProvider` list from the OCM API
2. Filters to only the providers listed in `EnabledImports` configuration
3. Identifies providers that are **due for import** (last imported > 24 hours ago or never imported)
4. **Excludes providers that recently failed** (within the last 24 hours)
5. Selects the provider with the **oldest** `DateLastImported` for processing

### 2. **Import Threshold**

- **Threshold**: 24 hours (configurable via `IMPORT_DUE_THRESHOLD_HOURS` constant)
- Providers are only imported if their `DateLastImported` is:
  - `NULL` (never imported), or
  - More than 24 hours ago
- **AND** they haven't failed within the last 24 hours

### 3. **Failed Import Tracking** ??

The worker maintains an **in-memory cache** of failed imports:

- When an import **fails with an exception**:
  - Provider is added to failed imports cache with timestamp
  - Provider will be **skipped** for the next 24 hours
  - Failure count is tracked for diagnostics

- When an import **completes successfully** (with or without changes):
  - Provider is removed from failed imports cache
  - Normal scheduling resumes
  - Note: `importedOK = false` is NOT treated as a failure - it may just mean no changes were needed

- **Automatic cleanup**:
  - Failed import records older than 24 hours are automatically removed
  - Providers become eligible for retry after threshold expires

### 4. **Priority Queue**

Providers are processed in order of **oldest to newest** `DateLastImported`:
- Provider A (last imported 3 days ago, never failed) ? **Highest priority**
- Provider B (last imported 2 days ago, never failed) ? Medium priority
- Provider C (last imported 3 days ago, **failed 2 hours ago**) ? **Blocked for 22 more hours**
- Provider D (last imported 12 hours ago) ? Not yet due

### 5. **Wait Behavior**

If no providers are due for import:
- The worker logs when the next provider will be due
- Reports if any providers are blocked by recent failures
- Waits until the next check cycle
- Does NOT perform any unnecessary imports

## Configuration

### appsettings.json

```json
{
  "ImportSettings": {
    "MasterAPIBaseUrl": "https://api-01.openchargemap.io/v3",
    "ImportRunFrequencyMinutes": 5,
    "EnabledImports": [
      "otopriz.com.tr",
      "ev24.cloud",
      "greems.io",
      "zepto.pl",
      "chargesini.com",
      "powergo",
      "punkt-e",
      "afdc.energy.gov"
    ]
  }
}
```

### Key Settings

| Setting | Description | Default |
|---------|-------------|---------|
| `ImportRunFrequencyMinutes` | How often to check for imports due | 5 minutes |
| `EnabledImports` | List of provider names (must match `DataProvider.Title` in OCM API) | [] |
| `MasterAPIBaseUrl` | OCM API base URL | https://api-01.openchargemap.io/v3 |

**Code Configuration:**
- `IMPORT_DUE_THRESHOLD_HOURS` - Applies to both import frequency and failure retry threshold (24 hours)

## Logging Examples

### Provider Due for Import
```
[2025-01-15 10:00:00] Checking for imports due..
[2025-01-15 10:00:01] Performing import for 'zepto.pl' (ID: 44, Last imported: 26.5 hours ago). 3 provider(s) total are due for import.
[2025-01-15 10:05:23] Import for 'zepto.pl' completed successfully in 322.5 seconds
[2025-01-15 10:05:23] Provider 'zepto.pl' removed from failed imports tracking after successful import
```

### Import with No Changes
```
[2025-01-15 10:00:00] Performing import for 'powergo' (ID: 35, Last imported: 25.0 hours ago)
[2025-01-15 10:01:15] Import for 'powergo' completed in 75.2 seconds (no changes or warnings)
```
**Note:** This is NOT treated as a failure - the provider will be imported again normally in 24 hours.

### Import Failure
```
[2025-01-15 10:00:00] Performing import for 'afdc.energy.gov' (ID: 23, Last imported: 30.2 hours ago)
[2025-01-15 10:02:15] Import failed for provider 'afdc.energy.gov': Connection timeout. Will not retry for 24 hours.
[2025-01-15 10:02:15] Provider 'afdc.energy.gov' added to failed imports tracking
```
**Note:** Only actual exceptions trigger failure tracking.

### Repeated Failure
```
[2025-01-16 12:00:00] Performing import for 'afdc.energy.gov' (ID: 23, Last imported: 50.2 hours ago)
[2025-01-16 12:01:30] Import failed for provider 'afdc.energy.gov': Connection timeout. Will not retry for 24 hours.
[2025-01-16 12:01:30] Provider 'afdc.energy.gov' has failed 2 time(s). Latest error: Connection timeout
```

### Providers Blocked by Failures
```
[2025-01-15 12:00:00] Checking for imports due..
[2025-01-15 12:00:01] 2 provider(s) are due but blocked by recent failures. They will be retried after the failure threshold expires.
[2025-01-15 12:00:01] No imports currently due. Next provider 'powergo' due in 6.2 hours at 2025-01-15 18:15:00 UTC
```

### Failure Threshold Expired
```
[2025-01-16 10:02:16] Checking for imports due..
[2025-01-16 10:02:16] Provider 'afdc.energy.gov' removed from failed imports tracking - threshold expired, will be retried if due
[2025-01-16 10:02:17] Performing import for 'afdc.energy.gov' (ID: 23, Last imported: 48.2 hours ago)
```

### No Providers Due
```
[2025-01-15 10:05:00] Checking for imports due..
[2025-01-15 10:05:01] No imports currently due. Next provider 'powergo' due in 6.2 hours at 2025-01-15 16:15:00 UTC
```

## Benefits

### 1. **Automatic Load Balancing**
- Providers are naturally distributed over time
- No manual intervention needed to reorder the import queue

### 2. **Real-Time Priority**
- If a provider fails and needs to retry, it automatically becomes high priority
- New providers added to the API are automatically detected

### 3. **Resource Efficiency** ??
- Only imports providers that actually need updating
- Avoids unnecessary API calls and processing
- **Prevents wasting resources on repeatedly failing providers**
- **Automatic retry after threshold expires**

### 4. **Self-Healing** ??
- If an import fails, the provider remains in the "due" list but is blocked from retry
- **Will automatically retry after 24 hours if still due**
- **Tracks failure count for monitoring chronic issues**
- **Successful import clears failure status**

### 5. **Transparent Scheduling**
- Clear logging shows which providers are due and why
- **Shows which providers are blocked by recent failures**
- **Logs when failed providers become eligible for retry**
- Easy to understand the scheduling logic from logs

### 6. **Failure Isolation** ??
- One failing provider doesn't block others
- Failed providers are quarantined for threshold period
- Other providers continue normal scheduling

## Monitoring

### Check Import Status

View the status file:
```bash
cat /tmp/import_status.json
```

Example output:
```json
{
  "LastImportedProvider": "zepto.pl",
  "DateLastImport": "2025-01-15T10:05:23.456Z",
  "LastImportStatus": "Completed Successfully",
  "ProcessingTimeSeconds": 322.5
}
```

### View Logs

```bash
# Docker
docker logs -f ocm-import-worker

# Docker Compose
docker-compose logs -f

# Systemd
journalctl -u ocm-import-worker -f
```

### Check Failed Imports

Failed imports are logged when:
- Added to tracking
- Failure count increments
- Removed from tracking (threshold expired or successful import)

Look for log messages containing:
- `"added to failed imports tracking"`
- `"has failed X time(s)"`
- `"removed from failed imports tracking"`

### Worker Shutdown

When the worker stops, it logs current failed imports:
```
[2025-01-15 18:00:00] Import Worker is stopping
[2025-01-15 18:00:00] Current failed imports being tracked: afdc.energy.gov, sitronics
```

## Troubleshooting

### Provider Not Being Imported

**Check:**
1. Is the provider listed in `EnabledImports`?
2. Does the name exactly match `DataProvider.Title` in the API?
3. Has it been more than 24 hours since last import?
4. **Has it failed recently (within 24 hours)?** ??

**Verify:**
```bash
# Fetch data providers from API
curl "https://api-01.openchargemap.io/v3/referencedata/" | jq '.DataProviders[] | {ID, Title, DateLastImported}'

# Check worker logs for failed import tracking
docker logs ocm-import-worker | grep "failed imports tracking"
```

### Import Failing Repeatedly

**Symptoms:**
- Provider appears in "failed imports tracking" logs
- `"has failed X time(s)"` messages in logs
- Provider not being attempted despite being due

**What Happens:**
1. First failure: Provider blocked for 24 hours
2. After 24 hours: Automatically retried if still due
3. Second failure: Blocked for another 24 hours
4. Pattern continues until underlying issue is resolved

**Resolution:**
1. Check the error details in logs: `"Latest error: ..."`
2. Verify API credentials are correct
3. Test the provider's data source URL manually
4. Fix underlying issue (network, credentials, data format)
5. Wait for threshold to expire (or restart worker to clear cache)
6. Monitor for successful import

**To Force Immediate Retry:**
```bash
# Restart the worker (clears in-memory failed imports cache)
docker restart ocm-import-worker

# Or stop and start
docker-compose restart
```

### Chronic Failures

**If a provider continuously fails:**

1. **Review failure count** in logs
2. **Check error messages** for patterns
3. **Temporarily disable** in `EnabledImports` if persistently broken:
   ```json
   "EnabledImports": [
     // "afdc.energy.gov",  // Temporarily disabled - investigate API changes
     "powergo",
     "zepto.pl"
   ]
   ```
4. **Investigate root cause**:
   - API endpoint changes
   - Authentication issues
   - Data format changes
   - Network/firewall issues

### High Frequency Imports

**If providers are being imported too frequently:**

Adjust the threshold in `Worker.cs`:
```csharp
private const int IMPORT_DUE_THRESHOLD_HOURS = 48; // Change from 24 to 48
```

This affects **both**:
- Import frequency threshold
- Failure retry threshold

### No Imports Happening

**Check:**
1. Worker service is running
2. `EnabledImports` list is not empty
3. API connection is working
4. At least one provider has DateLastImported > 24 hours ago
5. **Providers aren't all blocked by recent failures** ??

**Debug:**
```bash
# Check if providers are blocked
docker logs ocm-import-worker | grep "blocked by recent failures"

# See which providers are in failed state
docker logs ocm-import-worker | grep "failed imports tracking"
```

## API Integration

### DataProvider Model

The worker relies on this API response structure:
```json
{
  "DataProviders": [
    {
      "ID": 44,
      "Title": "zepto.pl",
      "DateLastImported": "2025-01-14T08:30:00Z",
      "IsApprovedImport": true
    }
  ]
}
```

### Required API Endpoint

- **GET** `/v3/referencedata/`
- Returns `CoreReferenceData` including `DataProviders[]`

### Import Completion Notification

After each successful import:
- **GET** `/v3/system/importcompleted/{dataProviderId}`
- Updates `DateLastImported` in the database

## Performance Characteristics

### Typical Import Cycle

1. **API fetch**: ~1-2 seconds
2. **Failed imports cleanup**: <0.1 seconds
3. **Provider selection**: <0.1 seconds
4. **Import processing**: 30 seconds - 20 minutes (varies by provider size)
5. **Total cycle**: 1-20 minutes per provider

### Resource Usage

- **Memory**: 500MB - 2GB (depends on import size)
  - Failed imports cache: <1MB (negligible)
- **CPU**: 1-2 cores during active import
- **Network**: 10MB - 500MB per import (varies by provider)

### Recommended Schedule

| Scenario | `ImportRunFrequencyMinutes` | Providers Enabled |
|----------|----------------------------|-------------------|
| Production | 5-15 | 10-20 |
| Development | 1-5 | 1-5 |
| Testing | 1 | 1-2 |

### Behavior (Dynamic Priority + Failure Tracking) ??
```csharp
// Selects provider with oldest DateLastImported > 24h
// AND not recently failed
// provider[2] (48h old, never failed) ? 
// provider[5] (30h old, never failed) ? 
// provider[1] (25h old, never failed)
// ? Skips provider[3] (50h old, failed 2h ago)
```

### Breaking Changes

? **Removed**: Sequential cycling through provider list  
? **Removed**: Dependency on `import_status.json` for determining next provider  
? **Removed**: Immediate retry of failed imports  
? **Added**: Real-time API-based scheduling  
? **Added**: Automatic priority based on DateLastImported  
? **Added**: In-memory failed import tracking ??  
? **Added**: Automatic failure threshold and retry ??  
? **Improved**: Logging shows why each provider was selected  
? **Improved**: Logs show failure counts and blocked providers ??

## Data Structures

### FailedImportInfo Class ??

```csharp
internal class FailedImportInfo
{
    public string ProviderName { get; set; }
    public DateTime FailedAt { get; set; }
    public string LastErrorMessage { get; set; }
    public int FailureCount { get; set; }
}
```

**Storage**: In-memory `Dictionary<string, FailedImportInfo>`
- **Thread-safe**: Protected by lock
- **Lifetime**: Worker service lifetime
- **Cleanup**: Automatic after threshold expires
- **Reset**: Worker restart clears all entries

## Future Enhancements

### Potential Improvements

1. **Configurable Thresholds**
   - Per-provider import frequencies
   - Per-provider failure retry thresholds
   - Different thresholds for different provider types

2. **Persistent Failure Tracking** ??
   - Save failed imports to disk
   - Survive worker restarts
   - Historical failure analysis

3. **Exponential Backoff** ??
   - First failure: retry after 1 hour
   - Second failure: retry after 4 hours
   - Third failure: retry after 24 hours
   - Automatic escalation

4. **Health Checks**
   - Skip providers that are consistently failing
   - Alert on chronic failures
   - Automatic disable after X failures

5. **Manual Triggers**
   - API endpoint to force immediate import of specific provider
   - Ability to clear failure status
   - Admin UI for managing import queue

6. **Metrics & Analytics**
   - Track import success rates
   - Monitor processing times
   - Alert on failures
   - **Dashboard showing failed imports and retry times** ??

## Support

For issues or questions:
- Check logs first: `docker logs ocm-import-worker`
- Look for failed import tracking messages
- Review this guide for common scenarios
- Open an issue on GitHub with full error logs
