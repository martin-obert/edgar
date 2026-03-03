# ---- Build stage ----
FROM python:3.13-slim AS builder

WORKDIR /build

# Install uv for fast, deterministic installs
RUN pip install --no-cache-dir uv

# Layer caching: copy only dependency metadata first.
# This layer is invalidated only when dependencies change, not on code edits.
COPY pyproject.toml .

# Install only declared dependencies — the project itself is not installed as a
# package; we copy the source directly in the runtime stage instead.
RUN uv venv /opt/venv && \
    uv pip install --python /opt/venv/bin/python --no-cache -r pyproject.toml


# ---- Runtime stage ----
FROM python:3.13-slim AS runtime

RUN groupadd --gid 1000 appuser && \
    useradd --uid 1000 --gid appuser --shell /bin/bash --create-home appuser

WORKDIR /app

# Copy the fully built venv from builder
COPY --from=builder /opt/venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"

# Copy application source
COPY . ./app

# Allow top-level imports from within app/ (e.g. "from generated.Edgar import ...")
ENV PYTHONPATH="/app/app:${PYTHONPATH}"

EXPOSE 8000
USER appuser

CMD ["uvicorn", "app.main:app", \
     "--host", "0.0.0.0", \
     "--port", "8000", \
     "--proxy-headers", \
     "--forwarded-allow-ips", "*", \
     "--workers", "2"]