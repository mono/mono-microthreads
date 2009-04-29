from stackless import *
from datetime import timedelta, datetime

threads = 100
loops = 1000000

def loop():
	global loops
	i = 0
	while i < loops:
		stackless.schedule()
		i = i + 1


t1 = datetime.now()

for i in xrange(threads):
	t = tasklet(loop)()

run()

t2 = datetime.now()

td = t2 - t1

print str(threads) + " threads * " + str(loops) + " loops = " + str(threads * loops) + " yields in " + str(td)
