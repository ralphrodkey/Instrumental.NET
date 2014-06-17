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

Mono
====
You may need to install `Common.logging` assemblies before attempting to compile this package. You can install them in your project with via NuGet with the following command:

```
mono --runtime=v4.0 /path/to/NuGet.exe install Instrumental.NET/packages.config -OutputDirectory packages
```

[Instrumental]:http://instrumentalapp.com
[Common.logging]:http://netcommon.sourceforge.net/
