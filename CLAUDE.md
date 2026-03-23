# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

E.D.G.A.R.s is a protocol bridge API between LLM clients (UE5, web frontends) and Ollama servers. It converts WebSocket streaming to HTTP SSE, enabling real-time bidirectional communication since UE5 doesn't support server-sent events natively.

The project is transitioning from a Python FastAPI backend to a .NET ASP.NET Core backend. The .NET backend (`backend/edgar-api/`) is the active implementation; the Python backend (`backend/api/`) is legacy.

## Build & Run Commands

### .NET Backend (primary)

```bash
# Build
dotnet build backend/edgar-api/Edgar.Service/Edgar.Service.csproj

# Run (development)
cd backend/edgar-api/Edgar.Service && dotnet run

# Publish Windows x64 self-contained binary
./build_backend.ps1
# Or manually:
dotnet publish backend/edgar-api/Edgar.Service/Edgar.Service.csproj -c Development -o ./publish --self-contained -r win-x64

# Docker
docker build -f backend/edgar-api/Edgar.Service/Dockerfile -t edgar-api .
```

### Vue Frontend

```bash
cd frontend/edgars-face
npm install
npm run dev       # Dev server at localhost:5173
npm run build     # Type-check (vue-tsc -b) then Vite build
npm run preview   # Preview production build
```

### Full-stack local development

Requires Ollama running (`ollama serve`) with at least one model pulled (e.g. `ollama pull qwen3:8b`).

## Architecture

```
Client (UE5/Web) ←→ WebSocket ←→ E.D.G.A.R.s API ←→ HTTP/SSE ←→ Ollama
```

### .NET Backend (`backend/edgar-api/Edgar.Service/`)

- **SessionManager** — WebSocket lifecycle, receives messages, coordinates session state
- **MessageHandler** — Async non-blocking message processor; handles user prompts and tool responses, tracks request progress
- **LlmService** — Streams HTTP requests to Ollama, deserializes JSON chunks in real-time
- **SessionService / InMemorySessionRepository** — Session persistence across WebSocket reconnections
- **OllamaModelDefinitionProvider** — Fetches and caches available models from Ollama
- **OllamaHttpClientAuthentication** — Cloudflare Access + Bearer token auth

### Frontend (`frontend/edgars-face/`)

Vue 3 + TypeScript + PrimeVue + TailwindCSS. Key modules:

- **WebSocketManager** (`websocket-manager.ts`) — Low-level WebSocket connection handling
- **MessageManager** (`message-manager.ts`) — Request/response correlation and timeouts
- **EdgarsTerminal** (`components/EdgarsTerminal.vue`) — Main terminal-style UI
- **GameView / HomeView** — Game screen (connected to LLM) and menu screen (no LLM)

### Data Flow: Prompt → Tool Call → Response

1. Client sends `{ headers: [...], body: "message" }` via WebSocket
2. Backend appends to chat history, POSTs to Ollama
3. Ollama streams chunks; backend relays content back via WebSocket
4. On tool calls: backend sends `tool_call_waiting` signal, client executes tools locally, sends results with `role: "tool"`
5. Backend re-prompts Ollama with tool results; repeats until complete

## WebSocket Protocol

All messages use an envelope format with headers array and body string. Key headers: `prompt-id` (required UUID), `tool-call-id`, `chunk-id`, `content-type`, `role`, `signal`. Signals: `request_complete`, `tool_call_waiting`. Content types: `text/plain`, `application/json`, `application/json+tool_call`, `empty`.

## Tech Stack

| Layer | Stack |
|-------|-------|
| Backend | C# / .NET 10.0 / ASP.NET Core / Serilog / System.Text.Json (snake_case) |
| Frontend | Vue 3.5 / TypeScript 5.9 / Vite 7 / PrimeVue 4.5 / TailwindCSS 4.2 / Pinia |
| Infra | Docker / GitHub Actions / Firebase Hosting (frontend) / GHCR + Cloudflare Tunnel (backend) |
| External | Ollama (LLM inference) / Grafana Loki (logs) |

## Key Configuration

- `backend/edgar-api/Edgar.Service/appsettings.json` — Ollama endpoint, WebSocket settings, Serilog/Loki logging
- `backend/api/.env` — Legacy Python API config (API_BASE_URL, Cloudflare Access creds)
- `frontend/edgars-face/vite.config.ts` — Vite with Vue plugin, TailwindCSS, PrimeVue auto-import

## Conventions

- Sessions are UUID-based with chat history persisted across WebSocket disconnections
- .NET backend uses dependency injection, structured logging with contextual session/request IDs
- JSON serialization uses snake_case naming policy throughout
- Feature branches follow `Feature/<name>` or `fix/<name>` naming
