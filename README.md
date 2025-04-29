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

## 🛡️ Database Concurrency Control

### Isolation Levels Deep Dive

| Level               | Dirty Reads | Non-Repeatable Reads | Phantom Reads | Concurrency | Use Case Examples                  |
|---------------------|-------------|-----------------------|---------------|-------------|------------------------------------|
| **Read Uncommitted**| ✅ Allowed   | ✅ Possible           | ✅ Possible    | High        | Logging, metrics aggregation       |
| **Read Committed**  | ❌ Prevented | ✅ Possible           | ✅ Possible    | Medium-High | Most OLTP applications             |
| **Repeatable Read** | ❌ Prevented | ❌ Prevented          | ✅ Possible    | Medium      | Financial batch processing         |
| **Serializable**    | ❌ Prevented | ❌ Prevented          | ❌ Prevented   | Low         | Booking systems, inventory management |

### SQL Locking Implementation

#### Core Lock Types
```sql
-- Explicit pessimistic lock example
BEGIN TRANSACTION
    SELECT * FROM Bookings WITH (
        UPDLOCK,    -- Update lock prevents other UPDLOCK/SHARED locks
        ROWLOCK,     -- Prevents lock escalation to page/table
        HOLDLOCK     -- Hold lock until transaction ends
    ) 
    WHERE Id = 1
    
    -- Application logic here
    
COMMIT TRANSACTION
```

## .NET Integration Patterns

#### 1. Pessimistic Locking with EF Core
```csharp
// Transaction with explicit locking
await using var transaction = await _context.Database.BeginTransactionAsync(
    IsolationLevel.Serializable);

try
{
    var booking = await _context.Bookings
        .FromSqlInterpolated($@"
            SELECT * FROM Bookings WITH (UPDLOCK, ROWLOCK, HOLDLOCK)
            WHERE Id = {bookingId}")
        .FirstOrDefaultAsync();

    // Critical section - other transactions will wait
    booking.Status = "Confirmed";
    await _context.SaveChangesAsync();
    
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

#### 2. Optimistic Concurrency with RowVersion
```csharp
// Model configuration
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Booking>()
        .Property(b => b.RowVersion)
        .IsRowVersion();
}

// Conflict handling
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    var entry = ex.Entries.Single();
    var databaseValues = await entry.GetDatabaseValuesAsync();
    
    // Resolve conflict (e.g., merge changes)
    entry.OriginalValues.SetValues(databaseValues);
}
```

## 🛡️ Advanced Locking Strategies

#### Distributed Locking with Redis
```csharp
// RedisLockService implementation
public class RedisLockService : IBookingLockService
{
    private readonly IDatabase _redis;
    private readonly TimeSpan _defaultLockTimeout = TimeSpan.FromSeconds(30);

    public async Task<bool> AcquireLockAsync(string key, string value)
    {
        return await _redis.StringSetAsync(
            key, 
            value, 
            _defaultLockTimeout, 
            When.NotExists);
    }

    public async Task ReleaseLockAsync(string key, string value)
    {
        var luaScript = @"
            if redis.call('GET', KEYS[1]) == ARGV[1] then
                return redis.call('DEL', KEYS[1])
            else
                return 0
            end";
            
        await _redis.ScriptEvaluateAsync(luaScript, new RedisKey[] { key }, new RedisValue[] { value });
    }
}
```

#### Semaphore Locking for In-Process Control
```csharp
public class SemaphoreLockService : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ConcurrentDictionary<string, DateTime> _lockTimes = new();

    public async Task<IDisposable> AcquireLockAsync(string key, TimeSpan timeout)
    {
        if (!await _semaphore.WaitAsync(timeout))
            throw new TimeoutException("Could not acquire lock");
            
        _lockTimes[key] = DateTime.UtcNow.Add(timeout);
        return new LockReleaser(() => ReleaseLock(key));
    }

    private void ReleaseLock(string key) 
    {
        _lockTimes.TryRemove(key, out _);
        _semaphore.Release();
    }

    private class LockReleaser : IDisposable
    {
        private readonly Action _release;
        public LockReleaser(Action release) => _release = release;
        public void Dispose() => _release();
    }
}
```

## 🚨 Deadlock Prevention Guide

#### Detection and Resolution
```csharp
-- Find deadlock victims
SELECT 
    victim = CASE WHEN deadlock.victim_id = sess.session_id THEN '✓' ELSE '' END,
    sess.session_id,
    sql_text.text AS query_text
