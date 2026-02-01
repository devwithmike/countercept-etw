# Countercept.Receiver/Dockerfile
FROM python:3.11-slim

# Install uv
COPY --from=ghcr.io/astral-sh/uv:latest /uv /bin/uv

WORKDIR /app

# Copy dependency files
COPY Countercept.Receiver/pyproject.toml Countercept.Receiver/uv.lock ./

# Install dependencies
RUN uv sync --frozen

# Copy the rest of the code AND the generated proto folder
COPY Countercept.Receiver/ .
# Ensure the compiled proto python file is accessible
COPY proto/ ../proto/

CMD ["uv", "run", "main.py"]