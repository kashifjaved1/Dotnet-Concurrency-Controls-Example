# .NET Concurrency Controls Example

![.NET Core](https://img.shields.io/badge/.NET-8.0-purple)
![SignalR](https://img.shields.io/badge/SignalR-Yes-blue)
![Redis](https://img.shields.io/badge/Redis-3.1.0-red)

A practical demonstration of three locking strategies in .NET for handling concurrent bookings:

- 🔒 **SQL Pessimistic Locking** (UPDLOCK/ROWLOCK)
- 🧠 **In-Memory Semaphore Locking**
- 🌐 **Distributed Redis Locking**

## Features

- Real-time lock status updates via SignalR
- Optimistic concurrency with EF Core `RowVersion`
- Clean architecture with DI services
- Responsive UI with lock notifications
- Switchable lock providers via configuration

## Project Structure
![image](https://github.com/user-attachments/assets/cdedfabc-4890-4199-a13a-9806762fa604)

# Locking Mechanisms
![image](https://github.com/user-attachments/assets/de0c7507-b444-4bb4-b17d-6c8e0f65a5e5)

## Getting Started

### Prerequisites
- .NET 8 SDK
- Redis Server (for Redis locking)
- SQL Server (for SQL locking)
