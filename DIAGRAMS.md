# Message Flow Diagrams

> All diagrams use **Mermaid** — rendered automatically on GitHub.

---

## 1. How a Message Gets Published to RabbitMQ

When your service calls `publisher.Publish(new OrderPlaced(...))`, here is what happens step by step:

```mermaid
sequenceDiagram
    participant Code as Service Code
    participant MT as MassTransit
    participant RMQ as RabbitMQ Exchange<br/>(fanout)
    participant Q1 as Queue: inventory
    participant Q2 as Queue: notification

    Code->>MT: publisher.Publish(new OrderPlaced(...))
    MT->>MT: Serialize to JSON<br/>Add message headers
    MT->>RMQ: Publish to Exchange<br/>Contracts:OrderPlaced
    RMQ-->>Q1: Copy of message
    RMQ-->>Q2: Copy of message
    Note over RMQ: Fanout = broadcast<br/>every bound queue gets a copy
```

---

## 2. How a Message Gets Consumed from RabbitMQ

Each service has its own dedicated queue. MassTransit creates it on startup and keeps polling it.

```mermaid
sequenceDiagram
    participant Q as RabbitMQ Queue
    participant MT as MassTransit
    participant C as IConsumer<OrderPlaced>
    participant DB as SQLite / Business Logic

    Q->>MT: Message arrives in queue
    MT->>MT: Deserialize JSON<br/>Wrap in ConsumeContext
    MT->>C: Consume(context)
    C->>DB: Read / Write data
    C->>MT: Task completed (no exception)
    MT->>Q: ACK — remove message from queue

    alt Exception thrown
        C-->>MT: throws Exception
        MT-->>Q: NACK — retry later
    end
```

---

## 3. Full End-to-End Event Chain

This shows the complete happy path and the failure paths triggered by a single `POST /api/orders`.

```mermaid
flowchart TD
    Client(["👤 Client\nPostman / curl"])
    OS["OrderService\nPOST /api/orders"]
    DB_O[("orders.db\nstatus = Pending")]

    EX_OP{{"Exchange\nOrderPlaced\n(fanout)"}}

    INV["InventoryService\nOrderPlacedConsumer"]
    NOTIF_1["NotificationService\nlogs OrderPlaced"]
    DB_I[("inventory.db")]

    EX_IR{{"Exchange\nInventoryReserved\n(fanout)"}}
    EX_SI{{"Exchange\nStockInsufficient\n(fanout)"}}

    PAY["PaymentService\nInventoryReservedConsumer\n80% success / 20% decline"]
    OS_IR["OrderService\nstatus = InventoryReserved"]
    NOTIF_2["NotificationService\nlogs InventoryReserved"]

    OS_SI["OrderService\nstatus = Failed"]
    NOTIF_SI["NotificationService\nlogs StockInsufficient"]

    EX_PP{{"Exchange\nPaymentProcessed\n(fanout)"}}
    EX_PF{{"Exchange\nPaymentFailed\n(fanout)"}}

    OS_PP["OrderService\nstatus = PaymentProcessed ✅"]
    NOTIF_PP["NotificationService\nlogs PaymentProcessed"]
    OS_PF["OrderService\nstatus = Failed ❌"]
    NOTIF_PF["NotificationService\nlogs PaymentFailed"]

    Client -->|POST /api/orders| OS
    OS --> DB_O
    OS -->|Publish| EX_OP

    EX_OP --> INV
    EX_OP --> NOTIF_1

    INV --> DB_I
    DB_I -->|stock ok| EX_IR
    DB_I -->|out of stock| EX_SI

    EX_IR --> PAY
    EX_IR --> OS_IR
    EX_IR --> NOTIF_2

    EX_SI --> OS_SI
    EX_SI --> NOTIF_SI

    PAY -->|success| EX_PP
    PAY -->|decline| EX_PF

    EX_PP --> OS_PP
    EX_PP --> NOTIF_PP

    EX_PF --> OS_PF
    EX_PF --> NOTIF_PF

    style EX_OP fill:#f90,color:#000
    style EX_IR fill:#f90,color:#000
    style EX_SI fill:#f90,color:#000
    style EX_PP fill:#f90,color:#000
    style EX_PF fill:#f90,color:#000
    style OS_PP fill:#2d6a4f,color:#fff
    style OS_SI fill:#9b2335,color:#fff
    style OS_PF fill:#9b2335,color:#fff
    style DB_O fill:#264653,color:#fff
    style DB_I fill:#264653,color:#fff
```

---

## 4. RabbitMQ Exchange → Queue → Consumer Binding

This shows how one fanout exchange fans out to multiple service queues, and how each service independently consumes its own copy.

```mermaid
graph LR
    PUB(["Publisher\nInventoryService"])

    subgraph RabbitMQ["RabbitMQ Broker"]
        EX{{"Exchange\nContracts:InventoryReserved\ntype = fanout"}}
        Q1["Queue\npayment-inventory-reserved"]
        Q2["Queue\norder-inventory-reserved"]
        Q3["Queue\nnotification-inventory-reserved"]
        EX --> Q1
        EX --> Q2
        EX --> Q3
    end

    CON1["PaymentService\nInventoryReservedConsumer\n→ process payment"]
    CON2["OrderService\nInventoryReservedConsumer\n→ status = InventoryReserved"]
    CON3["NotificationService\nInventoryReservedConsumer\n→ log event"]

    PUB -->|Publish| EX
    Q1 -->|consume| CON1
    Q2 -->|consume| CON2
    Q3 -->|consume| CON3

    style EX fill:#f90,color:#000
    style Q1 fill:#457b9d,color:#fff
    style Q2 fill:#457b9d,color:#fff
    style Q3 fill:#457b9d,color:#fff
```

---

## 5. What MassTransit Handles For You

```mermaid
graph TD
    subgraph Without["Without MassTransit — Manual"]
        A1["Declare AMQP connection"]
        A2["Open channel"]
        A3["Declare exchange + type"]
        A4["Declare queue"]
        A5["Bind queue to exchange"]
        A6["Serialize message to bytes"]
        A7["Publish with routing key"]
        A8["Start consumer loop"]
        A9["Deserialize bytes"]
        A10["Ack / Nack manually"]
        A11["Handle retries & dead-letter"]
        A1 --> A2 --> A3 --> A4 --> A5 --> A6 --> A7
        A8 --> A9 --> A10 --> A11
    end

    subgraph With["With MassTransit — What you write"]
        B1["publisher.Publish(new MyEvent(...))"]
        B2["class MyConsumer : IConsumer&lt;MyEvent&gt;\n{ Consume(context) { ... } }"]
    end

    style B1 fill:#2d6a4f,color:#fff
    style B2 fill:#2d6a4f,color:#fff
```

> MassTransit handles everything in the left box automatically at startup.
> You only write the two green boxes.
