# Backend in Docker, frontend on the host (Vite on a Windows bind mount is too slow).
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

docker compose stop web
docker compose up -d api

Set-Location client
pnpm install
pnpm dev
