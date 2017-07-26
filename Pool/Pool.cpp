#include "Pool.h"
#include "MyObject.h"

Pool::Pool(const size_t eSize, const size_t bSize)
{
	cout << "Initializing a pool with element size " << eSize << " and block size " << bSize << endl;
	blockSize = bSize;
	elemSize = eSize;
	size_of_pool = 0;
	freePtr = nullptr;
}

Pool::~Pool()
{
	
}

void* Pool::allocate()
{
	//test if new row needed 
	if (freePtr == nullptr)
	{
		cout << "Expanding Pool...\n" << endl;
		char** newPoolArray = new char*[size_of_pool +1];
		memcpy(newPoolArray, oldPoolArray, size_of_pool);
		newPoolArray[size_of_pool] = new char[elemSize*blockSize];
		delete [] oldPoolArray;
		oldPoolArray = newPoolArray;
		char *p = newPoolArray[size_of_pool];
		cout << "Linking cells starting at " << static_cast<void*>(oldPoolArray[size_of_pool]) << endl;
		for (int i = 0; i < blockSize - 1; ++i)
		{
			*reinterpret_cast<char**>(p) = p + elemSize;
			p += elemSize;
		}
		*reinterpret_cast<char**>(p) = nullptr;
		freePtr = newPoolArray[size_of_pool++];
	}
	void* temp = freePtr;
	freePtr = *reinterpret_cast<char**>(freePtr);
	return temp;
}

void Pool::deallocate(void* deadPtr)
{
	cout << "Cell deallocated at " << deadPtr << endl;
	char* address = reinterpret_cast<char*>(freePtr);
	freePtr = deadPtr;
	char** newPtr = reinterpret_cast<char**>(deadPtr);
	*newPtr = address;
}