from stackless import *

res = 0

def consumer(chan):
	global res
	while 1:
		data = chan.receive()
		res = res + 1
#		print data


def producer(chan):
	for i in range(0,1000000):
		chan.send(i)


c = channel()
t1 = tasklet(consumer)(c)
t2 = tasklet(consumer)(c)
t3 = tasklet(producer)(c)
run()
print res
