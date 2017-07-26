#include "XRef.h"
#include <stdlib.h>
#include <iostream>
using namespace std;


int main(int argc, char* argv[])
{
	string temp;
	XRef MyXRef;
	if (argc < 2)
	{
		cout << "No file put into command line." << endl;
		cout << "Please input the name of the file\n(Capitalization matters and don't worry about the extension): ";
		cin >> temp;
		temp += ".txt";
		MyXRef.FileRead(temp);
	}
	else
	{
		temp = argv[1];
		MyXRef.FileRead(temp);
	}
	system("PAUSE");
	return 0;
}