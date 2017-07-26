#pragma once
#include <iostream>
#include <string>

using namespace std;

class Pool
{
public:
	Pool(size_t elemSize, size_t blockSize = 5);
	~Pool();
	void* allocate();       // Get a pointer inside a pre-allocated block for a new object    
	void deallocate(void*); // Free an object's slot (push the address on the "free list")
private:
	char ** oldPoolArray;
	void * freePtr;
	int size_of_pool;
	size_t elemSize;
	size_t blockSize;
};