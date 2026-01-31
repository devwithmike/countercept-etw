# Variables
COMPOSE_FILE := docker-compose.yml
SERVICE_DIR := ./Countercept.Agent

.PHONY: up build clean help

up:
	@echo "Starting Docker containers..."
	docker-compose -f $(COMPOSE_FILE) up -d

publish:
	@echo "Publish the Countercept Agent..."
	dotnet publish $(SERVICE_DIR)/Countercept.Agent.csproj -c Release -o $(SERVICE_DIR) -r win-x64 --self-contained

down:
	@echo "Stopping Docker containers..."
	docker-compose -f $(COMPOSE_FILE) down

# Help command to list available options
help:
	@echo "Usage:"
	@echo "  make up        - Start docker-compose in detached mode"
	@echo "  make down      - Stop and remove docker containers"
	@echo "  make publish   - Publish the Countercept Agent"