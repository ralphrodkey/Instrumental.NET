Instrumental.NET
================

A C# agent library for the [Instrumental] logging service without using Statsd.

Features
========
 - Doesn't require Statsd
 - Uses [Common.logging], so it probably works with your existing logging library

Example
=======
```C#
using Instrumental.NET;

var agent = new Agent("your api key here");

agent.Increment("myapp.logins");
agent.Gauge("myapp.server1.free_ram", 1234567890);
agent.Time("myapp.expensive_operation", () => LongRunningOperation());
agent.Notice("Server maintenance window", 3600);
```

[Instrumental]:http://instrumentalapp.com
[Common.logging]:http://netcommon.sourceforge.net/