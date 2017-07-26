#pragma once
#include "Pool.h"
#include <string>
#include <iostream>
class MyObject
{
public:
	static MyObject* create(int id, const std::string& name) 
	{ 
		// Factory method
		return new MyObject(id, name);
	}
	MyObject(const MyObject&) = delete;
	MyObject& operator = (const MyObject&) = delete;
	static void operator delete(void* p)
	{
		myPool.deallocate(p);
	}
	friend std::ostream& operator<< (std::ostream& out, const MyObject&);


private:
	int id;
	std::string name;
	static Pool myPool;
	MyObject(int i, const std::string& nm) 
		: name(nm) 
	{
		id = i;
	}
	static void* operator new(size_t)
	{
		void* p = myPool.allocate();
		std::printf("Cell allocated at %p \n", p);
		return p;
	}
};