#include "MyObject.h"
#include <iostream>
#include <string>

using namespace std;

int main() {
	// Create an array of MyObject heap objects
	const size_t MAXOBJS = 20;
	MyObject* objs[MAXOBJS];
	for (size_t i = 0; i < MAXOBJS; ++i)
		objs[i] = MyObject::create(i, "\"" + to_string(i) + "\"");
	cout << "Object 5 == " << *objs[5] << endl;
	delete objs[5];
	objs[5] = 0;

	cout << "Creating another object:\n";
	MyObject* anotherObject = MyObject::create(100, "anotherObject");
	cout << "anotherObject == " << *anotherObject << endl;

	cout << "Creating yet another object:\n";
	MyObject* yetAnotherObject = MyObject::create(120, "yetAnotherObject");
	cout << "yetAnotherObject == " << *yetAnotherObject << endl;

	// Delete everything (although it's not "necessary"!)
	delete anotherObject;
	delete yetAnotherObject;
	for (size_t i = 0; i < MAXOBJS; ++i)
		delete objs[i];
	cin.get();
}