FROM sys.dm_exec_sessions sess
JOIN sys.dm_exec_connections conn ON sess.session_id = conn.session_id
CROSS APPLY sys.dm_exec_sql_text(conn.most_recent_sql_handle) AS sql_text
JOIN sys.dm_tran_locks locks ON sess.session_id = locks.request_session_id
JOIN sys.dm_os_waiting_tasks waits ON locks.lock_owner_address = waits.resource_address
JOIN sys.dm_xe_sessions deadlock ON waits.session_id = deadlock.address
```

#### .NET Deadlock Handling
```csharp
// Retry policy with Polly
var retryPolicy = Policy
    .Handle<SqlException>(ex => ex.Number == 1205) // Deadlock victim
    .WaitAndRetryAsync(new[]
    {
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromMilliseconds(200),
        TimeSpan.FromMilliseconds(500)
    });

await retryPolicy.ExecuteAsync(async () => 
{
    await using var transaction = await _context.Database.BeginTransactionAsync();
    // Critical operations
});
```

## 📊 Performance Optimization

### Locking Best Practices

#### 1. Minimize Lock Duration
```csharp
// BAD: Long-running transaction
using var transaction = /* ... */;
var data = await FetchExternalData(); // ← Network call inside transaction
// GOOD: Get data before transaction
```

#### 2. Precise Lock Targeting
```csharp
-- Better: Narrow scope
SELECT * FROM Orders WITH (UPDLOCK) WHERE OrderId = 1234

-- Worse: Broad scope
SELECT * FROM Orders WITH (TABLOCKX)
```

#### 3. Monitoring Active Locks
```csharp
-- Real-time lock monitoring
SELECT
    t.text AS QueryText,
    l.resource_type,
    l.request_mode,
    l.request_status,
    s.login_name
FROM sys.dm_tran_locks l
JOIN sys.dm_exec_sessions s ON l.request_session_id = s.session_id
JOIN sys.dm_exec_requests r ON s.session_id = r.session_id
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
```

## 🔒 Lock Compatibility Matrix

### SQL Server Lock Modes

| Abbr | Lock Mode           | Description                                                                 |
|------|---------------------|-----------------------------------------------------------------------------|
| **S**  | Shared              | Read-only access, multiple transactions can hold simultaneously            |
| **X**  | Exclusive           | Write lock, prevents all other locks                                       |
| **U**  | Update              | Initial read lock that converts to X lock for updates                      |
| **IS** | Intent Shared       | Signals intention to place S locks lower in hierarchy                      |
| **IX** | Intent Exclusive    | Signals intention to place X locks lower in hierarchy                      |
| **SIU**| Shared Intent Update| Special lock for index operations                                          |

### Compatibility Matrix

| Requested \ Held | IS  | S   | U   | IX  | X   | SIU |
|------------------|-----|-----|-----|-----|-----|-----|
| **Intent Shared (IS)** | ✅ | ✅ | ✅ | ✅ | ❌ | ✅  |
| **Shared (S)**        | ✅ | ✅ | ✅ | ❌ | ❌ | ⚠️  |
| **Update (U)**        | ✅ | ✅ | ❌ | ❌ | ❌ | ❌  |
| **Intent Exclusive (IX)** | ✅ | ❌ | ❌ | ✅ | ❌ | ❌  |
| **Exclusive (X)**     | ❌ | ❌ | ❌ | ❌ | ❌ | ❌  |
| **Shared Intent Update (SIU)** | ✅ | ⚠️ | ❌ | ❌ | ❌ | ❌  |

**Key**:
- ✅ = Compatible (locks can coexist)
- ❌ = Conflict (blocks requesting transaction)
- ⚠️ = Special case (depends on resource hierarchy)

## Getting Started

### Prerequisites
- .NET 8 SDK
- Redis Server (for Redis locking)
- SQL Server (for SQL locking)
