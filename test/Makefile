
MCS=gmcs
MONO=mono

#MCSFLAGS=-debug
MCSFLAGS=-r:Mono.Tasklets

#RUNFLAGS=--debug
RUNFLAGS=-O=all
#RUNFLAGS=

OPTIM=" " peephole branch inline cfold consprop copyprop deadce linears cmov shared sched intrins tailc loop fcmov leaf aot precomp abcrem ssapre exception ssa treeprop

PROGS = intro1.exe intro2.exe YieldTest.exe LocalTest.exe IteratorTest.exe TimeTest1.exe TimeTest2.exe GCTest.exe SleepTest.exe

all: Mono.MicroThreads.dll SocketTest.exe $(PROGS)

.PHONY: Mono.MicroThreads.dll SocketTest.exe clean distclean

Mono.MicroThreads.dll:
	make -C ../Mono.MicroThreads

SocketTest.exe: Mono.MicroThreads.dll
	make -C SocketTest

GfxTest.exe: Mono.MicroThreads.dll
	$(MCS) $(MCSFLAGS) -out:$@ -r:Mono.MicroThreads.dll -r:System.Drawing -r:System.Windows.Forms GfxTest.cs

SleepTest.exe: Mono.MicroThreads.dll
	$(MCS) $(MCSFLAGS) -out:$@ -r:Mono.MicroThreads.dll -r:Mono.Posix SleepTest.cs

%.exe: %.cs #Mono.MicroThreads.dll
	$(MCS) $(MCSFLAGS) -out:$@ -r:Mono.MicroThreads.dll $<


# Run rules

run: test.exe
	$(MONO) $(RUNFLAGS) $^

runy: YieldTest.exe
	$(MONO) $(RUNFLAGS) $^ 1 1000000

runyo: YieldTest.exe
	for i in $(OPTIM); do echo $$i":"; $(MONO) --optimize=$$i $^ 100 50000; done

runyield: YieldTest.exe
	@echo running yieldtest
	@(sleep 10; echo -n size=;  ps -C mono -o size=)&
	@$(MONO) $(RUNFLAGS) $^ 100 500000
	@(sleep 10; echo -n size=;  ps -C mono -o size=)&
	@$(MONO) $(RUNFLAGS) $^ 1000 40000
	@(sleep 10; echo -n size=;  ps -C mono -o size=)&
	@$(MONO) $(RUNFLAGS) $^ 10000 3000
	@(sleep 10; echo -n size=;  ps -C mono -o size=)&
	@$(MONO) $(RUNFLAGS) $^ 100000 300
	@(sleep 10; echo -n size=;  ps -C mono -o size=)&
	@$(MONO) $(RUNFLAGS) $^ 500000 50

runtt1: TimeTest1.exe
	$(MONO) $(RUNFLAGS) $^

runtt2: TimeTest2.exe
	$(MONO) $(RUNFLAGS) $^

runss: SocketTest.exe
	$(MONO) $(RUNFLAGS) $^

runsc: SocketTest.exe
	$(MONO) $(RUNFLAGS) $^ client

runl: LocalTest.exe
	$(MONO) $(RUNFLAGS) $^

rungc: GCTest.exe
	$(MONO) $(RUNFLAGS) $^

gdb: test.exe
	gdb --args $(MONO) $(RUNFLAGS) test.exe

gdb3: test3.exe
	gdb --args $(MONO) $(RUNFLAGS) test3.exe

clean:
	make -C ../Mono.MicroThreads clean
	make -C SocketTest clean
	rm -f *.exe *.dll *.pdb *.mdb *.so

distclean: clean
	make -C ../Mono.MicroThreads distclean
	make -C SocketTest distclean
	rm -f *~

fixlf:
	make -C ../Mono.MicroThreads fixlf
	find . -name "*.cs" | xargs flip -uv
