# EcommerceEDA

A demo of **Event-Driven Architecture (EDA)** applied to a simple e-commerce domain, built with **.NET 10** and **RabbitMQ**.

---

## What is Event-Driven Architecture?

In a traditional system, services call each other directly (Service A calls Service B, waits for a response, then calls Service C). This creates tight coupling — if one service is slow or down, everything else is affected.

In an **event-driven** system, services communicate by sending **events** (messages describing something that happened). A service publishes an event and moves on — it doesn't wait or care who is listening. Other services pick up that event and react independently.

Think of it like a group chat: you post a message, and anyone in the chat who cares about it will read and respond. You don't need to know who they are.

---

## What is RabbitMQ?

**RabbitMQ** is the "group chat server" in this analogy — it's a **message broker** that sits between your services and handles the delivery of events.

- A service that sends a message is called a **producer**
- A service that receives and handles a message is called a **consumer**
- Messages are stored in **queues** inside RabbitMQ until a consumer picks them up
- **Exchanges** route messages to the right queues — in this project each event type gets its own exchange (fanout), so every interested service gets a copy

This means if the InventoryService is temporarily down, RabbitMQ holds the messages and delivers them when it comes back up. Nothing is lost.

---

## Project Structure

```
EcommerceEDA/
├── src/
│   ├── Contracts/           # Shared event definitions (used by all services)
│   ├── OrderService/        # HTTP API — the only entry point for users
│   ├── InventoryService/    # Checks and reserves stock
│   ├── PaymentService/      # Processes payments
│   └── NotificationService/ # Logs all events (simulates emails/alerts)
├── docker-compose.yml       # Starts RabbitMQ
└── README.md
```

---

## Event Flow

When a customer places an order, the following chain of events fires automatically:

```
User
 │
 │  POST /api/orders
 ▼
OrderService ──── publishes ────► OrderPlaced
                                       │
                         ┌─────────────┴─────────────┐
                         ▼                           ▼
                  InventoryService           NotificationService
                  checks stock               logs the event
                         │
              ┌──────────┴──────────┐
              ▼                     ▼
       InventoryReserved      StockInsufficient
              │                     │
   ┌──────────┴────────┐            ▼
   ▼                   ▼      OrderService
PaymentService   NotificationService  (status → Failed)
processes pmt    logs the event
   │
   ├──► PaymentProcessed ──► OrderService (status → PaymentProcessed)
   │                    └──► NotificationService (logs it)
   │
   └──► PaymentFailed ──► OrderService (status → Failed)
                     └──► NotificationService (logs it)
```

### Events (defined in `Contracts/Events.cs`)

| Event | Published by | Consumed by |
|---|---|---|
| `OrderPlaced` | OrderService | InventoryService, NotificationService |
| `InventoryReserved` | InventoryService | PaymentService, OrderService, NotificationService |
| `StockInsufficient` | InventoryService | OrderService, NotificationService |
| `PaymentProcessed` | PaymentService | OrderService, NotificationService |
| `PaymentFailed` | PaymentService | OrderService, NotificationService |

---

## Services

### OrderService (HTTP API)
- Exposes `POST /api/orders` — creates an order and publishes `OrderPlaced`
- Exposes `GET /api/orders/{id}` — returns the current order status
- Listens for downstream events and updates the order status accordingly
- Persists data to SQLite (`~/.ecommerce-eda/orders.db`)

**Order statuses:**
| Value | Meaning |
|---|---|
| `0` | Pending — just created |
| `1` | InventoryReserved — stock confirmed |
| `2` | PaymentProcessed — order complete |
| `3` | Failed — out of stock or payment declined |

### InventoryService (Worker)
- Listens for `OrderPlaced`
- Checks if all items are in stock
- If yes: deducts stock, publishes `InventoryReserved`
- If no: publishes `StockInsufficient`
- Persists stock to SQLite (`~/.ecommerce-eda/inventory.db`)

### PaymentService (Worker)
- Listens for `InventoryReserved`
- Simulates payment processing (80% success, 20% decline — random)
- Publishes `PaymentProcessed` or `PaymentFailed`

### NotificationService (Worker)
- Listens for **all** events
- Logs them — in a real system this would send emails, SMS, push notifications, etc.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Homebrew](https://brew.sh) (macOS) — to install RabbitMQ

---

## Setup & Running

### 1. Install and start RabbitMQ

```bash
brew install rabbitmq
brew services start rabbitmq
```

RabbitMQ management UI is available at http://localhost:15672 (username: `guest`, password: `guest`). You can see queues, exchanges, and message rates here.

### 2. Seed the inventory database

The inventory database starts empty. Add a product before placing orders:

```bash
sqlite3 ~/.ecommerce-eda/inventory.db \
  "INSERT OR REPLACE INTO Products (Id, Name, Stock) VALUES ('prod-1', 'Widget', 100);"
```

> The database file is created automatically when InventoryService first starts.
> Run this command after starting the services (step 3) if the file doesn't exist yet.

### 3. Start all services

Open four separate terminals (or run them in background):

```bash
# Terminal 1
dotnet run --project src/OrderService/OrderService.csproj

# Terminal 2
dotnet run --project src/InventoryService/InventoryService.csproj

# Terminal 3
dotnet run --project src/PaymentService/PaymentService.csproj

# Terminal 4
dotnet run --project src/NotificationService/NotificationService.csproj
```

---

## Testing

### Using the REST Client (VS Code)

1. Install the [REST Client extension](https://marketplace.visualstudio.com/items?itemName=humao.rest-client)
2. Open `src/OrderService/OrderService.http`
3. Click **Send Request** above a request block — results appear in a panel on the right

### Using curl

**Place an order:**
```bash
curl -s -X POST http://localhost:5299/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "cust-001",
    "items": [
      { "productId": "prod-1", "productName": "Widget", "quantity": 2, "unitPrice": 9.99 }
    ]
  }' | python3 -m json.tool
```

**Check order status** (replace `{id}` with the id from the response above):
```bash
curl -s http://localhost:5299/api/orders/{id} | python3 -m json.tool
```

Wait 2–3 seconds between placing and checking — the event chain runs asynchronously.

**Watch events fire in real time:**
```bash
tail -f /tmp/ecommerce-eda-logs/notification.log
```

### Test scenarios in `OrderService.http`

| Scenario | What to expect |
|---|---|
| Happy path (`prod-1`, qty 2) | Status `2` (PaymentProcessed) — or `3` if payment sim fails (20% chance) |
| Unknown product | Status `3` (Failed) — StockInsufficient |
| Quantity 999 | Status `3` (Failed) — StockInsufficient |

---

## Tech Stack

| Technology | Role |
|---|---|
| .NET 10 | Runtime and framework for all services |
| ASP.NET Core | HTTP API in OrderService |
| MassTransit 8 | Abstracts RabbitMQ — handles publishing, consuming, queue setup |
| RabbitMQ | Message broker — routes events between services |
| Entity Framework Core | ORM for SQLite persistence |
| SQLite | Lightweight database for Orders and Inventory |

---

## Why MassTransit?

Working directly with RabbitMQ requires managing connections, channels, exchange declarations, queue bindings, serialization, retries, and error handling manually. **MassTransit** is a library that wraps all of that — you write a simple consumer class and it handles the plumbing. It also makes it easy to swap RabbitMQ for another broker (Azure Service Bus, Amazon SQS) with minimal code changes.
