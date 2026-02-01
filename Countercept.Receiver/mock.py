import os
import time
import pika
from datetime import datetime
from google.protobuf.timestamp_pb2 import Timestamp

# Import your generated proto
from proto import telemetry_pb2

# Configuration - Match your docker settings
RABBITMQ_HOST = os.getenv("RABBITMQ_HOST", "localhost")
QUEUE_NAME = "countercept_events"

def send_mock_event():
    # 1. Setup RabbitMQ Connection
    print(f"Connecting to RabbitMQ at {RABBITMQ_HOST}...")
    connection = pika.BlockingConnection(pika.ConnectionParameters(host=RABBITMQ_HOST))
    channel = connection.channel()

    # Ensure queue exists
    channel.queue_declare(queue=QUEUE_NAME, durable=True)

    # 2. Create a Mock ETW Event using Protobuf
    event = telemetry_pb2.EtwEvent()
    event.event_uuid = "mock-uuid-12345"

    # Handle the Protobuf Timestamp format
    now = datetime.utcnow()
    event.timestamp.FromDatetime(now)

    event.machine_name = "MOCK-WINDOWS-VM"
    event.provider_name = "Windows-Kernel-Process"
    event.event_id = 1  # Process Start
    event.process_id = 9999
    event.task_name = "Process"
    event.opcode_name = "Start"

    # Add Payload fields
    event.payload["ImageName"] = r"C:\Windows\System32\calc.exe"
    event.payload["CommandLine"] = "calc.exe /multiproc"
    event.payload["ParentID"] = "444"

    # 3. Serialize and Publish
    message_body = event.SerializeToString()

    channel.basic_publish(
        exchange='',
        routing_key=QUEUE_NAME,
        body=message_body,
        properties=pika.BasicProperties(
            delivery_mode=2,  # make message persistent
        )
    )

    print(f" [x] Sent Mock Event: {event.payload['ImageName']}")
    connection.close()

if __name__ == "__main__":
    send_mock_event()