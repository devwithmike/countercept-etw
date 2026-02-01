import os
import time
import json
import logging
import signal
import sys
from datetime import datetime

import pika
from elasticsearch import Elasticsearch, helpers
from google.protobuf.json_format import MessageToDict

from proto import telemetry_pb2

# --- Configuration ---
RABBITMQ_HOST = os.getenv("RABBITMQ_HOST", "localhost")
RABBITMQ_QUEUE = "countercept_events"
ELASTIC_HOST = os.getenv("ELASTICSEARCH_HOST", "http://localhost:9200")
print("===============================")
print(ELASTIC_HOST)
print("===============================")
BATCH_SIZE = 100  # Number of events to hold before writing to DB
FLUSH_INTERVAL = 5.0  # Seconds to wait before forcing a write

# Setup Logging
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    handlers=[logging.StreamHandler()]
)
logger = logging.getLogger(__name__)

class TelemetryIngestor:
    def __init__(self):
        self.buffer = []
        self.last_flush_time = time.time()
        logger.info(f"Connecting to ElasticSearch at {ELASTIC_HOST}...")

        # For v8 client to talk to an unsecured v8 server:
        self.es = Elasticsearch(
            ELASTIC_HOST,
            # Force the client to realize we are not using SSL
            verify_certs=False,
            # Disable the 'Accept' header compatibility mode that often triggers 400s
            meta_header=False
        )

        # Simplified health check
        try:
            info = self.es.info()
            logger.info(f"Connected to cluster: {info['cluster_name']}")
        except Exception as e:
            logger.error(f"Could not connect to ElasticSearch: {e}")
            sys.exit(1)

    def flush_buffer(self):
        """Writes the buffered events to ElasticSearch using the Bulk API."""
        if not self.buffer:
            return

        try:
            # Use the helper to perform a bulk insert (highly optimized)
            success, failed = helpers.bulk(self.es, self.buffer, stats_only=True)
            logger.info(f"Flushed batch: {success} indexed, {failed} failed.")

        except Exception as e:
            logger.error(f"Failed to flush buffer to ElasticSearch: {e}")
            # In a production app, you might want to retry or dump to a 'dead letter' file
        finally:
            self.buffer = []
            self.last_flush_time = time.time()

    def process_message(self, ch, method, properties, body):
        """Callback function triggered when RabbitMQ pushes a message."""
        try:
            # 2. Deserialize Protobuf
            # We create an empty object and parse the raw bytes into it
            event = telemetry_pb2.EtwEvent()
            event.ParseFromString(body)

            # 3. Transform to Dict
            # ElasticSearch speaks JSON, so we convert the Proto object to a native Dict.
            # preserving_proto_field_name=True keeps names like 'event_uuid' instead of 'eventUuid'
            doc = MessageToDict(event, preserving_proto_field_name=True)

            # Add a timestamp for when *we* processed it (latency tracking)
            doc["ingested_at"] = datetime.utcnow().isoformat()

            # 4. Prepare for Bulk Indexing
            # We determine the index name dynamically based on the event date.
            # Pattern: countercept-events-YYYY.MM.DD
            index_name = f"countercept-events-{datetime.now().strftime('%Y.%m.%d')}"

            action = {
                "_index": index_name,
                "_source": doc
            }

            self.buffer.append(action)

            # 5. Check Flush Conditions
            # If buffer is full OR time has passed, write to DB
            if len(self.buffer) >= BATCH_SIZE or \
               (time.time() - self.last_flush_time) > FLUSH_INTERVAL:
                self.flush_buffer()

            # Acknowledge the message to RabbitMQ so it removes it from the queue
            ch.basic_ack(delivery_tag=method.delivery_tag)

        except Exception as e:
            logger.error(f"Error processing message: {e}")
            # Nack: Tell RabbitMQ we failed, don't requeue it (or do, depending on strategy)
            ch.basic_nack(delivery_tag=method.delivery_tag, requeue=False)

    def start_consuming(self):
        """Main loop connecting to RabbitMQ."""
        while True:
            try:
                logger.info(f"Connecting to RabbitMQ at {RABBITMQ_HOST}...")
                connection = pika.BlockingConnection(
                    pika.ConnectionParameters(host=RABBITMQ_HOST)
                )
                channel = connection.channel()

                # Declare queue (idempotent: creates if not exists)
                channel.queue_declare(queue=RABBITMQ_QUEUE, durable=True)

                # Tell RabbitMQ not to flood us; give us 1 message at a time
                channel.basic_qos(prefetch_count=BATCH_SIZE)

                channel.basic_consume(
                    queue=RABBITMQ_QUEUE,
                    on_message_callback=self.process_message
                )

                logger.info(" [*] Waiting for logs. To exit press CTRL+C")
                channel.start_consuming()

            except pika.exceptions.AMQPConnectionError:
                logger.error("RabbitMQ connection lost, retrying in 5s...")
                time.sleep(5)
            except KeyboardInterrupt:
                logger.info("Shutting down...")
                self.flush_buffer() # Save whatever is left
                break

if __name__ == "__main__":
    print("Running Countercept Telemetry Ingestor...")
    ingestor = TelemetryIngestor()
    ingestor.start_consuming()