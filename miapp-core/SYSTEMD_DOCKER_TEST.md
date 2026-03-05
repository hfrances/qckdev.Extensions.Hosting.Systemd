# Run miapp-core under systemd in Docker

This setup is intended to validate `UseSystemd()` with a real `systemd` unit (`Type=notify`).

## Build image

```bash
docker build -f miapp-core/Dockerfile.systemd -t miapp-core:systemd .
```

Run this command from:

`qckdev.Extensions.Hosting.Systemd`

## Run container

```bash
docker run --rm -d ^
  --name miapp-core-systemd ^
  --privileged ^
  --cgroupns=host ^
  -v /sys/fs/cgroup:/sys/fs/cgroup:rw ^
  -p 8080:80 ^
  miapp-core:systemd
```

Notes:
- `--privileged` and cgroup mount are required for `systemd` in container.
- This is for local testing only, not a production deployment model.

## Verify service state

```bash
docker exec miapp-core-systemd systemctl status miapp-core
docker exec miapp-core-systemd systemctl is-active miapp-core
docker exec miapp-core-systemd journalctl -u miapp-core --no-pager -n 100
```

## Verify HTTP endpoint

```bash
curl http://localhost:8080/swagger/index.html
```

## Stop container

```bash
docker stop miapp-core-systemd
```

## Automated test (MSTest)

The integration test `SystemdDockerIntegrationTest` is gated by env var:

`RUN_SYSTEMD_DOCKER_TESTS=1`

Example:

```bash
RUN_SYSTEMD_DOCKER_TESTS=1 dotnet test qckdev.Extensions.Hosting.Systemd.Test/qckdev.Extensions.Hosting.Systemd.Test.csproj -c Release -f net6.0 --filter FullyQualifiedName~SystemdDockerIntegrationTest
```
