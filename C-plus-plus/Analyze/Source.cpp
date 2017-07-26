#include "Analyze.h"
#include <algorithm> // For for_each, transform, adjacent_find, count_if, accumulate, copy, max_element
#include <bitset>
#include <cassert>
#include <cctype> // For tolower
#include <cstdio> // For remove
#include <cstdlib> // For system
#include <fstream>
#include <functional> // For bind
#include <iostream>
#include <iterator> // For back_inserter, ostream_iterator, istream_iterator
#include <sstream>
#include <numeric> // For accumulate
#include <stdexcept>
#include <string>
#include <vector>
using namespace std;


int main()
{
	Analyze Analysis;
	string temp = "";
	system("dir /b *.dat > datfiles.txt 2>nul");
	ifstream datFiles("datfiles.txt");
	temp = "gage2scope.ini";
	Analysis.ReadData(temp);
	while (datFiles)
	{
		getline(datFiles, temp);
		if (temp == "")
		{
			break;
		}
		cout << temp << ": ";
		Analysis.ReadData(temp);
		Analysis.CleanGraph();
		Analysis.FindPulse();
		cout << endl;
		Analysis.ClearData();
	}
	cout << endl;
	system("PAUSE");
	return 0;
}