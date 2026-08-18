# Dashboard

[![Deploy](https://github.com/tzer0m/Dashboard/actions/workflows/deploy.yml/badge.svg)](https://github.com/tzer0m/Dashboard/actions/workflows/deploy.yml)

A central status page for a self-hosted homelab. It shows every service in one place — up/down status, response time, and the latest deploy result — and pushes updates to connected clients in real time.

## Features

- **Live status via SignalR** — a background service polls [Uptime Kuma](https://github.com/louislam/uptime-kuma)'s public status page API on a configurable interval and broadcasts changes to all connected clients over a SignalR hub, so the page updates in place instead of reloading. This replaced an earlier `location.reload()`-based refresh and significantly cut Cloudflare Worker traffic.
- **Deploy badges from GitHub Actions** — a server-side badge service authenticates to the GitHub Actions API (so it works for private repos), checks the latest `deploy.yml` run for a given repo, and returns a shields.io-compatible message/colour. Results are cached in memory for 60 seconds to stay well under GitHub's rate limits.
- **Shields.io-compatible badge endpoint** — `GET /Badge?repo=owner/name` exposes the same deploy status as a JSON endpoint shields.io can render directly, so the badge can be embedded elsewhere without leaking a GitHub token client-side.
- **External status API** — `GET /Api/Status` (API-key protected via `X-API-Key`) returns combined health + deploy status for every configured service, as consumed by a Home Assistant integration.
- **Per-service metadata** — each configured service records its name, URL, type, whether it's locally or globally accessible, its host device, local IP/port, whether it requires auth, and an optional favicon override.
- **Network diagram** — embeds a draw.io network diagram alongside the service list.

## Tech stack

- ASP.NET Core (Razor Pages) on .NET 10
- SignalR for live client updates
- `IMemoryCache` for badge caching
- A `BackgroundService` polling loop for Kuma status

## Configuration

Configuration lives in `appsettings.json` (see `appsettingsGit.json` for the shape, values stripped).

Each entry under `Services` needs a `Name` that exactly matches the corresponding monitor's name in Kuma, or its status won't be picked up (a warning is logged once per unmatched service).

## Deployment

Deployed via GitHub Actions on push to `master`, using a self-hosted runner. The workflow stops the `Dashboard.service` systemd unit, publishes a fresh build, and restarts the service.
