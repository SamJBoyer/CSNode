# CSNode
a c# node for BRAND.

Things you have to do: 
- in the build.sh file you HAVE declare the path to the unity project to be built
- the make file has to be targeting the name of your executable. this will not automatically generate
- ever node folder that uses unity must include a build.sh file and an asset called BuildScript inside the asset directory in a folder called "Editor". this is included in the package, but don't forget it or delete it.

Common errors and their solutions
- Build Problems
  - Sometimes when building from the make file, the build will fail due to lack of permissions for library/bee files. to fix this, go the to the unity project folder for the project you're trying to build and delete the library, then re-enter the make command. this will regenerate the library with the appropriate permissions
  - If you build the program from the editor it will create a [projectname]_Data folder, but it will be locked and owned by owner. These permissions will cause the build to fail when done from brand or by entering make directly from the terminal. To fix this, delete the folder using entering command sudo rm -rf [name of folder]. This can also sometimes happen when building directly from BRAND using "XADD supervisor_ipstream * commands make"
  - Double check that youre path in the build.sh files are correct. They will NOT automatically generate from github and have to be redone manually EVERY time you you create a new node 
