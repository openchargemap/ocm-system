## API Mirror Service
This dotnet core worker can be registered as a systemd service which in turn runs a complete read-only clone of the OCM API, syncing data changes from the master API.

This service requires:
- A host capable of running the latest .net core runtime.
- MongoDB installed on the host with read/write enabled for local connections.

### TODO:
- 'Official' clones could register/deregister themselves with master API (or cloudflare worker) on service startup/shutdown to participate in API reads.
- Implement full resync and full cache invalidation. If cache is syncing from empty, API calls need to error otherwise cache could be used for live queries.
- Implement block hashing compare, currently using dates and IDs as sync keys leaves potential room for some items to be out of sync
- Docker configuration for minimal setup of new clones. Auto update of clones when new update released: https://docs.docker.com/docker-hub/builds/, https://containrrr.github.io/watchtower/

### Linux build
The OCM Api and website we're original built using the .net framework on Windows, with SQL server as the backend database. A mongodb based caching layer was later added to the API which allowed read operations to avoid querying the SQL database. The system has since been ported to linux as a systemd based worker service.

The API can be run as a standalone read-only mirror of the main API, with an automated sync of data pulled from the master API.

- Install dotnet core 3.x sdk for your system (~350MB), check with `dotnet --version`
	- `sudo snap install dotnet-sdk --channel=3.1/stable --classic`
- Install latest monogdb for your system, set service to run on startup
- Clone and build the ocm api:
    - `git clone https://github.com/openchargemap/ocm-system`
    - `cd ocm-system/API/OCM.Net/OCM.API.Web`
    - `dotnet build`

To run the API server on port 5000 bound to default public network interface:
- Debug: `dotnet run --urls http://0.0.0.0:5000`
- Release: `dotnet run -c Release --urls http://0.0.0.0:5000`

To build and deploy the API service worker as systemd managed service:

- Build as release: 
```sh
cd ~/ocm-system/API/OCM.Net/OCM.API.Worker
dotnet publish -c Release
sudo mkdir /opt/ocm-api
sudo cp -R bin/Release/netcoreapp3.1/publish/* /opt/ocm-api
```

### Deploying as a service (systemd)
- Copy ocm-api.service file to systemd service config location:
    - `sudo cp ocm-api.service /etc/systemd/system/ocm-api.service`
- Create/update symlink in /usr/sbin for the build:
    - `sudo ln -s -f /opt/ocm-api/OCM.API.Worker /usr/sbin/ocm-api`
 - Reload systemd config: 
    - `sudo systemctl daemon-reload`
 - Check systemd config for service: 
    - `sudo systemctl status ocm-api`
 - Start service: 
    - `sudo systemctl start ocm-api`
 - Set service to run whenever host restarts: 
    - `sudo systemctl enable ocm-api.service`
 - Watch log as service starts up and sync rebuilds cache:
    - `journalctl -f` or `journalctl -f -u ocm-api`

### Refresh build (apply latest software changes):
 
```sh

cd ~/ocm-system/API/OCM.Net/OCM.API.Worker
git pull
dotnet publish -c Release
sudo systemctl stop ocm-api
sudo cp -R bin/Release/netcoreapp3.1/publish/* /opt/ocm-api
sudo systemctl start ocm-api

```


### Example Ubuntu 18.x : fresh install docker and start api clone
```sh
// https://www.digitalocean.com/community/tutorials/how-to-install-and-use-docker-on-ubuntu-18-04
sudo apt update
sudo apt install apt-transport-https ca-certificates curl software-properties-common
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add -
sudo add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/ubuntu bionic stable"

```