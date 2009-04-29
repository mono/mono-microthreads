MCS=gmcs
MCS_FLAGS=-debug -define:MONO

SOURCES = \
	Client.cs \
	SocketTest.cs \
	Server.cs

all: SocketTest.exe

SocketTest.exe: $(SOURCES)
	$(MCS) $(MCS_FLAGS) -target:exe -out:$@ -r:../Mono.MicroThreads.dll $(SOURCES)
	cp -a SocketTest.exe* ../

clean:
	rm -rf *.exe *.mdb bin obj

distclean: clean
	rm -f *~
