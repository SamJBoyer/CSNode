.PHONY: all clean build run

all: build

build:
	@echo "Starting Unity build process..."
	@./build.sh

run:
	@echo "Launching the executable..."
	@./CSNode.bin

clean:
	@echo "Cleaning build directory..."
	@rm -rf CSNode_Data CSNode.x86_64 UnityPlayer_s.debug CSNode_s.debug CSNode_s CSNode UnityPlayer.so CSNode.bin
	@echo "Clean complete."
