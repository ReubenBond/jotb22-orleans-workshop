# Microsoft Orleans workshop for J On The Beach 2022

## Lab 1 - Hello, Orleans

## Lab 2 - Hello, Grains

## Lab 3 - Dealing With State

## Lab 4 - Scaling Out

* Adding another host
* Looking at it with the Orleans Dashboard

## Lab X - Timers &amp; Reminders

* Add some statistics to our URL Shortener
* Using a timer, periodically clear statistics

* Reminder: forgettable URLs: delete the URL after some period of time

## Lab X - Fan-Out, Fan-In

* Modify the timer in the above to periodically flush stats to a stats collector grain
* NOTE: collecting stats at the *domain* level, not URL level
* Stats collectors fan-in by a factor of (eg) 2^16 at a time
* Hash domain, accrue at per-domain stats grain, then progressively up a chain of fan-in aggregator grains to a root TopN grain
* Flush all the way up to root-level stats on the 10 most popular domains in a given time period

## Other ideas for exploration

* Observers to monitor a given domain's statistics
* Observers to monitor a client based on IP
* Per-client statistics for URL create/update events
* RequestContext for AuthZ to perform administrative actions on a URL (eg, creator or admin can delete)
  * Could auto-generate a key at initial creation time, requiring that to be used to delete the URL or redirect it
* Throttling creation of shortened URLs by client IP
* Banning clients by IP