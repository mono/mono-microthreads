MCS=gmcs

#MCSFLAGS=-debug -define:MONO\;MT_DEBUG\;EXTRA_CHECKS\;MT_TIMING
#MCSFLAGS=-define:MONO -debug
MCSFLAGS=-define:MONO -r:Mono.Tasklets

SOURCES = \
	Channel.cs \
	CriticalSection.cs \
	MicroThread.cs \
	Scheduler.cs \
	Semaphore.cs \
	Socket.cs

all: Mono.MicroThreads.dll

Mono.MicroThreads.dll: $(SOURCES)
	$(MCS) $(MCSFLAGS) -target:library -out:$@ $(SOURCES)
	cp Mono.MicroThreads.dll* ../test/

clean:
	rm -rf *.dll *.mdb bin obj

distclean: clean
	rm -f *~

fixlf:
	find . -name "*.cs" | xargs flip -uv
