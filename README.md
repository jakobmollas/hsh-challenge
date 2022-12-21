# hsh-challenge
![Continuous Integration](https://github.com/jakobmollas/hsh-challenge/workflows/Continuous%20Integration/badge.svg)

HSH code challenge for wannabe code monkeys.
The application is a simple WPF application that monitors a file for changes and displays the contents continuously.

* C#/.NET WPF (6.0)
* Focus on async constructs and testabiity
* Using [SemaphoreSlim](https://learn.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim?view=net-6.0) to implement deterministic testing of the async poller
* XUnit/Moq
