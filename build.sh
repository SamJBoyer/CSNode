x#!/bin/bash

# YOU MUST MAKE SURE THE FOLLOWING 3 LINES ARE CORRECT. THEY WILL NOT BE CORRECT OFF GITHUB

# Set the project path and output directory
PROJECT_PATH="/home/sam/Desktop/Github/CSNode/Redis Stream Test/"
# Path to Unity executable - make sure this path is correct
UNITY_PATH="/home/sam/Unity/Hub/Editor/2022.3.32f1/Editor/Unity"
# Set the executable name
EXECUTABLE_NAME="CSNode.bin"


# EVERYTHING BELOW THIS LINE IS DONE AUTOMATICALLY AND SHOULDN'T NEED TO BE EDITED

OUTPUT_DIR=$(pwd)
# Ensure the output directory exists
mkdir -p $OUTPUT_DIR
#get the folder name for building 
DIR_NAME=$(basename "$OUTPUT_DIR")

# Define the build target (Linux in this case, adjust as needed)
BUILD_TARGET=StandaloneLinux64

# Run Unity in batch mode to build the project
"$UNITY_PATH" \
    -quit \
    -batchmode \
    -nographics \
    -projectPath "$PROJECT_PATH" \
    -buildTarget $BUILD_TARGET \
    -executeMethod BuildScript.Build \
    -buildScene "$DIR_NAME" \
    -buildOutput "$OUTPUT_DIR/$EXECUTABLE_NAME"

# Check if the build was successful
if [ $? -eq 0 ]; then
    echo "Build succeeded. Executable is located at $OUTPUT_DIR/$EXECUTABLE_NAME"
else
    echo "Build failed."
    exit 1
fi
